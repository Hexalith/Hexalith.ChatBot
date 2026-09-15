---
title: Product Requirements Document - Hexalith.ChatBot
created: "2026-05-10"
updated: "2026-09-14"
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
  - "_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md"
  - "_bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/source-manifest.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/qualification-evidence.md"
documentCounts:
  productBriefs: 1
  research: 0
  brainstorming: 0
  projectDocs: 3
  projectContext: 1
classification:
  projectType: saas_b2b
  domain: enterprise collaboration / AI project workspace
  complexity: medium
  projectContext: greenfield product on existing Hexalith platform
workflowType: 'prd'
releaseMode: single-release
status: final
completedAt: "2026-05-10T21:29:40.8096941+02:00"
finalizedAt: "2026-09-14"
lastEdited: "2026-09-14"
editHistory:
  - date: "2026-09-14"
    changes: "Finalized the recommended validation update after resolving all four Critical and eleven High findings, clearing the repeated contract/rubric/source gates at Critical and High severity, reconciling current source revisions, triaging open assumptions, and applying structure and prose polish. A5, A6, A9a, A10, A11, and A13 remain evidence gates as stated in Current Release Status."
  - date: "2026-09-14"
    changes: "Reopened to apply the current validation bundle's four Critical and eleven High contract fixes; evidence gates remain open pending re-validation."
  - date: "2026-09-14"
    changes: "Finalized the recommended update after complete input reconciliation, memlog audit, source pinning, structure/prose polish, and clean rubric v6, adversarial v5, and contract/source v5 gates. The product remains release-gated by A5, A6, A13, A10, and A11 as stated in Current Release Status."
  - date: "2026-09-14"
    changes: "Reopened the PRD to reconcile the 2026-09-13 validation findings, recovered decision memory, current product brief, and Epic 12 recovery-provenance architecture. Tightened AI approval, allowlist, audit atomicity, tenant isolation, authenticity, replay, idempotency, classifier, lifecycle, chat, ownership, release-gate, recovery-evidence, policy-schema, privacy, and source-lineage contracts."
  - date: "2026-06-09"
    changes: "Approved governed interactive chat as an MVP write surface through the shared command gateway and FrontComposer shell."
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

The user-voice anchor from the product brief: *"I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context."* In the MVP, "subject" is plain-language framing. Actionable collaboration must resolve to a governed Project; general non-project workspaces are post-MVP. M0 proves the loop for one team, M1 extends it across UI, CLI, and MCP, and M2 makes it production-ready.

### Product Thesis and Validation

Hexalith.ChatBot treats AI as a governed project actor, not a disconnected assistant. Its working thesis is that email can become authorized Project work only when the system can prove the Project, requester, authorized files, policy, human approval where required, command, and recorded outcome. This is an internal thesis pending the §Measurable Outcomes gates, not an external competitive claim.

The differentiator is the combination of durable tenant, party, Project, conversation, folder, approval, command, event, and projection boundaries with visible ambiguity handling. The system exposes evidence and routes uncertainty to authorized review rather than using hidden model confidence to attach email or execute work silently.

Validation evidence must demonstrate that email is correctly associated or visibly routed for resolution. It must show that unresolved identity, authorization, or context causes refusal or pause and that AI receives only authorized scope and performs low-risk assistance only within policy. Boundary-crossing work must produce a fully attributed proposal and require authorized human approval. Approved effects must use bounded-context commands, with equivalent state and audit outcomes visible across UI, CLI, and MCP. The bar is governed, fail-closed execution with a complete decision trail—not perfect model output. Association accuracy, authorization enforcement, approval completion, command consistency, and audit completeness are the measured proof points.

## Current Release Status

- **Artifact:** finalized on 2026-09-14 after the validation-recommended update and reviewer resolution. The approved normative appendices are current contract authority; neither document closure nor file presence waives an increment gate or substitutes for its required evidence.
- **M0:** blocked until A5 (live-AI provider), A6 (runtime data protection), and A13 (cross-context execution, authority, audit, fencing, and correction-owner contracts) have `approved-current` gate records; the TaskIntentDetector and ActionRiskClassifier are additionally disabled until their exact A9a M0 qualification records are `approved-current`.
- **M1:** requires revalidated `approved-current` A5/A6/A13 and exact deployed detector/classifier A9a gate records, and is additionally blocked until A11-M1 has an `approved-current` record that freezes and evidences every mandatory M1 metric definition, denominator, supported-request mix, target, sample/window, source, owner, and pass/fail rule.
- **M2:** requires the exact deployed detector/classifier A9a qualification to remain `approved-current` and is additionally blocked until A10 and A11-M2 have `approved-current` gate records for fresh qualifying recovery evidence and the complete evidenced SLO catalog, respectively.
- **Claims:** no pilot-onboarding, GDPR-satisfaction, tamper-evident-completeness, or production-readiness claim is permitted while its gate is open.

Gate status has exactly five values: `open`, `approved-current`, `expired`, `invalidated`, and `superseded`. The system computes status only from immutable records that conform to `addendum.md` §Increment Gate Record Contract and bind to the exact candidate, dependency revisions, environment, evidence hashes, independent approvers, expiry, and reopen predicates. A rejected decision or required-threshold failure results in `open`. Material drift or revocation results in `invalidated`; elapsed expiry results in `expired`; and a valid successor sets the predecessor to `superseded`. File presence or prose approval cannot close a gate. This final PRD claims no closure for A5, A6, A9a detector/classifier qualification, A10, A11-M1, A11-M2, or A13.

### Authority Map

| Decision surface | Sole authority |
| --- | --- |
| Increment scope, evidence, rollback, and permitted claims | §Minimum Release Slice — Three Increments |
| Success thresholds and counter-metrics | §Measurable Outcomes |
| Workflow states and transitions | §Shared Workflow Contract |
| Public ChatBot operations and queries | §Command and Query Contracts |
| Bounded-context and conversation-assignment ownership | §Context Ownership |
| AI classification, allowlists, policy schema, gate records, evaluation protocol, approval freshness, pipeline profiles, idempotency, replay, authenticity, sender authority, and operating backlog | `addendum.md` normative appendices |
| Functional and quality requirements | §Functional Requirements and §Non-Functional Requirements |
| Open approval/evidence state | §Open Assumptions and Decisions and `qualification-evidence.md` |
| Durable decisions and overrides | `.memlog.md`; mapped by `memlog-audit-2026-09-14.md` |

Repeated journey and strategy prose is explanatory only and cannot change these authorities.

## Product Classification, Source Context, and Boundaries

Hexalith.ChatBot is classified as a B2B SaaS product in the enterprise collaboration and AI project workspace domain. Its complexity is medium: it is not a regulated vertical product, but it has significant requirements for multi-tenant identity, external collaboration, mailbox ingestion, project association, file governance, approval workflow, auditability, and cross-surface command parity.

The PRD is a greenfield product definition on the existing Hexalith platform and depends on Hexalith.Conversations, Hexalith.Projects, Hexalith.Folders, Hexalith.Parties, Hexalith.Tenants, Hexalith.EventStore, Keycloak, Aspire, and Hexalith.FrontComposer. The SaaS concern is governed collaboration among internal users, external parties, automation clients, and AI actors across organizational boundaries.

Source context for downstream architecture and story creation is the repository baseline enumerated in `source-manifest.md`, refreshed on 2026-09-14. The manifest pins the direct product/architecture inputs and the checked-out revisions for Hexalith.Conversations, Hexalith.Projects, Hexalith.Folders, Hexalith.Parties, Hexalith.Tenants, Hexalith.EventStore, Hexalith.FrontComposer, Hexalith.Memories, and Hexalith.Commons.

**Material-change re-check protocol.** A "material change" to a sibling bounded-context artifact changes any of the following: the command/event contract surface ChatBot depends on (command names, event names, schemas, error codes), the authorization model (role names, scope semantics, policy claims), the identifier model (party-ID format, project-ID lifecycle, conversation-ID semantics), the integration topology (replacing or removing one of the named integrations in §Integration List), or any RBAC matrix entry that ChatBot's FRs reference. Editorial revisions, internal refactoring, and documentation-only changes are not material.

The **System Architect** owns the trigger for re-check; the Architect monitors the sibling repositories' release notes and PRD updates and opens a re-check task within 5 business days of any material change. The re-check produces either (a) a confirmation that ChatBot integration assumptions still hold, or (b) a list of required PRD/architecture/story updates with owners and a target completion date. The re-check outcome is appended to `.memlog.md` and refreshes `source-manifest.md` regardless of result.

The MVP has one administratively provisioned entitlement for an approved tenant's governed email-to-Project workflow. Mailbox, service-client, AI-actor, command-capability, and operating-limit controls are safety/governance controls, not commercial plan gates. MVP behavior, authorization, required audit retention, association, attachments, approval, CLI/MCP access, and failure handling cannot vary by subscription tier.

The MVP is not a general-purpose email client, CRM, helpdesk, workflow builder, enterprise search product, unrestricted AI platform, full project-management suite, public API marketplace, broad enterprise-admin suite, or billing system. It excludes subscription enforcement, arbitrary third-party integrations, cross-tenant Project discovery, implicit service-client elevation, broad customizable permissions, autonomous confirmation without human or configured policy approval, self-service provisioning, trials, and marketplace packaging. Future packaging may consider tenant, mailbox, Project, email, AI-action, retention, approval-policy, automation, channel, and analytics limits, but it cannot weaken tenant isolation, authorization, audit completeness, or fail-closed behavior.

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

Association confidence must be configurable and auditable. `T_high` and `T_low` are tenant-policy thresholds calibrated against the evaluation dataset and reviewed through audit evidence. Emails at or above `T_high` are eligible for automatic association only when required deterministic evidence is present and no conflict exists. Every other scorer outcome enters `NeedsReview`; `T_low` affects candidate ranking and presentation, not automatic disposition. No email may be silently associated when evidence conflicts, is stale, is unauthorized, or the scorer fails.

Authorization must be enforced at command and query boundaries across UI, CLI, and MCP. External participants, AI agents, and automation clients must resolve to scoped parties before accessing files, creating task requests, triggering commands, or sending outbound communication. Unauthorized projects must never appear as candidates, evidence, logs visible to the user, CLI output, or MCP response payloads.

Mailbox ingestion must tolerate duplicates, retries, and partial failures. Message intake, attachment storage, and task creation must use idempotency, duplicate detection, retry handling, and visible failure states so repeated delivery does not create conflicting project records.

The system must preserve auditability. Every durable mutation carries the FR81a canonical envelope; security-sensitive non-mutating denials and restricted reads use the separate auditable-attempt path. Records include the applicable actor, tenant, timestamp, source identity, decision/reason, evidence references, command surface, requester/approver, resource, policy, stable operation/decision identity, and result without becoming an unrestricted secondary data store.

### Measurable Outcomes

The increment gate in §Minimum Release Slice determines whether an increment can be released and which disable, rollback, and claim consequences apply. A11-M1 freezes each mandatory M1 definition, denominator, supported-request mix, target, sample/window, evidence source, owner, and pass/fail rule before observation; A11-M2 completes the production SLO catalog.

| ID | Measure and denominator | Target / status | Owner and gate evidence |
| --- | --- | --- | --- |
| SM1 — Final association correctness | Messages assigned to the adjudicated authorized Project / adjudicated messages with exactly one authorized in-scope Project. Safe abstention is separate; a wrong-Project result remains incorrect after correction. | `>= 95%` on the versioned A9a release dataset. | Test Architect; M0 release decision. |
| SM2 — Reassignment rate | Automatic associations later corrected or reassigned / all automatic associations, per tenant, rolling 7 days. | Diagnostic; cannot independently pass or fail an increment. | Test Architect. |
| SM3 — Review resolution | Ambiguous items reaching an authorized terminal decision / ambiguous items whose review due window ended, per tenant, rolling 7 days. | Diagnostic; cannot independently pass or fail an increment. | Test Architect. |
| SM4 — Review time | Median elapsed time from first `NeedsReview` to authorized terminal human disposition, per tenant, rolling 7 days. | Diagnostic; cannot independently pass or fail an increment. | Test Architect. |
| SM5 — Evidence sufficiency | Ambiguous items resolved using only presented evidence / all resolved ambiguous items, per tenant, rolling 7 days. | Diagnostic; SM9 supplies the pilot target. | Test Architect. |
| SM6 — Safe routing | Unresolved or unauthorized emails routed to visible review or failure / all such emails. | `100%`; M0 release decision. | Test Architect; M0 gate. |
| SM7 — Automatic-association precision/recall | Correct automatic associations / all automatic associations; and correct automatic associations / adjudicated unambiguous messages with exactly one authorized in-scope Project. | `>= 95%` precision and `>= 90%` recall; pass/fail, not a tenant SLO. At least `500` A9a messages for M0 and `2000` for M1 before each gate and quarterly afterward; closed populations, minima, adjudication, and confidence bounds follow `addendum.md` §Association Evaluation Protocol. [ASSUMPTION A9a] | Test Architect approves each gate and quarterly run. |
| SM8 — Pilot continuity | One pilot tenant uses the workflow for `4` consecutive weeks with the controlled M0 mailbox pattern, expands to at least `2` patterns by M1 exit, and represents at least `5` active Projects. | Starter M1 target until A11-M1 freezes evidence. [ASSUMPTION A11] | Product Lead; M1 gate. |
| SM9 — Evidence-led resolution | Ambiguous decisions resolved from presented evidence without manual context re-entry / all ambiguous decisions. | `>= 70%`; starter pilot target until A11-M1. [ASSUMPTION A11] | Product Lead; pilot outcome. |
| SM10 — Context lead time | Median email-to-governed-context time relative to the A11 baseline. | Reduction `>= 40%`. [ASSUMPTION A11] | Increment-gate owners; M2 gate. |
| SM11 — Manual-update reduction | Manual Project updates sourced from email for pilot Projects relative to the A11 baseline. | Reduction `>= 30%`. [ASSUMPTION A11] | Increment-gate owners; M2 gate. |
| SM12 — Governed review use | Completed AI-action reviews with approval, rejection, refusal, and audit outcomes visible in UI and at least one machine surface. | At least `10` in M0 and `30` cumulatively by M1; count/use contract retained in the A11 evidence bundle. | Product Lead; M1 gate. |
| SM13 — AI-action execution | Approved governed AI actions completed without command failure or rollback / all approved governed AI actions in the A11 window. | `>= 95%`. [ASSUMPTION A11] | Increment-gate owners; M2 gate. |
| SM14 — Attachment auto-handling | Attachments from associated email stored in the correct governed Project folder with classification metadata / all such attachments. | `>= 90%`; remainder enter review with an FR77 reason. [ASSUMPTION A11] | Increment-gate owners; M2 gate. |
| SM15 — Governed machine-surface adoption | Pilot workflows using CLI or MCP for a parity-set operation without bypassing authorization, approval, idempotency, or audit. | At least `1` by M1 exit. | Product Lead; M1 gate. |
| SM16 — Usable AI result | Governed AI results accepted without revision and then used by append, approved send, or allowlisted command / all governed AI results reviewed by pilot users. Report acceptance, revision-request, rejection, and abandonment separately. | `>= 60%` by M1 exit; cannot gate M1 until A11-M1 freezes mix, denominator, sample/window, source, owner, and rule. [ASSUMPTION A11] | Increment-gate owners; M1/M2 gates. |
| SM-C1 — Unauthorized association | Critical false-positive associations into a Project the sender is not authorized to read. | `0`. [ASSUMPTION A9a] | Test Architect; M0 gate. |
| SM-C2 — Unauthorized disclosure | Unauthorized Project names, evidence, files, or audit details disclosed on any surface. | `0`. | Increment-gate owners; M0 gate. |
| SM-C3 — Approval quality | Rubber-stamp approvals / reviewed approvals, rolling 7 days. | `<= 15%`; breach triggers workflow tuning, never approval bypass. | Increment-gate owners; M1 gate. |
| SM-C4 — Automation quality | Effect of SM10, SM11, or SM14 gains on SM2 and SM-C1. | Gains must not increase reassignment or critical false positives. | Increment-gate owners; M2 gate. |
| SM-C5 — Supported-request coverage | Supported request mix and supported, denied, unsupported, and abandoned rates reported beside SM16. | SM16 cannot improve by narrowing below the A11 baseline. | Increment-gate owners; M1/M2 gates. |

Association reporting additionally publishes safe abstention—correct review/no-association disposition over all messages expected not to auto-associate—separately for ambiguous, no-match, unauthorized, cross-tenant, and authenticity-anomaly partitions. Automatic and human-confirmed wrong-Project results are reported separately over all adjudicated messages; neither correction nor abstention removes a wrong result.

The gate outcome also requires deterministic auto-association only with required evidence, ranked evidence and explicit confirm/reject/defer choices for ambiguity, retained correction events, and no ambiguous attachment without confirmation. Unresolved senders cannot access or act on a Project, and cross-tenant access remains prohibited. Boundary-crossing AI effects require non-downgradable approval, while low-risk assistance remains policy-bounded. The gate also requires governed attachment storage, idempotent reprocessing, countable failures, and complete audit coverage for association, override, approval, command, attachment, retry, duplicate suppression, and AI action.

#### Cross-surface parity outcomes

M1 parity means UI, CLI, and MCP share backend state transitions for intake-status inspection, candidate review, association confirm/reject/defer/correct, attachment storage/status, task-intent capture/status, AI-action approval, approved-command execution, retry, operation status, and audit lookup. Presentation and event-driven initiation may differ, but candidate order/evidence, authorization, records, reason codes, redaction, idempotency, and audit outcomes are equivalent and contract-tested. Human decisions over MCP require a current human-delegated session, recorded presence, and `actorType=human`; AI/tool/service principals receive structural denial.

#### Validation outcomes

Validation uses a labeled corpus and a representative pilot mailbox sample covering deterministic and ambiguous matches, no match, unauthorized and cross-tenant references, duplicates, retries, attachments, external parties, and risky AI approval paths. Passing evidence includes correct association decisions, complete evidence display, and complete audit records across UI, CLI, and MCP.

The **Test Architect** owns association-quality measurement, and the **Product Lead** owns pilot-adoption measurement. Both report results before each increment release.

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

Including CLI and MCP in the MVP requires narrower scope elsewhere: no additional collaboration channels, full task lifecycle, broad document intelligence, commercial packaging, arbitrary integrations, or operations beyond the core governed email-to-project set. UI-only is insufficient because automation builders and AI agents are first-class actors. By the end of M1, pilot automation or AI-agent workflows must use CLI or MCP for at least one parity-set path without bypassing authorization, approval, idempotency, or audit behavior.

**M0 communication constraint:** M0 release notes identify CLI/MCP as M1 scope and do not claim cross-surface parity. The singular M1 parity contract must ship before the governed cross-surface pilot exits.

### Minimum Release Slice — Three Increments

The MVP is delivered in three increments within one release window. M0 is a controlled pilot-preview deployment, M1 is the governed cross-surface pilot, and M2 is the MVP production/release-candidate gate. A pilot deployment is not permission to claim MVP completion or production readiness. The named team (one product lead, one architect, backend/service engineers, one frontend engineer, one CLI/MCP engineer, one security/identity engineer, one QA/test architect, DevOps) sizes the per-increment scope. If any increment slips, breadth may shrink but dependency order and safety gates do not.

The three increments share a common non-negotiable: no increment is shippable if tenant isolation, authorization, fail-closed behavior (as contracted in NFR15a — see Reliability NFRs), idempotency (per the per-operation-class table in `addendum.md`), audit completeness (per NFR50a), or safe AI approval behavior is removed. If resources are constrained, trim dashboards, advanced mailbox inference, advanced approval-policy flexibility, document-intelligence breadth, and UI polish before trimming any increment's safety floor.

| Increment gate | Audience/environment | Mandatory approval evidence | Owner and approver | Disable/rollback condition | Permitted claim |
| --- | --- | --- | --- | --- | --- |
| M0 | Named internal users in one configured tenant and one controlled mailbox pattern | End-to-end UI loop; `approved-current` A5 AI-provider gate record before live AI use; `approved-current` A6 data-class gate record before onboarding; `approved-current` A13 gate record covering Conversations append/concurrency and assignment ownership, Conversations/Folders correction commands and acknowledgements, closed Tenants/EventStore/Projects authority mapping, atomic-audit ownership, and supported-write-path ACL/fencing proof; `approved-current` A9a M0 TaskIntentDetector (precision ≥80%, recall ≥75%) and ActionRiskClassifier (misclassification ≤1%, zero boundary-crossing false-low-risk) qualification records before first use; M0 governance bootstrap; M0 authenticity; first-store isolation; authorization, atomic audit, idempotency, retry-profile conformance, WCAG; SM1/SM6/SM7/SM-C1/SM-C2 | Product Lead + Test Architect; Security + Architecture approve A5 and safety evidence; Compliance/Data Protection + Architecture approve A6; System Architect plus Conversations, Folders, Projects, Tenants, and EventStore owners approve A13, with Security validating authority and fencing; Test Architect independently approves A9a artifact qualification | Any required A5/A6/A13/A9a gate record whose status is not `approved-current` disables the affected live-AI or detector/classifier artifact and pilot onboarding; any tenant leak, unauthorized disclosure/mutation, unaudited mutation, audit-stream fork, or critical association false positive disables intake/AI execution | Controlled pilot preview only |
| M1 | Approved pilot users and automation clients on the governed cross-surface environment | Revalidated `approved-current` A5/A6/A13 gate records and retry profiles; `approved-current` A9a M1 TaskIntentDetector (precision ≥90%, recall ≥85%) and ActionRiskClassifier (offline misclassification ≤1%, production-sampled disagreement ≤2%) qualification records; `approved-current` A11-M1 metric record; singular parity tests including AI/service-principal denial of human decisions; exact AI allowlist; governed composer; service-client, outbound, policy-schema, WCAG; SM8/SM12/SM15/SM16/SM-C3/SM-C5 | Product Lead + System Architect + Test Architect; Security, Compliance/Data Protection, and affected producer owners confirm inherited approvals; Security approves programmable surfaces; Test Architect independently approves A9a artifact qualification; Product Lead and Test Architect approve A11-M1 | Any required A5/A6/A13/A9a/A11-M1 gate record whose status is not `approved-current` blocks onboarding and disables the affected detector/classifier or live AI; parity divergence, command-spine bypass, policy drift, self-approval, or unsafe outbound disables the affected surface/command | Governed cross-surface pilot only |
| M2 | Production-shaped multi-tenant release-candidate environment | Revalidated `approved-current` A5/A6/A13/A11-M1 gate records; `approved-current` A9a TaskIntentDetector and ActionRiskClassifier qualification records bound to the exact deployed M2 candidate and dependencies, retaining the M1 thresholds and current production-sampled result; `approved-current` A10 recovery and A11-M2 SLO gate records; replay/isolation, audit reconstructability, operability; SM10-SM14/SM16/SM-C4/SM-C5 | Product Lead + System Architect + DevOps + Test Architect; Security, Compliance/Data Protection, and affected producer owners approve; Test Architect independently approves A9a artifact qualification | Any required A5/A6/A13/A9a/A10/A11-M1/A11-M2 gate record whose status is not `approved-current` blocks production release and disables the affected artifact, store, or surface | MVP production/release candidate |

#### Increment M0 — Vertical Thesis Path (UI-only)

M0 proves one complete email-to-governed-action loop end-to-end, in the UI, for one tenant. Qualified deterministic association is automatic and audited; humans drive every ambiguous association and every risky AI-action decision.

- One controlled Microsoft 365 / Exchange mailbox pattern for one configured tenant, with stable message identity, conversation/thread identifiers, attachment metadata, and delivery/retry state.
- Minimum inbound-authenticity floor: provider DMARC/DKIM/SPF verdict passthrough, required-header discrepancy capture, delegated-sender evidence, external-sender posture, safe `strict` default, and fail-closed review/block routing (FR48a–FR48d).
- Deterministic association using explicit project identifier, mailbox routing rule, or conversation/thread identifier (the three signals listed; learned/inferred matching is out of scope for M0).
- Ambiguous association review in the UI: candidate projects with ranked evidence; user can confirm, reject, defer, or correct a prior association.
- Governed attachment capture into Hexalith.Folders with metadata, status, and quarantine of unsafe attachments.
- One task-intent and AI-action path: detect a candidate action from associated project email, package authorized project context, classify risk via the heuristic + tag classifier (see `addendum.md` §Risk Classifier), route approval-required work to a human reviewer, and execute through one allowlisted command (see M0 allowlist in `addendum.md` §Command Allowlist v0).
- Keycloak-backed identity and tenant scope; fail-closed authorization enforced at the command/query boundary per NFR15a.
- FR81a-compliant M0 governance bootstrap: two distinct current Tenants `TenantOwner` principals create the first immutable ChatBot admin grant, M0 policy snapshot, and four M0 service-client grants through the stable commands in §Shared Workflow Contract. Security, Compliance/Data Protection, and owner approvals remain additionally required where the M0 gate or schema names them. No first version is created by direct data seeding.
- M0 association states: Received, Associated, NeedsReview, Deferred, Rejected, Failed, Skipped, Correcting, CorrectionDelayed, Corrected. `Skipped` is in M0 because duplicate suppression and out-of-scope mailbox rules require a terminal safe state.
- Required audit events for M0 ops: message intake, candidate generation, association decision, attachment handling, AI action proposal, approval decision, command execution result, retry/duplicate suppression, correction.
- Native-store and API negative tenant-isolation tests for every M0 record class, plus actor tests for human user, tenant admin, project owner, background worker, M365 event, and AI actor. No M0 store may be piloted until its below-application isolation proof passes.
- A6-approved runtime controls for purpose/lawful-basis metadata, retention, legal hold, export, deletion/erasure, backup propagation, production key custody, and surviving-metadata proof for every M0 data class. Missing runtime evidence blocks persistence and pilot onboarding.
- Dependency failure handling for M0 dependencies: M365, Keycloak, Hexalith.Projects/Folders/Parties, EventStore, attachment scanner, AI service.
- WCAG 2.2 AA conformance applies only to the M0 UI surfaces enumerated in NFR60: inbound authenticity review, ambiguous association review, AI action approval, and the project conversation view that hosts them. Later increments inherit the bar as their surfaces are added.

M0 is not shippable without every item listed above for Increment M0. Outbound communication, CLI, MCP, multi-tenant rollout, and operational dashboards are explicitly out of scope for M0.

#### Increment M1 — Cross-Surface Parity & Full Governance

M1 extends the M0 loop across surfaces and completes the governance model that the parity bet depends on.

- CLI and MCP parity for the singular exit set: intake-status inspection, candidate review, confirm/reject/defer/correct association, attachment storage/status inspection, task-intent capture/status, AI-action approval, approved-command execution, retry, operation status, and audit lookup. Human-decision operations require a human-delegated session with current authentication and recorded user presence; AI/tool/service MCP principals are structurally denied those mutations.
- Cross-surface parity enforced by a single shared command pipeline at the architectural layer (see FR81a and `addendum.md` §Shared Command Pipeline). Contract tests verify the invariant; they do not enforce it.
- Service-client permissions and Keycloak service-account flows.
- One outbound draft-and-send path that preserves sender authority (per FR48 + `addendum.md` §Authority class mapping), recipients, approved content, and audit history.
- Full state-transition matrix expansion per §Association Lifecycle and States.
- Per-tenant, per-role, per-project, per-action-type, per-recipient, per-risk-class approval policies — surfaced through the Tenant Policy Schema in `addendum.md` §Tenant Policy Schema.
- Tenant-admin permission model as its own FR group (FR75a–FR75g): what admins can see, what they can operate on, the audit obligations attached to admin actions, and the absence of any bypass to authorization or audit.
- Versioned command allowlist artifact under change control (see `addendum.md` §Command Allowlist v1).
- Governed interactive composer (`S1a`) in FrontComposer: message admission through CommandGateway, attributed response/proposal, safe streaming, stop/cancel, idempotent retry, typed failure, and conversion of every risky request into a mandatory-approval proposal.
- Risk-classifier mechanism named, calibrated against the evaluation dataset (see A9a), with a stated misclassification fallback and audit chain when classification disagrees with reviewer action.
- Native-store/API isolation tests for every M1 record and machine surface, plus actor tests for CLI client, MCP client, and service client. Customer multi-tenant rollout remains blocked until the M2 gate.
- Advanced inbound-authenticity policy tuning and broader controlled-provider compatibility; the minimum authenticity floor is already mandatory in M0.
- WCAG 2.2 AA evidence for the governed composer, correction, outbound approval, tenant policy, and M1 admin surfaces.

#### Increment M2 — Operations, Recovery, Continuity

M2 makes the system operable in production by tenant administrators and Hexalith ops, with recovery and continuity guarantees.

- Operational dashboards for mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, and audit lag.
- Recovery: RPO ≤ 15 min and RTO ≤ 4 hr remain provisional (A10). The accepted Epic 12 recovery-provenance architecture is still `activation: pending`; its transition-completion check and artifacts cannot satisfy A10. There is no current qualifying hosted four-job bundle; fresh exact-candidate controlled-loss evidence and an RTO-capable full-window or production-shaped drill are stop-ship M2 gates. Current details live in `qualification-evidence.md`.
- Replay/simulation denies production credentials/resources at composition time, replaces every effectful adapter, enforces egress denial, and proves before/after invariance across production stores and external-resource ledgers (FR95a and `addendum.md` §Replay Isolation).
- Durable mutations and decisions use stable `operation_id` / `decision_slot_id` identities with lifetime deduplication and expected-revision concurrency; time-window hashes apply only to safe non-mutating proposal suppression.
- WORM/retention hardening and signed per-tenant checkpoints protect the canonical per-aggregate envelopes already committed with each mutation; no secondary ledger may replace the FR81a atomic boundary (see NFR49a).
- Audit completeness as a production observable: the fraction of state-mutating operations whose audit chain reconstructs the operation end-to-end ≥ 99.5% per rolling 7-day window (see NFR50a).
- Native-store isolation tests for M2 vector indexes, embedding stores, and prompt-context caches, recurring production probes for all record classes, and the first authorized multi-tenant rollout gate (FR55a / NFR9a).
- WCAG 2.2 AA conformance for M2 operational, compliance, and queue-operation surfaces. CLI and MCP are outside WCAG scope.
- Complete A11-M2-calibrated SLO catalog with numeric targets, error budgets, live signal provenance, alert routing, and burn tests. Unsupported rows block the corresponding production-readiness claim.

Architecture and epics must preserve the increment dependency order and the sole gate table above: M1 starts only after the M0 gate passes and cannot exit until A11-M1 is approved-current; M2 starts only after the M1 governed-pilot gate passes and then revalidates A5/A6/A13/A9a/A11-M1 while also closing A10 and A11-M2. Within an increment, internal ordering is an architecture concern.

Out of scope for MVP:

- Autonomous project creation from email.
- General email client replacement.
- Full task lifecycle management.
- Full document intelligence over attachments.
- General user-upload ingestion outside governed mailbox attachment capture.
- General non-project "subject" workspaces that cannot resolve to a governed Project boundary.
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
- General user-upload ingestion into governed Project folders, after mailbox attachment capture proves the authorization, provenance, and retention model.

### Vision (Future)

The long-term vision is a governed AI-native project collaboration layer for the Hexalith ecosystem. Email is the first wedge, but the broader product becomes a multi-channel workspace where internal users, external participants, automation, and AI agents collaborate around durable project context.

In the vision state, Hexalith.ChatBot provides reusable project-aware AI workers, multi-channel conversation capture, governed task execution, audit-ready action history, document intelligence, reusable MCP tools, and consistent command access across human and machine surfaces. The product becomes the safe operating boundary where enterprise collaboration and agentic automation meet.

The governed interactive chat surface is an M1 MVP write surface delivered through FrontComposer. Every submission enters CommandGateway; read-only/no-external-effect assistance may return an attributed response, while any boundary-crossing request becomes a mandatory-approval proposal. There is no ungoverned freeform write path. Epic 10 owns the implementation handoff; this PRD owns the behavior and gate.

### Delivery Strategy, Resources, and Risk Controls

The MVP is one release window with dependency-ordered increments: M0 proves the fail-closed UI vertical, M1 proves structural UI/CLI/MCP parity and full governance, and M2 proves production operations, recovery, audit checkpoints and availability, idempotency revalidation, replay isolation, and cross-tenant cache/vector isolation. §Minimum Release Slice is authoritative: M1 starts only after M0 passes, M2 starts only after M1 passes, and no summary may defer an earlier control or weaken a gate.

The named team is a Product Lead, System Architect, backend/service engineers (plural; reducing this creates a single point of failure), one frontend engineer, one CLI/MCP engineer, one security/identity engineer, one QA/Test Architect, and DevOps support. It is sized for the three increments, with WCAG scope limited to each increment's delivered surfaces. Team growth may compress time but cannot change dependency order or safety evidence.

| Risk | Required mitigation |
| --- | --- |
| Incorrect Project association | Deterministic evidence, calibrated thresholds, visible candidate review, fail-closed routing, correction, seeded evaluation data, and zero critical unauthorized false positives. |
| Tenant or permission leakage across UI, CLI, MCP, clients, workers, indexes, or projections | Keycloak-derived tenant scope, command/query authorization, no CLI/MCP data-plane bypass, scoped candidates/evidence, negative isolation tests, and auditable denials. |
| Unreliable mailbox integration | Stable message identity, idempotency, duplicate suppression, retry and degraded-health states, M365 permission handling, and failure queues isolated from Project state. |
| Unsafe AI action or untrusted source context | Immutable origin/instruction boundaries for email, threads, attachments, filenames, retrieved context, and tool results; scoped context, allowlists, determinate risk classification, human approval for boundary-crossing effects, refusal, and complete proposal/decision/outcome audit. |
| Governance friction | Evidence-led review, low-friction ambiguity resolution, grouping/prioritization, and measured approval quality without bypass. |
| External-participant adoption | Preserve email as the external channel and resolve participants through Hexalith.Parties without requiring an MVP portal login. |
| Unproven UI/CLI/MCP value | Constrain parity to the singular governed operation set and measure real machine-surface adoption without governance bypass. |
| Capacity or schedule pressure | Move schedules before trust controls. Trim dashboards, advanced inference, flexible policies, rich document intelligence, analytics polish, and non-essential UI polish before isolation, association correctness, authorization, audit, idempotency, or fail-closed behavior. |

M0 alone is a controlled thesis proof, not MVP completion; the MVP is not shippable until M2 closes. Any slip or gate-scope change is appended to `.memlog.md`, never silently absorbed. Growth Features, Vision, §Traceability Overview, and the explicit exclusions in §Minimum Release Slice define the complete scope boundary.

## User Journeys

### Journey 1: Business Contributor Requests AI Help From a Project Conversation

Amira is a business contributor helping move a customer delivery project forward. The project conversation contains internal discussion, messages from external parties represented through Hexalith.Parties, and email-derived updates with attached documents. A new project email enters the workspace, Hexalith.ChatBot associates it with the project using deterministic evidence, and the evidence is visible before Amira asks the AI for help.

Before Hexalith.ChatBot, Amira would have copied the thread into a separate AI tool, downloaded attachments, searched the project folder for the latest documents, and manually checked whether the response was safe to send. She would have moved faster, but with weak traceability and a meaningful risk of using stale or unauthorized context.

In Hexalith.ChatBot, Amira opens the project conversation and sees the message associated with the project. The external sender is resolved as a party, the attachments are linked to governed project folders, and the system shows why this email belongs in this project. Amira expects the AI to understand the project without re-explaining the thread, but she also needs to know whether the AI is using approved context or guessing.

In M1, Amira submits the request through the governed composer. The UI immediately shows admission state and attribution; a safe read-only response may stream with stop/cancel and retry controls. Her request to compare files and draft external communication crosses file and outbound boundaries, so the system creates a proposed action with visible Project scope, requester identity, input files, command, expected output, and risk classification. The action cannot execute until an authorized reviewer approves it.

The value moment is controlled acceleration. Amira can review the proposed AI action inside the same project conversation, approve it, reject it, or request changes. After approval, the action executes through the governed command model, and the result is recorded back into the project conversation with audit history.

If the AI lacks sufficient context, the system asks for additional files or clarification instead of fabricating an answer. If approval is rejected, the rejection reason remains visible in the conversation. If command execution fails, Amira sees a clear failure state and retry path without losing the original request.


### Journey 2: Business Contributor Resolves an Ambiguous Project Association

Marc is an authorized project contributor responsible for resolving ambiguous project communication for projects he can access. A message arrives from a known external party, but that party participates in multiple active projects. The subject line references a shared initiative, and the attachments could plausibly belong to more than one workspace.

Hexalith.ChatBot does not attach the message automatically. It presents candidate project rows with confidence state and evidence: sender or recipient party match, thread references, project alias or identifier, subject/body signals, attachment names or metadata, prior associations, conversation participants, prior corrections, and reason labels. Marc can confirm a candidate, choose a different project, reject all candidates, defer the decision, or escalate/manual review.

The critical moment is trust. Marc does not need to search mailbox history or compare project records manually. The system gives him enough evidence to choose, records the decision as an auditable association event, and preserves the original email context.

If no candidate is viable, the message remains in a visible unresolved state instead of disappearing or contaminating a project workspace. If multiple candidates are equally likely, the system names the uncertainty instead of hiding it. If Marc chooses the wrong project and later corrects it, the correction is recorded with actor, time, previous association, new association, and reason. That correction can inform future association evaluation, but it must not automatically authorize unsafe future matches.


### Journey 3: External Party Sends Project Context Into Hexalith

Elena is an external party represented in Hexalith.Parties. She may be a customer, supplier, partner, or any organization/contact participating in a project. In the MVP, she does not authenticate into Hexalith.ChatBot, does not receive a tenant account, and does not need to learn a new collaboration tool.

Elena sends an email with a decision, request, and supporting attachment to the project mailbox or controlled project email pattern. From her perspective, the workflow still feels like normal external collaboration. She expects the receiving team to understand the context, use the right documents, and respond responsibly.

Hexalith.ChatBot resolves Elena as a party, checks tenant and project scope, evaluates project association signals, and stores the message and attachment only if the project context is safe enough. Inbound permissions are derived from project email collaboration rules and scoped party relationships. If association is ambiguous, an internal authorized user chooses the correct project. If Elena is not authorized for the project, if her identity cannot be resolved, or if sender spoofing/mismatch is detected, the system fails closed and does not expose candidate projects, files, or internal context.

The value moment is invisible governance. Elena can keep using email, while the internal team gains structured project context, governed file handling, and auditable follow-up. If her identity is unresolved, the internal user sees an unverified external party state with evidence and can link to an existing party, create/link a pending party, reject, or quarantine according to policy.


### Journey 4: Project Owner Corrects a Wrong Association

Priya is the project owner for a sensitive delivery project. She notices that an email-derived conversation item has been associated with her project, but the message actually belongs to another workspace. The mistake matters because the message has already generated candidate task intent and may have influenced AI context preparation.

Priya opens the association details and sees the original evidence, actor, timestamp, confidence state, candidate projects shown, and a correction impact manifest covering every downstream record and effect created from the association. She corrects the association or marks it as misfiled only with current authority on both the source and destination Projects. The system keeps the prior association audit-visible. Through Hexalith.Conversations and Hexalith.Folders, it requests owner-governed conversation reassignment and attachment reassignment, quarantine, revocation, or governed-copy compensation. It refreshes or invalidates AI context indexes and records an owner acknowledgement or explicit irreversible-effect disposition for every manifest item.

The value moment is accountable repair. Priya can see whether each consequence was repaired, contained, requires compensation, or cannot be repaired; sent mail, completed external/tool actions, appended messages, and prior file disclosure are never falsely described as reversed. The system remains `Correcting` or `CorrectionDelayed` and prevents AI use of every affected source/destination context until the manifest is complete.


### Journey 5: Tenant Admin Configures Governed Email Collaboration

Nora is a tenant admin responsible for making external collaboration safe. She configures controlled mailbox patterns, party resolution rules, project association signals, tenant policies for low-risk AI assistance, MVP approval rules for externally visible or project-mutating output, and audit visibility rules.

Her concern is not only whether the workflow works when everything is clean. She needs confidence that the system behaves safely when senders are unresolved, messages are duplicated, attachments are renamed, projects share similar names, an external party participates in multiple projects, or a dependency is unavailable.

Nora reviews operational views for unresolved parties, ambiguous project matches, duplicate message suppression, rejected associations, approval queues, failed command executions, and accuracy/correction metrics. She verifies that unauthorized projects never appear as candidates, evidence, CLI output, MCP payloads, or audit details to users who cannot access them.

The value moment is operational confidence. Nora sees that mailbox ingestion, party resolution, authorization, approval, and audit records are tenant-scoped and policy-driven. When a message cannot be safely resolved, it enters a visible review or failure state instead of being silently attached, discarded, or executed.


### Journey 6: Developer Uses CLI To Inspect and Resolve Project Email Workflow

Leo is a developer and automation builder supporting project operations. He uses the CLI to inspect unmatched or ambiguous project emails, view candidate projects and evidence, resolve an association, check attachment storage status, trigger an approved command, and verify audit output.

Leo does not expect the CLI to mimic the chatbot UI. He expects operation parity: the same command model, authorization rules, candidate list, evidence fields, resolve/reject/defer/correct actions, permission failures, state transitions, and audit results as the UI and MCP.

He runs a CLI command to list unresolved associations, inspects the candidate project evidence, selects the correct project, confirms attachment status, and checks the resulting audit record. When a retry is needed, the CLI exposes retry status without creating duplicate conversations, files, or task requests. If a CLI command succeeds but an audit projection is delayed, the CLI returns a clear partial-success state rather than implying the system is fully reconciled.

The value moment is repeatability. Leo can script governed operational tasks without bypassing authorization, approval, and audit requirements. The CLI becomes a trusted automation surface because it shares the same backend behavior as the user-facing experience.


### Journey 7: Compliance or Support Reviewer Investigates a Risky Action

Sofia is reviewing a reported concern: an AI-assisted response may have used the wrong project context. She needs to reconstruct what happened without relying on screenshots or informal explanations.

She opens the audit history and sees the source message ID, tenant, project, requester, party identities, candidate project evidence, selected association, rejected alternatives, input files, prior decisions that influenced the action, approval policy, approval decision, command surface, model/agent identity, executed command, timestamp, output, destination, and result. If the action was rejected, deferred, retried, corrected, or duplicate-suppressed, that state is visible as well.

The value moment is reconstructability. Sofia can determine who initiated the action, which project context was used, which emails, attachments, parties, and prior decisions influenced it, what candidates or alternatives were rejected, what policy applied, what model or agent acted, what output was produced, and where it went.

If Sofia lacks permission for a project, the system does not expose project names, candidate evidence, files, or sensitive audit details. Investigation access is powerful, but still tenant- and role-scoped. Sofia can investigate and escalate, but she does not necessarily have authority to mutate project association or project state.


### Journey 8: User Reviews an AI Action Before It Leaves the Project Boundary

Amira asks the AI to prepare a response that may include project file content and be sent to an external party. The AI response is ready, but the system pauses before anything leaves the project boundary.

Amira sees the context used, the files referenced, the proposed action, the recipient or destination, the policy rule that triggered review, and the expected command. She can approve, reject, request revision, or cancel. The interface names the risk in plain language: the action is externally visible, file-exposing, project-mutating, tool-invoking, or participant-representing.

If Amira asks the AI to do something outside project boundary or policy, the AI refuses or routes for approval. No external email, project mutation, file exposure, or tool invocation occurs until the required authorization path succeeds. The denial or approval decision is audited with reason and policy rule.


### System Journey: Governed AI Execution

A project-aware AI agent receives a request from a conversation actor or command surface. The request asks it to analyze project files, summarize context, draft a response, classify an incoming request, or prepare a command for execution.

The AI agent does not receive an unbounded workspace. It receives Project scope, requester identity, authorized input files, tenant policy, permitted action types, current approval requirement, evidence source traceability, and immutable origin/trust labels. Email bodies, quoted threads, attachments, filenames, retrieved context, and tool results remain untrusted data and cannot define system/tool instructions, authorization, or approval. It can perform low-risk read-only assistance if policy allows, but it must create a proposed action for risky operations.

The value moment is enforceable agency. The AI can help move work forward, but it cannot silently cross tenant boundaries, access unauthorized files, send outbound communication, mutate project state, or invoke external tools without the required authorization and approval path.

If association is unresolved, the AI refuses project-specific action or asks for association resolution. If required context is missing, the AI asks for clarification or additional files. If authorization fails, the action is denied and audited. If approval is required, the AI waits for a human decision. If execution succeeds or fails, the result is recorded through the same command/event model as human and CLI actions.


## Product Surfaces and Ownership

### Context Ownership

This section defines the PRD's authoritative ownership boundaries. The Executive Summary and Shared Workflow Contract refer to these boundaries. ChatBot's first-class durable records are detailed in §Data Governance Surface.

- Hexalith.ChatBot owns AI-mediated collaboration workflows, user-facing assistant interactions, and the derived records enumerated in §Data Governance Surface (associations, candidate rankings, evidence snapshots, AI action proposals, approval records, policy snapshots, inbound-authenticity records, correction impact manifests, projections, lifecycle, vector indexes, replay traces, queue projections). It owns manifest orchestration and acknowledgements, not the Conversations/Folders records or effects listed by a manifest.
- Hexalith.Projects owns Project identity, Project access/membership, lifecycle, and Project authorization. It does not own conversation messages or a mutable conversation-membership list.
- Hexalith.Conversations owns conversation identity, messages, append/history semantics, its message event stream, and conversation-to-Project assignment/reassignment. Projects may authorize or orchestrate that assignment but cannot dual-write it. The allowlisted product ID `Project.AppendConversationMessage` maps to this owner despite its legacy prefix; its exact current mapping and A13 gap are in `addendum.md` §AI allowlist executable contract mapping.
- Hexalith.Parties owns internal and external participant identity resolution.
- Hexalith.Folders owns governed project folders, attachment storage, file access control, and file metadata.
- Hexalith.Tenants owns tenant facts, tenant boundaries, tenant policies, and authorization context.
- Hexalith.EventStore owns its current durable command/event gateway and envelopes. Ownership of the proposed atomic canonical mutation-audit envelope is not present in the pinned public contract and must be accepted by EventStore or assigned to an approved transactional owner under A13 before M0. ChatBot owns rebuildable investigation projections, not a post-commit substitute for canonical mutation audit.
- Mail integration owns message capture, headers, attachments, delivery state, and Microsoft 365 / Exchange synchronization concerns.
- Other contexts consume decisions through published contracts rather than duplicating decision logic.

### Data Governance Surface (Hexalith.ChatBot first-class durable records)

ChatBot does not own the source records (project, party, message, file), but it owns substantial derived state that is security-sensitive, retention-governed, and tenant-scoped. Architecture and operations treat the following records as ChatBot-owned, with full audit, retention, redaction, and isolation obligations:

| Record class | Source | Retention class | Redaction sensitivity | Isolation surface | Owner increment |
|---|---|---|---|---|---|
| Association record | derived from mailbox event + Project ID | A6-approved association-record period; no pre-approval default | high (carries Project name + sender identity) | per-tenant store partition | M0 |
| Candidate ranking | derived from `AssociationScorer` + evidence | A6-approved minimized candidate-evidence period; no pre-approval default | medium (carries evidence references) | per-tenant store partition | M0 |
| Evidence snapshot | extracted from message + provider headers | bound to associated message retention | high (carries message content fragments) | per-tenant store partition | M0 |
| AI action proposal | derived from `TaskIntentDetector` output and governed user request | A6-approved AI-proposal period, bounded by source retention | high (carries proposal text, file references) | per-tenant store partition | M0 |
| Approval record | derived from reviewer decision | audit retention | high (carries reviewer identity + reason) | per-tenant store partition | M0 |
| Projection | derived from event stream | rebuilt on demand from EventStore | inherits source class | per-tenant store partition | M0 |
| Policy snapshot | derived from Tenant Policy Schema state; M0 contains only the immutable bootstrap and M0 knobs, while M1 adds editor-managed/M1 rows | audit retention with A6 export/erasure treatment | medium (no PII; carries policy values) | per-tenant store partition with M0 native-store isolation proof | M0 |
| Inbound-authenticity record | derived from provider verdicts, inspected headers, delegation evidence, external-sender posture, policy version, reviewer decision, and successor links | A6-approved source-message/intake period with legal-hold, export, erasure, backup-propagation, and surviving-metadata treatment | high (carries mailbox identity, sender/header evidence, and reviewer identity; mailbox-only redacted query) | per-tenant/mailbox partition with M0 native-store/API isolation proof | M0 |
| Correction impact manifest | immutable version derived from predecessor association, source/destination Project authority, affected ChatBot records, owner-context records/effects, repair/compensation evidence, irreversible-effect dispositions, and authenticated acknowledgements | A6-approved correction/audit period with explicit legal-hold, export, erasure, backup-propagation, and surviving-metadata treatment; no pre-approval default | high (carries Project, message, file, recipient, action/effect, authority, and owner-acknowledgement evidence) | per-tenant partition with M0 native-store/API isolation proof; source/destination Project authorization on item access | M0 |
| Lifecycle state | derived from workflow events | bound to workflow retention | low | per-tenant store partition | M0 |
| Workflow instance map (predecessor → successor) | derived from terminal-state reprocessing | audit retention | low | per-tenant store partition | M0 |
| Vector index / embedding store / prompt-context cache | derived from Project files + conversations | configurable per `ai-context.retention` | high (derived material can leak source content) | per-tenant store partition enforced at the store layer per FR55a / NFR9a | M2 |
| Outbound trace store (test-tenant only) | derived from replay/simulation per FR95a | bounded by test-tenant retention policy | medium | test-tenant only; nightly probe asserts no production presence | M2 |
| Approval queue / failed-association queue / unresolved-party queue (operational views) | projections | rebuilt on demand | inherits source class | per-tenant store partition | M1 |

Each record class carries tenant ID, source provenance, derivation contract version, redaction state, and an A6-approved retention class. The retention, legal-hold, export, deletion, backup-propagation, and proof controls for a class exist before that class first persists; M2 may add operator configuration but cannot retroactively legitimize M0/M1 storage. Partitioning and native-store negative isolation tests are required in the first increment that introduces each class; a store cannot enter multi-tenant use or surface through CLI/MCP/service clients before that proof passes. M2 adds recurring production probes and the M2-specific vector/embedding/prompt-cache proofs. Deletion follows the NFR49a retention path; mutation is restricted to authorized retention workflows.

### UI Surface Inventory (handoff to UX)

The eight journeys plus the System Journey imply distinct UI surfaces. The PRD does not author the inventory in detail — that work belongs to `bmad-ux`, which will produce a per-surface UX specification. The PRD enumerates the surfaces here to give UX an unambiguous starting list and to pin the per-increment NFR60 scoping.

**M0 UI surfaces (required for M0 release):**
- **S1 — Project conversation view** (UJ1, UJ3): renders email-derived messages, parties, attachments, decisions, approvals, AI outcomes; hosts the `informational` / `actionable` classification badges from FR26 and the AI-summary distinction from FR27.
- **S2 — Ambiguous association review** (UJ2, UJ3): candidate-list, evidence panel (FR23), confirm / reject / defer / correct affordances.
- **S2a — Inbound authenticity review** (UJ3): mailbox-scoped redacted provider/header/delegation evidence and `GetInboundAuthenticityStatus` for `AuthenticityReviewRequired`; a current `mailbox-admin` initiates accept/reject or terminal reprocessing and an independent current `policy-admin` approves the exact evidence digest and decision. Blocked/rejected items expose reason, owner, retention, and successor guidance without candidate Project data.
- **S3 — AI action approval** (UJ1, UJ8): proposed-action surface per FR42 acceptance bullets — command, input files, recipients, sender authority, risk classification, policy snapshot, expected outcome, approve / reject / request-revision / cancel.

**Pre-pilot governed operator surface (required before first persisted pilot data; not a general UI):**
- **O1 — Data-rights operations API/provisioning interface** (FR58/A6): restricted to `compliance-admin` plus the required independent current TenantOwner approval. It exposes request/status/result, owner-by-owner partial completion, legal-hold precedence, rejection/appeal guidance, expiring redacted result delivery, retry, and audit. It is not part of CLI/MCP parity and grants no direct owner-context mutation authority.

**M1 UI surfaces:**
- **S1a — Governed chat composer** (UJ1, System Journey): submits through CommandGateway, displays admission/attribution, streams safe responses, supports stop/cancel and idempotent retry, converts risky requests into mandatory-approval proposals, and renders typed failure/audit outcomes (FR28a–FR28f).
- **S4 — Correction surface** (UJ4): correction affordance with `correcting` state from FR91a, predecessor display.
- **S5 — Tenant admin configuration** (UJ5): Tenant Policy Schema editor (per `addendum.md` §Tenant Policy Schema), mailbox configuration, approval-policy configuration, allowlist version pin, two-person-rule confirmation for security-sensitive knobs.
- **S6 — Outbound approval** (UJ8 outbound extension): pre-send approval surface for outbound communication.
- **S7 — Cross-surface attribution view** (UJ6): which surface originated each action (FR85).

**M2 UI surfaces:**
- **S8 — Operational dashboards** (UJ5 expanded, UJ7): mailbox processing, failed associations, approval queues, duplicate handling, AI action outcomes, audit projection lag, SLO/error-budget status from NFR42a.
- **S9 — Compliance investigation** (UJ7): audit search, evidence reconstruction, replay event distinction from FR95a, per-project redaction.
- **S10 — Admin queue operations** (FR75c): pause/resume mailbox, client, or opaque queue partitions and request investigation. Per-item retry/requeue/quarantine/dismiss controls appear only to actors with current Project authority.

CLI and MCP surfaces (M1) are not UI; they are addressed by §CLI and MCP Parity Boundary and do not participate in NFR60.

## Domain-Specific Requirements

### Compliance & Regulatory

Hexalith.ChatBot must support GDPR/EU data protection expectations for email-derived project context, attachments, external party records, AI action history, and audit records. The product must define what personal data is captured from email and project conversations, why it is processed, how long it is retained, who can access it, and how tenant administrators can respond to deletion, export, and correction requests where legally applicable.

External participants represented through Hexalith.Parties may include names, email addresses, organizations, domains, message metadata, attachments, and project participation history. These records must be tenant-scoped and access-controlled. The system must avoid exposing external-party details, candidate projects, project names, files, evidence snippets, or audit details to users who are not authorized for the relevant tenant and project.

Audit records must be complete enough to reconstruct governed decisions, but they must not become an unrestricted secondary data store. Audit views must enforce the same tenant, role, and project visibility rules as operational workflows. Retention and deletion behavior must be explicit for message content, attachments, association evidence, AI prompts/outputs, approval decisions, and immutable event records.

### Technical Constraints

Security must be enforced at command and query boundaries, not only in the UI. UI, CLI, and MCP must share the same authorization model, tenant scope, party/project resolution rules, and audit requirements. All project association decisions, AI actions, approval decisions, file access, outbound communication, and administrative operations must be attributable to an authenticated user, automation client, external party record, or governed AI actor.

Tenant isolation is non-negotiable. Project candidates, evidence snippets, party records, attachments, command outputs, CLI responses, MCP payloads, and audit views must never leak cross-tenant or unauthorized project information. Ambiguous association must fail closed when tenant, project, party, or authorization scope cannot be established.

AI actions must operate inside explicit project boundaries. The system must track requester, project scope, input files, proposed command, policy decision, approval state, execution result, and output destination. Risky actions must require review before modifying project state, exposing file content, sending outbound communication, invoking external tools, or acting on behalf of a participant.

Email ingestion must treat mailbox delivery as unreliable. The system must support stable operation identity, duplicate suppression, retry tracking, partial failure states, attachment security checks, and reconciliation between source messages and Project context. Canonical audit-durability failure must fail closed for every state mutation.

### Microsoft 365 / Exchange Integration Requirements

The MVP must support controlled Microsoft 365 / Exchange-style mailbox integration patterns for project email collaboration. Integration design must respect tenant boundaries, mailbox ownership, delegated access, shared mailbox patterns where applicable, and enterprise identity provider expectations.

Mailbox ingestion must preserve message identifiers, sender and recipient addresses, timestamps, thread/conversation identifiers, headers needed for correlation, attachment metadata, and delivery/retry state. These fields are required for project association evidence, duplicate suppression, audit reconstruction, and troubleshooting.

The system must support secure authorization for mailbox access and avoid broad mailbox permissions where narrower delegated or application permissions can satisfy the workflow. Mailbox configuration should be tenant-admin controlled and auditable, including which mailboxes are monitored, which project patterns or aliases are allowed, and which users or services can process inbound and outbound project email.

Outbound project email must be governed. Sending or drafting external responses from project context must respect approval policy, sender authority, participant permissions, audit requirements, and Microsoft 365 mailbox constraints. The product must record who requested the outbound action, which mailbox or identity sent it, what content was approved, and which project context or files influenced it.

### Integration Requirements

Hexalith.ChatBot must integrate with existing Hexalith bounded contexts rather than duplicating their authority. The canonical ownership and integration contract is §Context Ownership; this section adds only the local constraint that ChatBot may orchestrate those contexts but may not absorb their source-of-truth roles.

The product must use stable identifiers across these contexts so email-derived decisions, attachment links, AI actions, approval records, CLI operations, MCP calls, and audit views can be correlated without copying source-of-truth records into ChatBot-owned storage.

CLI and MCP integration must expose the same governed operations as the UI for the email-to-project workflow. These surfaces must not provide bypass paths around UI-enforced permissions, approvals, tenant isolation, or audit recording.


## B2B Governance and Tenant Requirements


### Technical Architecture Considerations

Hexalith.ChatBot must operate as an orchestration layer over existing Hexalith bounded contexts. It must not become the source of truth for projects, parties, files, tenants, identity, or event history. It references those domains through stable identifiers and executes state changes through governed service commands.

Email-to-project association logic is owned by a Project Association context. Mailbox ingestion, CLI, MCP, and AI actors may submit candidate signals or association requests, but they must not independently assign authoritative project links.

The technical architecture must support strict tenant isolation, command/query authorization at every surface, a shared operation contract across UI/CLI/MCP, deterministic and auditable project association before AI consumes project context, fail-closed behavior when required context is unresolved, event-backed traceability, and a clear separation between low-risk read-only assistance and risky actions requiring approval.


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
| ProjectAdmin / ProjectOwner | Authorized projects, associated conversations, project files, approvals, correction history | Review associations, approve risky AI actions as a current human principal, correct associations, manage project collaboration boundaries, inspect project audit history | Cross-tenant access; mailbox permission changes outside tenant policy; approving a proposal as its originating AI/tool/service principal |
| ProjectMember / Contributor | Authorized project conversations, allowed files, assigned review/approval items | Request AI help, review proposed actions, approve or cancel actions when policy grants authority and current human presence is recorded, inspect visible status | Approving actions without delegated authority; AI/tool/service self-approval; viewing restricted evidence |
| MailboxOwner | Tenant-authorized mailboxes and mailbox permission status | Grant or manage controlled mailbox access according to tenant policy | Project association overrides unless also granted project authority |
| Auditor / Compliance Reviewer | Authorized audit records, failure states, redacted support context | Investigate association decisions, approvals, command outcomes, risky AI actions, and failure states | Mutating project state or broadening access by audit role alone |
| Compliance Admin | Tenant-scoped data-rights requests, legal holds, retention dispositions, owner acknowledgements, and redacted/authorized result packages | Initiate and track export/erasure, place or release holds, authorize retention actions, deliver expiring redacted results, retry eligible partial/failed work | Direct mutation of Projects/Conversations/Folders/Parties/EventStore-owned data; acting without validated request authority or required independent TenantOwner approval; bypassing a hold |
| ServiceClient | Explicitly granted tenant/project/service scopes | Execute granted command/query operations with correlation, expiry, and audit metadata | Inheriting human roles; operating outside granted scope; silent privilege escalation |
| CLI Client | Same backend resources as the authenticated actor or service client | Inspect, associate, reject, defer, retry, approve, execute, status, and audit operations where authorized | Bypassing UI/API authorization, audit logging, validation, or tenant filters |
| Human-delegated MCP Client | Same backend resources as the currently authenticated delegating human | Access the parity set, including human decisions, only while current user presence and `actorType=human` are recorded | Delegation without current human presence; any originating-AI/tool/service self-approval; restricted or cross-tenant access |
| AI/tool MCP Client | Explicit read/proposal/low-risk scopes of the governed AI or tool actor | Inspect authorized status/evidence, create proposals, and invoke eligible low-risk assistance where policy permits | Association decisions; `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, or equivalent human decisions; approved execution; outbound/admin mutations; restricted or cross-tenant access |
| AI Actor | Explicitly packaged project context, authorized files, allowlisted commands | Produce proposals, perform low-risk assistance, execute approved actions through governed commands | Acting as a privileged system user; using unapproved files, tools, commands, or recipients |
| Background Worker | Assigned workflow items, mailbox events, retries, projections | Process idempotent work, retries, duplicate suppression, projections, and notifications | Mutating state without command validation, tenant scope, idempotency, and audit behavior |

#### Owner-authority mapping (A13 gate)

ChatBot role labels are application roles; they never create tenant or Project authority by themselves. The closed mapping below is deny-by-default and must be accepted by the owning contexts before M0:

| ChatBot authority | Required owner-side authority | Conflict/staleness rule |
| --- | --- | --- |
| TenantAdmin and scoped admin roles | Active Hexalith.Tenants `TenantOwner` membership **plus** an explicit ChatBot admin-role grant and exact EventStore permission token for the affected ChatBot domain/action | `TenantContributor`, `TenantReader`, global-admin status, or a UI label alone never grants the role. Any missing, stale, unavailable, malformed, or case-mismatched owner response denies. |
| ProjectAdmin / ProjectOwner | Active tenant membership plus the corresponding current Hexalith.Projects resource grant | Projects is authoritative; a ChatBot/Tenants role cannot broaden it. Stale Project context may inform a read-only display only within NFR6 and never authorizes mutation. |
| ProjectMember / Contributor | Active tenant membership plus current Hexalith.Projects membership/action grant | The most restrictive result wins; possession of a Project or conversation ID is not authority. |
| MailboxOwner | Active tenant membership, explicit ChatBot `mailbox-admin` scope where needed, and current Microsoft 365 mailbox/delegation evidence | Mailbox authority does not grant Project access or association-decision authority. |
| Auditor / Compliance Reviewer | Active tenant membership, explicit ChatBot audit/compliance grant, exact EventStore read permission, and Project authority for unredacted per-item evidence | Tenant-wide views remain aggregate/redacted unless Project authority is current. |
| Service, CLI, MCP, worker, and AI actors | Exact Keycloak client/user subject, `eventstore:tenant`, `eventstore:domain`, and closed `eventstore:permission` claim plus the resource grant/delegation required by the originating human or Project | Claims and owner responses are compared ordinally and case-sensitively. Human-decision commands additionally require current human authentication, recorded user presence, `actorType=human`, and a principal distinct from every originating AI/tool/service identity. Local/dev claims-only validation is forbidden for pilot; production uses the actor-backed Tenants/EventStore gateway and fails closed. |

Global administrators are a separate Tenants system authority and receive no implicit ChatBot data access. Every command/query evaluates tenant lifecycle and membership, the closed ChatBot role, Project/resource authority, and action permission before handler, projection, cache, replay, or data access. Revocation-sensitive mutations require current owner/gateway authorization; an eventually consistent local projection cannot overrule it.

### Service Client Permissions

Service clients are not users. They require dedicated identities with least-privilege scopes per integration and operation. Service-client authorization must not inherit UI roles except through explicit delegated flows where the source user, tenant, scope, and expiry are recorded.

Service-client classes and their enumerated scopes:

| Service-client class | Increment | Scope | Authorized command/query set | Credential expiry |
|---|---|---|---|---|
| `mailbox-ingestion-client` | M0 | per-tenant; one configured mailbox pattern | `MailboxEvent.Receive`, `Message.Capture`, `Attachment.Capture` | 90 days, auto-rotated |
| `audit-projection-client` | M0 | per-tenant; read source events, write projection state | `Event.Read`, `Projection.Append`, `Projection.Rebuild` (admin-triggered only) | 90 days, auto-rotated |
| `background-retry-client` | M0 | per-tenant; retry-only access to failed operations | `Operation.Retry`, `Operation.Status.Read` | 30 days, auto-rotated |
| `ai-action-mediator-client` | M0 | per-tenant + per-AI-actor; bound to requester, Project, proposal, approval, and one allowlisted command | submits exactly one approved `Project.AppendConversationMessage`; AI actor never authenticates directly | short-lived delegated grant; expires on use, revocation, or 5 minutes |
| `cli-automation-client` | M1 | per-tenant; configured per CLI installation; user-delegated via OAuth device flow | the MVP parity set (per §Cross-surface parity outcomes) | 7 days, refresh required |
| `mcp-human-delegated-client` | M1 | per-tenant + current authenticated human; records source user, user presence, `actorType=human`, scope, and delegation expiry | the actor-authorized MVP parity set; human-decision mutations require current presence at submission | 1 hour maximum; terminates on logout, revocation, or lost presence |
| `mcp-tool-client` | M1 | per-tenant + per-AI/tool actor; MCP-tool-scoped grants | only actor-visible status/evidence queries plus `ProposeAIAction` and eligible `ExecuteLowRiskAssistance`; explicitly excludes association decisions, approval/revision decisions, approved execution, outbound, policy, permission, and admin mutations | 24 hours maximum; refresh required |
| `ai-action-execution-client` | M1 | per-tenant + per-AI-actor; bound to a specific approved AI-action proposal | exactly one command from the current allowlist version, exactly once | bound to approval record; expires on use or 5 minutes |

Service-client authorization never inherits UI roles. Delegated flows (for example `cli-automation-client` or `mcp-human-delegated-client`) record source user, tenant, scope, expiry, OAuth grant evidence, human-presence evidence, and actor type in audit. No AI-, tool-, mediator-, execution-, or generic service-client credential can approve, reject, or request revision of an AI proposal, including one it originated or will execute. Every service-client action carries tenant, client identity, operation, resource type, triggering integration event where applicable, result, and audit metadata in its audit envelope per NFR50.

Expired, revoked, over-scoped, and under-scoped service-client credentials must fail closed and be covered by acceptance tests.

### Command and Query Contracts

MVP command contracts include the canonical governed workflow operations listed in this section. Architecture may split or compose them, but UI/API, CLI, and MCP must expose equivalent authorization outcomes, state transitions, audit behavior, and redaction semantics for the parity set.

This is the complete ChatBot-owned stable operation-ID catalog. Owner-context implementation targets are mapped separately and remain blocked until accepted under A13. Surface exposure is separately governed; this catalog is not an AI allowlist, whose exact deny-by-default version is in `addendum.md` §Command Allowlist v1.

- `CaptureMailboxEvent`
- `ResolveInboundAuthenticityReview`
- `ReprocessInboundAuthenticity`
- `ProposeEmailProjectAssociation`
- `AssociateEmailToProject`
- `ConfirmEmailProjectAssociation`
- `RejectEmailProjectAssociation`
- `DeferEmailProjectAssociation`
- `MarkEmailAssociationNeedsReview`
- `SkipEmailAssociation`
- `CorrectEmailProjectAssociation`
- `ReprocessEmailAssociation`
- `ResumeEmailAssociationReview`
- `MarkEmailAssociationFailed`
- `AcknowledgeAssociationCorrectionStore`
- `MarkAssociationCorrectionDelayed`
- `LinkOrResolveEmailParticipant`
- `MarkParticipantResolutionDisposition`
- `ResumeParticipantResolutionReview`
- `CaptureEmailAttachment`
- `StoreEmailAttachmentInProjectFolder`
- `MarkEmailAttachmentOutcome`
- `CaptureTaskIntent`
- `MarkTaskIntentDisposition`
- `SubmitGovernedChatMessage`
- `CancelGovernedChatRequest`
- `StopGovernedChatResponse`
- `RetryGovernedChatMessage`
- `ProposeAIAction`
- `ExecuteLowRiskAssistance`
- `ApproveAIAction`
- `RejectAIAction`
- `RequestAIActionRevision`
- `CancelAIAction`
- `ExecuteApprovedProjectCommand`
- `RetryApprovedAIActionExecution`
- `CreateOutboundProjectEmailDraft`
- `SendApprovedProjectEmail`
- `ReconcileOutboundSendOutcome`
- `AddWorkflowResolutionAnnotation`
- `RetryMailboxIntake`
- `RetryAttachmentCapture`
- `RetryCommandExecution`
- `RebuildAuditProjection`
- `GrantServiceClientPermission`
- `RevokeServiceClientPermission`
- `ConfigureMailboxSource`
- `PauseMailboxSource`
- `ResumeMailboxSource`
- `DisableMailboxSource`
- `UpdateTenantPolicy`
- `UpdateCommandAllowlist`
- `ConfigureNotificationRouting`
- `UpdateOperationalLimits`
- `ApplySafetyControl`
- `ReleaseSafetyControl`
- `GrantChatBotAdminRole`
- `ChangeChatBotAdminRole`
- `RevokeChatBotAdminRole`
- `PauseQueuePartition`
- `ResumeQueuePartition`
- `ClaimQueueItem`
- `AssignQueueItem`
- `SupersedeWorkflowDecision`
- `InitiateDataExport`
- `RetryDataExport`
- `InitiateDataErasure`
- `RetryDataErasure`
- `PlaceLegalHold`
- `ReleaseLegalHold`
- `ExecuteRetentionDisposition`
- `RetryRetentionDisposition`
- `DispatchWorkflowNotification`
- `RetryWorkflowNotification`

AI entry commands and allowlist targets remain separate:

| ChatBot entry command | Stable product allowlist member | Owner executable target | Status |
| --- | --- | --- | --- |
| `ExecuteApprovedProjectCommand` | `Project.AppendConversationMessage` | Conversations `AppendMessageCommand` v1 / `MessageAppended` | Blocked by A13 pending producer implementation and accepted audit/concurrency/retention mapping. |
| `ExecuteLowRiskAssistance` | `ChatBot.ExecuteLowRiskAssistance` | ChatBot shared-pipeline command v1 | Specified for M1; A8/A9a qualification required. |

Each mutating command must include actor identity, tenant scope, correlation ID, stable `operation_id`, target resource IDs, expected aggregate/workflow revision or an owner-approved equivalent concurrency/uniqueness token, expected result codes, applied policy/approval references, and canonical audit metadata. Human decisions additionally carry the authoritative `decision_slot_id`. No owner-contract exception is usable until its A13 mapping is accepted and contract-tested.

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
- `GetInboundAuthenticityStatus`
- `ListOperationalQueues`
- `GetServiceClientPermissions`
- `GetProjectAccessForActor`
- `GetOutboundSendStatus`
- `GetDataExportStatus`
- `GetDataExportResult`
- `GetDataErasureStatus`
- `GetLegalHoldStatus`
- `ListLegalHolds`
- `GetRetentionDispositionStatus`

Queries must apply the same tenant and role filters as commands. `GetInboundAuthenticityStatus` is per tenant/mailbox/provider-message ID and returns the stable authenticity state, redacted provider/header/delegation evidence references, policy version, evidence timestamp/freshness, reviewer/approval state, owner, terminal/successor IDs, and next safe action. Only the mailbox-scoped initiating `mailbox-admin`, independent `policy-admin`, or an explicitly authorized auditor may read it; it returns no candidate Project or Project content. There is no admin/debug bypass in MVP.

### Association Lifecycle and States

Email-to-project association must be idempotent by tenant, mailbox identity, and message identity. Reprocessing the same message must not create duplicate project links, conversations, files, task requests, or audit decisions except retry metadata.

Required association states (canonical ordering, matching §Shared Workflow Contract):

- `Received`
- `Associated`
- `Rejected`
- `Deferred`
- `NeedsReview`
- `Failed`
- `Skipped`
- `Correcting`
- `CorrectionDelayed`
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

Outbound email authority must be explicit. For every FR48 class, authorization is the intersection of token subject/client and OAuth mode/scope, target mailbox, mailbox ACL/delegation, current membership where applicable, sender/send-as/send-on-behalf identity, tenant, current ChatBot Project/outbound scope, timestamped evidence, and execution-time revalidation. API permission, membership, delegation, or ChatBot policy alone is never sufficient. Risky outbound actions require fresh human approval and preserve frozen content, the full sender-authority tuple, recipients, Project context, and source command surface.

Message identity must be stable enough for idempotency and audit reconstruction. The integration must retain source message ID, internet message ID where available, conversation/thread identifiers, mailbox identity, delivery timestamp, sender/recipient metadata, and attachment identifiers.

### Integration List

MVP integrations are limited to the systems needed to prove the governed email-to-project collaboration loop:

- Hexalith.Projects
- Hexalith.Conversations
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

If Keycloak or identity resolution is unavailable, command/query operations fail closed instead of falling back to broad access. If EventStore or audit writing is unavailable, **every** state-mutating operation fails closed per NFR15a—not only risky commands and AI actions. During an AI service outage, deterministic project association, audit, authorization, and manual resolution workflows remain usable. The M0 tag-and-heuristic risk classifier also remains usable because it does not depend on AI services; therefore, the approval gate survives the outage (see `addendum.md` §Risk Classifier).

### Performance & Operability Considerations

The MVP must be operable under realistic mailbox and project volumes without turning ambiguous association into a manual bottleneck. The system should measure ingestion latency, candidate generation latency, ambiguous-resolution time, command execution latency, audit projection lag, retry volume, duplicate suppression rate, failed mailbox processing rate, and dead-letter rate.

CLI and MCP operations must return clear status for long-running or eventually consistent work. If audit projections, attachment scans, or command outcomes are delayed, surfaces should return pending or partial-success states rather than stale success claims.

Operational views should expose API health, mailbox integration health, background worker health, database health, Keycloak connectivity, mailbox backlog, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, service-client failures, and audit projection lag.

### Compliance Requirements

The B2B governance implementation must support enterprise expectations without overclaiming certification status. The MVP must include practical controls for GDPR/EU data protection, tenant isolation, least privilege, auditability, identity integration, and Microsoft 365 / Exchange mailbox governance.

Compliance requirements include GDPR-aware handling of email content, external party records, message metadata, attachments, AI prompts/outputs, approvals, and audit records; tenant-admin visibility into captured data and retention; command/query authorization across UI/CLI/MCP; audit records for association and AI workflows; secure mailbox access; explicit outbound authority; and suppression of unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details.

Storage locations for tenant Project metadata, email metadata, audit records, and derived AI outputs are defined per data class in §Data Governance Surface. Region pinning follows NFR12 and `data.residency-region`. Export/delete workflows are operationally supported through FR58 and remain blocked from pilot claims until the A6 data-class contract settles retention, legal hold, backup propagation, erasure, and surviving metadata.

### Audit Requirements

The system must record who or what associated an email to a project, when, using which rule/signal, and whether the association was automatic, suggested, corrected, rejected, deferred, or overridden.

Minimum audit fields include `tenantId`, `actorId`, `actorType`, `commandName`, `resourceId`, `decision`, `reasonCode`, `correlationId`, and `timestamp`.

Every durable mutation must carry the FR81a atomic canonical audit envelope in every increment. Security-sensitive non-mutating attempts—including authorization denials, restricted reads, and service-client failures—also produce audit records, but those records cannot masquerade as or repair a missing mutation envelope. Completeness is measured separately for both sets.

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

Tenant isolation monitoring has zero tolerance for cross-tenant access events. Every durable mutation requires atomic canonical audit, and security-sensitive non-mutating attempts require the separate auditable-attempt record.


### Implementation Considerations

The MVP should be implemented as a narrow vertical slice through the governed email-to-project workflow. It should avoid building a broad collaboration platform before proving project association, authorization, approval, audit, and cross-surface parity.

Implementation should prioritize stable identifiers across Hexalith bounded contexts, shared command/query contracts for UI/CLI/MCP, idempotent mailbox ingestion, explicit association lifecycle states, evidence-first candidate generation, tenant-scoped authorization filters before candidate or audit projection, allowlisted service commands, auditable AI action proposals and outcomes, contract tests, parity tests, and test fixtures for clear matches, ambiguous matches, no-match cases, unauthorized references, cross-tenant references, duplicate delivery, retries, attachment states, degraded dependencies, and risky AI approvals.


## Functional Requirements

### Glossary

| Term | Definition |
| --- | --- |
| Actor | A human user, external party record, service client, background worker, mailbox event processor, CLI client, MCP client, or AI actor that initiates or participates in a workflow. |
| AI actor | A governed service actor that can use only explicitly provided project scope, authorized files, allowlisted tools or commands, and tenant policy. It is not a privileged system user. |
| Allowlisted command | A Hexalith service command present in the current versioned command allowlist (see `addendum.md` §Command Allowlist v0 / v1). Only allowlisted commands can be invoked by AI actors; commands removed from the allowlist cannot be invoked by AI actors even when the underlying service still accepts them. |
| Aspire | .NET Aspire — the application composition framework Hexalith uses for development and runtime topology (service discovery, resource declaration, telemetry wiring, local-dev container orchestration). Aspire is a build/deploy concern, not a runtime dependency of individual user requests. |
| Approval | A decision by a currently authenticated, authorized human with recorded user presence that permits, rejects, revises, or cancels a proposed approval-required action before execution. Policy may classify, route, or deny an action but cannot approve or co-sign the human gate. |
| Approval-required | A risk classification (per `addendum.md` §Risk Classifier) for actions that cannot execute without human approval: actions that modify project state, send external communication, expose file content, create or assign tasks, invoke external tools, or act on behalf of a participant. Distinct from `low-risk` (allowed if tenant policy permits) and `denied` (refused regardless of approval). |
| Association | The decision that links email-derived context to one project or explicitly rejects, defers, fails, skips, or corrects that link. |
| Candidate project | A project the system may suggest for association after applying tenant, authorization, and evidence filters. Unauthorized projects must not appear as candidates. |
| Command surface | The client or origin through which an operation is requested, including UI/API, CLI, MCP, background worker, mailbox event, or AI actor. |
| Context package | The bounded project, requester, file, policy, evidence, command, and redaction context made available to an AI actor or governed command. Synonym: **Scoped AI context.** The PRD uses "Context package" in the FR/NFR catalog and "Scoped AI context" in narrative prose; the two terms refer to the same artifact. |
| Evaluation dataset | Offline, consented/redacted/synthetic qualification partitions maintained by the Test Architect (A9/A9a) for `AssociationScorer`, `TaskIntentDetector`, and `ActionRiskClassifier`. It is not a runtime dependency. |
| Evidence | The auditable signals used to justify association, authorization, approval, refusal, correction, or investigation. |
| External party | A customer, supplier, partner, organization, or contact represented through Hexalith.Parties and participating through email or another governed channel. |
| Party | A tenant-scoped participant identity owned by Hexalith.Parties. A party may represent an internal or external participant. |
| Projection | A derived read model or project view rebuilt from source records, commands, events, and audit history. |
| Fail closed | A code path that, on encountering an error or unmet precondition that would otherwise let it proceed without enforcing a safety control, returns a typed error and writes no durable state — instead of falling back to permissive behavior. See NFR15a for the enumerated paths and conditions. |
| Idempotency key | A stable `operation_id` for a durable mutation or `decision_slot_id` for one authoritative human decision. Canonical hashes are evidence and may suppress non-mutating proposals, but time windows never permit a durable operation to run twice. |
| Low-risk | An `ActionRiskClassifier` result limited to product-declared read-only/no-external-effect subtypes enabled by `ai-action.low-risk-subtypes`. Boundary-crossing effects are structurally non-downgradable and remain `approval-required`. |
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
| UJ1 - Business contributor requests AI help | FR21-FR28f, FR33, FR35-FR46 | NFR1-NFR11, NFR16, NFR49-NFR55, NFR60-NFR64 | Project context, governed composer, scoped AI, approval, audit |
| UJ2 - Business contributor resolves ambiguous association | FR3-FR12, FR64-FR69, FR76-FR80 | NFR13-NFR18, NFR23-NFR30, NFR37-NFR48 | Candidate evidence, confirm/reject/defer, fail-closed ambiguity |
| UJ3 - External party sends project context | FR1-FR4, FR13-FR20, FR29-FR34 | NFR1-NFR12, NFR31-NFR36, NFR49-NFR55 | Party resolution, tenant scope, authorization before exposure |
| UJ4 - Project owner corrects wrong association | FR7-FR8, FR23-FR28, FR60-FR63, FR87-FR96 | NFR13-NFR22, NFR49-NFR59 | Correction, derived-context invalidation, audit reconstructability |
| UJ5 - Tenant admin configures governed collaboration | FR9, FR18-FR20, FR51-FR53, FR67-FR75 | NFR23-NFR48, NFR65-NFR70 | Policy configuration, mailbox health, operational queues |
| UJ6 - Developer uses CLI | FR80-FR86, FR90-FR95 | NFR24-NFR36, NFR67-NFR70 | CLI parity, status, idempotency, redaction, audit lookup |
| UJ7 - Compliance/support investigates risky action | FR54-FR63, FR85-FR86, FR90-FR91 | NFR49-NFR59 | Reconstructable audit records with safe visibility |
| UJ8 - User reviews AI action before boundary crossing | FR39-FR50 | NFR16, NFR46-NFR48, NFR60-NFR64 | Risk explanation, approval decision, refusal and outbound control |
| System journey - Governed AI execution | FR33, FR39-FR46, FR81-FR89 | NFR1-NFR22, NFR31-NFR36, NFR49-NFR55 | Policy-aware AI actor, allowlisted command execution, failure behavior |

Success trace: UJ1/System Journey validate SM12, SM13, SM16, SM-C2, SM-C3, and SM-C5; UJ2 validates SM1–SM6, SM7, SM-C1, and SM-C4; UJ3 validates SM6, SM8, SM14, and SM-C2; UJ5 validates SM8–SM12, A11-M1 pilot-measure qualification, and A11-M2 SLO qualification; UJ6 validates SM15; UJ7 validates canonical completeness and investigation-view availability in NFR50a.

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
| FR13-FR20 - Participants, identity, authorization | Known/unresolved/external Party resolution, consent metadata, cross-tenant/unauthorized suppression, revoked identity, service-client scope, idempotent linking, safe error redaction, M0 authenticity gate, and canonical audit. |
| FR21-FR28f - Conversation, context, governed chat | Project/message ownership, informational/actionable detector outputs, source-vs-AI provenance, composer admission, safe streaming, stop/cancel, risky proposal conversion, duplicate retry, typed failure, WCAG, and audit attribution. |
| FR29-FR38 - Files and task intent | Attachment capture/store/quarantine, unauthorized metadata/content, duplicate attachment, detector failure/review, terminal intent dispositions, expected revisions, redaction, audit, and increment gate. |
| FR39-FR46 - AI action mediation | Low-risk read-only assistance, approval-required outbound/file-exposing action, denied cross-tenant or unauthorized-file request, unsupported MVP action, mixed-risk request, approval rejection, approval revision, command failure, and audit reconstruction. This depends on A5 and A8. |
| FR47-FR54 - Outbound, authenticity, and administration | Every authority class, M0 provider/header/delegation/external evidence, strict/paranoid routing, membership revocation, content freeze, unauthorized admin, policy invariants, idempotent send, redaction, and audit. |
| FR55-FR63 - Audit and governance | Required audit field presence, redacted audit view for restricted actors, policy snapshot retrieval, source evidence retention, superseded human decision, support investigation, export/delete operational support, and audit-store unavailable fail-closed behavior. This depends on A6. |
| FR64-FR80 - Reliability and operations | Retryable/terminal failures, queue ownership, claim concurrency, duplicate suppression, degraded/stale states, notification routing, policy limits, safe message codes, filtering/pagination, long-running status, isolation, and audit. |
| FR81-FR89 - Cross-surface parity and state model | UI/API, CLI, and MCP association/status/audit parity; invalid transition rejection; equivalent redaction and error codes; command-surface attribution; long-running status retrieval; and replay of the same scenario across at least one machine surface. This depends on A3. |
| FR90-FR96 - Idempotency, correction, evaluation, replay | Stable operation/decision identities, semantic conflicts, expected revisions, correction propagation, dataset isolation, replay-safe composition, egress denial, production invariance, evidence freshness, and M2 stop-ship behavior. |

### Project Email Intake and Association

- FR1: The system can capture authorized mailbox events as project collaboration inputs.
- FR2: The system can preserve source email identity, thread identity, mailbox identity, sender, recipients, timestamps, and attachment references.
- FR3: The system can associate incoming email with an existing project using deterministic evidence.
- FR4: The system can detect ambiguous project association and route it to human review.
- FR5: Authorized users can review candidate projects with visible evidence, confidence state, reason codes, and the consequences of each available decision.
- FR6: From `NeedsReview`, authorized users can confirm one visible candidate with current evidence, reject all candidates with a required reason, or defer with a required owner and revisit condition. A deferred item must first be resumed through `ResumeEmailAssociationReview` after its revisit condition and evidence refresh; users cannot invoke the worker-only `MarkEmailAssociationNeedsReview` transition.
- FR7: Authorized Project owners with current authority on both the source and destination Projects can start correction of a previously selected association. Completion follows the correction impact manifest and owner-acknowledgement contract in FR91a; ChatBot never directly mutates Conversations-, Folders-, or Projects-owned records.
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
  - **Accept when:** the "why" panel for any associated email displays, at minimum: the originating signal class (explicit identifier / mailbox routing rule / thread identifier / human selection / correction), matched value, confidence score, disposition (`auto` or `needs-review`), typed reason, visible-candidate rule, decision actor, decision timestamp, and links to superseding corrections.
- FR24: Authorized users can see association, attachment, task, approval, command, failure, retry, and next-action status for a project conversation.
- FR25: The system can keep project conversation context separate across tenants and projects.
- FR26: The system can distinguish informational project context from actionable requests.
  - **Accept when:** every email carries an `informational` or `actionable` badge; actionable means `request-information`, `request-action`, or `request-decision` from the independently versioned `TaskIntentDetector`. The item surfaces detector version, evidence offsets, confidence, and review/capture/dismiss affordances. No action-risk result is implied.
- FR27: The system can distinguish system-generated summaries from source evidence so users do not confuse AI interpretation with original email, attachment, or command facts. Every external or retrieved fragment carries an immutable origin/trust label and remains data, never system/tool policy, authority, or approval. Suspicious instruction-like content returns `untrusted-instruction-detected`, prevents model/tool invocation, and lets an authorized Project actor inspect, exclude/quarantine through the source-owner workflow, then submit a new request; MVP cannot promote it into a trusted instruction source.
  - **Accept when:** AI-generated content is visually distinct (typographic treatment + label `AI summary`), is preceded by a one-line provenance string (`Generated by <model+version> at <timestamp> from <source-evidence-IDs>`), and can be collapsed to reveal the source evidence directly. Source evidence display is the default; AI summaries are opt-in to expand. WCAG 2.2 AA non-color status applies (the distinction does not rely on color alone).
- FR28: The system can preserve visible human-review history for each email, attachment, approval, AI action, and command.
- FR28a — **Governed chat submission (M1):** Authorized users can submit a Project-scoped message through the FrontComposer `S1a` composer; every submission enters CommandGateway with actor, tenant, Project, conversation, stable `operation_id`, expected revision, and source attribution.
- FR28b — **Admission outcome (M1):** The composer shows `accepted`, `needs-review`, `approval-required`, `denied`, `unsupported`, or typed failure before implying that AI work has started.
- FR28c — **Safe response streaming (M1):** Eligible read-only/no-external-effect responses may stream with model/version, evidence provenance, and completion state; partial output is visually marked and never treated as a committed Project message.
- FR28d — **Stop and cancel (M1):** Users can stop a `Streaming` generation through `StopGovernedChatResponse`, cancel an `Admitted` pre-stream request through `CancelGovernedChatRequest`, or cancel an `AwaitingApproval` proposal through `CancelAIAction`. Each command uses expected revision; the UI distinguishes all three outcomes and records the winning transition.
- FR28e — **Risky-request conversion (M1):** Every request that modifies state, exposes files, sends externally, creates/assigns tasks, invokes tools, or acts on behalf becomes a mandatory-approval proposal and cannot stream or execute the boundary-crossing result first.
- FR28f — **Retry, failure, and audit (M1):** Each logical submission has a stable `chat_request_id`; each immutable generation attempt has its own `attempt_operation_id`, expected conversation revision, and predecessor link. Reusing an attempt ID returns that attempt's prior outcome or a typed conflict. Retrying a `stopped` or `failed` attempt creates exactly one new linked attempt; it never commits or resumes partial output from its predecessor. In a stop/completion race, the first expected-revision commit wins: completion commits one result, while a winning stop discards uncommitted partial output. Failures expose safe next actions and every attempt is attributed in the canonical audit chain.

### Files and Attachments

- FR29: The system can capture attachments from associated project email.
- FR30: The system can store captured attachments in governed project folders.
- FR31: Authorized users can inspect attachment capture and storage status.
- FR32: The system can prevent unauthorized actors from viewing attachment metadata or content.
- FR33: The system can make authorized Project files available as scoped AI context only through explicit authorization, policy checks, auditable context packaging, and preserved origin/trust labels that keep retrieved content and tool results outside the instruction-authority channel.
- FR34: The system can represent attachment workflow state as exactly `PendingScan`, `Stored`, `Unsafe`, or `Failed`, consistent with §Shared Workflow Contract. Capture and retry are audited commands/events that create or transition attempts; they are not attachment states.

### Task Intent and AI Action Mediation

Action-risk classifications and pre-classification dispositions:

| Classification or disposition | Default outcome | Examples | Required controls |
| --- | --- | --- | --- |
| Low-risk read-only | Allow only when tenant policy and project authorization permit it. | Summarize already-associated project conversation, list visible status, explain candidate evidence already visible to the actor. | Project scope, actor authorization, policy snapshot, source evidence references, audit record. |
| Approval-required | Pause for authorized human approval before execution. | Draft or send external email, expose file content in generated output, create or assign a task, mutate project state, invoke an external tool, act on behalf of a participant. | Action preview, affected resources, recipients or destination, sender authority, approver identity, approval decision, command allowlist, audit record. |
| Classifier-indeterminate | Refuse with typed `classifier-indeterminate`; create no proposal or durable idempotency state. | Missing classifier tags, unknown effect surface, undeclared authority class, invalid/unqualified classifier artifact. | Redacted reason, separately typed security-sensitive non-mutating attempt record, remediation guidance, and a new linked request after remediation. Human approval cannot override this outcome. |
| Denied | Refuse and audit when policy or authorization blocks the action. | Cross-tenant access, unauthorized files, unresolved project association, unresolved actor identity, unapproved sender authority, command outside allowlist. | Safe refusal message, redacted reason, policy or authorization reference, audit record when security-sensitive. |
| Unsupported | Decline or route to manual handling when the product does not support the action in MVP. | Full task lifecycle automation, autonomous project creation, broad document intelligence, arbitrary third-party workflow execution. | Clear unsupported-state response, optional task-intent capture, no project mutation unless separately approved. |

Only `Low-risk read-only` and `Approval-required` are successful `ActionRiskClassifier` outputs. `Classifier-indeterminate` is a fail-closed classifier failure, while `Denied` and `Unsupported` are dispositions resolved before classification. Mixed requests inherit the strictest applicable result; denied portions may be split only when separation is safe and audited.

- FR35: The system can detect candidate task or action intent from authorized project conversation actors and preserve the source message evidence.
  - **Data contract.** A captured task-intent record includes `tenant_id`, `project_id`, `source_message_id`, `requester_party_id`, `detected_intent_summary` (<= 280 chars), `detected_action_kind` (`request-information|request-action|request-decision|informational`), `source_evidence_offsets`, `detector_version`, `confidence_score` in `[0.0,1.0]`, `detected_at`, and state. A9a has separate informational/actionable partitions; target precision/recall is >= 80%/75% at M0 and >= 90%/85% at M1. The offline corpus is not a runtime dependency. [ASSUMPTION A9a]
- FR36: Authorized users can review captured task intent before governed action. The review surface displays the data contract from FR35 plus the source message in full and the available state transitions per FR37/FR38.
- FR37: Authorized users can convert captured task intent into a governed task or action request. Conversion creates the proposal record per FR41 / `addendum.md` §Risk Classifier and links it to the source task-intent record. Conversion is itself an audited operation.
- FR38: Authorized users can mark captured task intent as not actionable, duplicate, already handled, or out of scope. Each of these is a terminal state for the task-intent record (the record is preserved for evaluation per A9a); duplicate additionally links the predecessor task-intent ID.
- FR39: The system can classify AI action requests through the independently versioned categorical `ActionRiskClassifier` defined in `addendum.md`; it does not share scores or runtime artifacts with association or task-intent detection. Missing/invalid artifacts or an indeterminate result return typed `classifier-indeterminate`, create no proposal or durable idempotency state, and cannot be overridden by human approval.
- FR40: The system can allow only product-declared read-only/no-external-effect subtypes when tenant policy `ai-action.low-risk-subtypes` and Project authorization permit them.
- FR41: The system must require approval by a currently authenticated, authorized human with recorded user presence and `actorType=human` before AI actions that modify Project state, expose files, send external communication, create or assign tasks, invoke tools, or act on behalf of a participant. The approver must be distinct from every originating AI/tool/service principal; those identities are structurally denied approval mutations. Policy may classify, route, or deny an action but cannot approve or co-sign the human gate. These six effect classes are structurally non-downgradable by tenant policy.

Approval-fatigue mitigation uses prioritization, grouping, digesting, notification ceilings, and staffing/escalation. The SM-C3 threshold triggers workflow tuning, never approval bypass.
- FR42: Authorized human users can approve or reject proposed AI actions after reviewing the action summary, affected Project resources, external recipients, sender authority, risk classification, expected outcome, and approval expiry.
  - **Accept when:** the approval surface for any pending AI action displays, at minimum: the proposed command name (from the current allowlist version), the input files (each rendered as a tappable evidence reference with redaction state), the proposed outbound recipients if any, the sender authority class the action would use (per `addendum.md` §Authority class mapping), the risk classification with the input tuple that produced it (per `addendum.md` §Risk Classifier), the policy snapshot ID, `approved_at`/`expires_at` when decided, the expected post-state (resource changes, side effects, audit events that will be emitted), and the approver's available decisions: `approve` / `reject` / `request-revision` / `cancel`. Approval requires current human presence, `actorType=human`, authority for the risk class, and independence from the originating AI/tool/service principals; the surface disables `approve` with a stable reason when any condition fails or evidence is expired.
- FR43: The system can execute approved AI actions only through allowlisted governed commands.
- FR44: Authorized users can inspect AI action proposals, approvals, denials, executions, failures, and outcomes.
- FR45: Authorized users can preview outbound communication, file access, command execution, and AI-generated changes before approval or execution.
- FR46: The system can refuse or block unsafe AI, automation, command, or mailbox requests that exceed tenant policy, project authorization, sender authority, or approved command scope.

### Outbound Communication

- FR47: Authorized users can create outbound Project email drafts within approved Project and sender authority. A draft whose send result becomes unknown cannot be retried; an authorized mailbox reconciliation must reach audited `NotSent` before a new draft may be created.
- FR48: The system can distinguish draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority. The mapping rule from M365 / Exchange permission models to ChatBot sender-authority classes is defined in `addendum.md` §Authority class mapping; the conflict case (M365 grants send-on-behalf but ChatBot grants no such authority) resolves to fail-closed (the action cannot be taken from ChatBot, even if the underlying mailbox would accept it).
- FR48a — **Inbound provider authenticity passthrough (M0).** Every inbound event records the M365 / Exchange DMARC, DKIM, and SPF verdicts as supplied by the provider.
- FR48b — **Inbound header inspection (M0).** The mailbox adapter parses the required authenticity/sender headers and records discrepancies as intake metadata and review reason codes; discrepancies never silently broaden trust.
- FR48c — **On-behalf-of disambiguation (M0).** Delegated-send evidence records the delegate as sender authority and preserves the principal as `principal_for`; outbound applies the same identity rule.
- FR48d — **External-sender posture and authenticity states (M0).** Unresolved external senders carry `external_sender = true`. `mailbox.authenticity-strictness` supports `strict` or `paranoid` only: `strict` anomalies enter `AuthenticityReviewRequired`; `paranoid` anomalies enter terminal `AuthenticityBlocked`. Only `AuthenticityAccepted` starts association `Received`. Accept/reject and blocked/rejected successor creation require a current `mailbox-admin` initiator, an independent current `policy-admin` approval bound to the exact evidence digest, expected revision, and stable operation ID; reprocessing additionally requires changed policy/provider evidence and creates one audit-linked successor. Advanced tuning remains M1.
- FR49: The system must require authorized human approval before outbound Project communication leaves the Project boundary.
- FR50: The system can preserve proposed and approved content/resource digests, recipients, sender-authority evidence tuple, Project context, requester, human approver, principal-independence evidence, `approved_at`, `expires_at`, policy/classifier versions, decision outcome, and any expiry/material-drift reason in approval records.

### Admin, Governance, and Audit

- FR51: Tenant administrators can configure mailbox integration settings and monitored mailbox patterns.
- FR52: Tenant administrators can enable product-declared low-risk read-only subtypes and configure approval routing; they cannot downgrade the mandatory-approval effects in FR41 or extend the AI allowlist.
- FR53: Tenant administrators can review mailbox permission status and degraded mailbox processing states.
- FR54: Compliance or support reviewers can investigate association decisions, approval decisions, command outcomes, and risky AI actions.
- FR55: The system must atomically produce a canonical audit envelope for every durable mutation across association, participant, file, conversation, task-intent, approval, command, AI, retry, duplicate, policy, administration, notification, and outbound workflows. It must also record security-sensitive non-mutating attempts such as denials, restricted reads, and service-client failures in a separate auditable-attempt path.
- FR55a — **Cross-tenant isolation at store introduction (M0+).** Every derived store that holds tenant material must enforce tenant isolation by construction and pass a negative native-store/API test in the first increment that introduces it. No store may enter multi-tenant use or surface through CLI/MCP/service clients before that proof passes. M2 adds the vector/embedding/prompt-cache proofs and recurring production probes.
- FR56: Authorized users can query audit records by tenant, actor, command, resource, decision, reason, correlation, and time context.
- FR57: The system can hide unauthorized project names, candidate evidence, file metadata, audit details, CLI output, MCP payloads, and error details.
- FR58: Through the pre-pilot governed O1 operations surface, a `compliance-admin` acting on a validated data-subject or tenant request and an independent current TenantOwner with `compliance-admin` grant can initiate, review, and track export, erasure, legal-hold, and retention workflows. Status/result queries expose owner-by-owner completion, redaction, hold precedence, rejection/appeal guidance, retry eligibility, and recipient-bound results that expire after 24 hours. ChatBot orchestrates owner commands and acknowledgements but never directly mutates source-owned data; A6 evidence must prove the complete path before first persisted pilot data.
- FR59: The system can propagate correlation context across mailbox intake, project association, file handling, approval, AI mediation, command execution, audit, UI, CLI, and MCP.
- FR60: The system can preserve source evidence used for association, authorization, approval, rejection, refusal, correction, retry, and audit investigation with retention boundaries and redaction behavior.
- FR61: The system can maintain versioned policy snapshots used for association, authorization, approval, AI action classification, and command execution decisions.
- FR62: Authorized users can append human notes or resolution rationale through `AddWorkflowResolutionAnnotation`, linked to an existing canonical association, participant, approval, retry, quarantine, or correction envelope. An annotation is non-authoritative, cannot alter the recorded decision, and cannot satisfy or repair missing canonical audit completeness.
- FR63: Authorized users can supersede reversible human decisions where policy permits while preserving the original decision in audit history.

### Reliability, Failure Handling, and Operations

- FR64: The system can detect duplicate mailbox delivery and avoid duplicate project artifacts.
- FR65: The system exposes the family-specific retry/recovery commands in §Shared Workflow Contract for failed mailbox intake, attachment capture, association reprocessing, approved-action/command pre-commit failure, audit projection rebuild, notification delivery, and data-subject/retention work. Approval decisions are immutable and are revised or superseded through their named successor commands, never retried in place.
- FR66: The system can surface terminal and non-terminal failure states to authorized users.
- FR67: The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status.
  - **Accept when:** each surfaced queue/health view renders, at minimum: the queue/health name, current depth or status enum (per NFR43), oldest item age, owner role for triage, and a link to the per-item detail (which carries the FR23 / FR42-grade detail panels). Status enums are stable strings (`healthy` / `degraded` / `failed` / `unknown`), not derived from counts. The view refreshes within the bounded staleness in NFR6 and shows the freshness timestamp per NFR48.
- FR68: The system can fail closed when project association, participant identity, tenant scope, authorization, audit writing, or required dependencies cannot be resolved.
- FR69: Authorized users can view and manage queues for ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachment handling, and retryable operations.
- FR70: Authorized users can assign or claim review items that require human resolution.
- FR71: Authorized users can see the next required human action for an email, task intent, attachment, approval, or failed operation.
- FR72: The system can notify authorized users when review, approval, failure, degraded mailbox, quarantine, or retry states require attention.
- FR73: Tenant administrators can configure notification routing and escalation rules for unresolved review, approval, degraded, quarantine, and failure states.
- FR74: Authorized administrators can disable, quarantine, or rate-limit exactly four subject classes—mailbox sources, service clients, AI actors, and command capabilities—through the closed safety-control matrix in §Shared Workflow Contract. Outbound is governed as a command capability, not a fifth class.
  - **Decomposition guidance for story authoring:** decompose the four subjects × three actions into per-cell scenarios against `ApplySafetyControl` and add release scenarios against `ReleaseSafetyControl`. Each scenario inherits the matrix actor/co-approver, transition, event, bound, and safer-state-on-failure rule.
- FR75: Authorized administrators can configure per-tenant rate limits, quotas, and circuit breakers for mailbox processing, AI mediation, command execution, and outbound communication.

### Tenant-Admin Permission Model (FR75a–FR75g; M0 bootstrap, M1 full surface)

The tenant admin is not a superuser. Admin scope is bounded so the "no admin/debug bypass" promise (per NFR1, NFR2, NFR7) holds against the operational dashboards admins need.

- FR75a: A `tenant-admin` role holds the union of every admin scope in FR75b–FR75g; finer-grained admin roles (`mailbox-admin`, `policy-admin`, `compliance-admin`, `operations-admin`) hold proper subsets. `GrantChatBotAdminRole`, `ChangeChatBotAdminRole`, and `RevokeChatBotAdminRole` are the only ChatBot admin-grant mutators; each requires a current Tenants `TenantOwner`, an independent authorized TenantOwner, a closed role/scope, expected revision, and atomic audit. Service clients and AI actors cannot initiate or approve them.
- FR75b — **See-only scopes:** admins can read operational queue summaries (depth, age, owner), health/status enums, and aggregate metrics (per FR67) across all tenant projects without holding per-project membership. Reading per-item detail (project name, evidence content, file metadata, audit reasons) requires per-project authority; admin role does not grant it.
- FR75c — **Operate scopes:** admins without Project authority may pause or resume a mailbox, client, or opaque queue partition. They may request an investigation using content-free identifiers. They cannot retry, requeue, quarantine, dismiss, or otherwise mutate an individual Project workflow item. Per-item operations require current Project authority and revalidate the original requester's authority, expected revision, policy snapshot, and audit envelope. An emergency per-item operation, if later introduced, requires independent approval, reason, bounded scope, expiry, and its own versioned contract; none exists in MVP.
- FR75d — **Policy scope (`policy-admin`):** can initiate only the Tenant Policy Schema knobs whose row names `policy-admin`; each schema row is authoritative for initiating role and co-approver. Every security-sensitive knob requires an independent second authorized admin, separation of duty, and a documented justification in the canonical audit envelope.
- FR75e — **Mailbox scope (`mailbox-admin`):** can configure mailbox patterns, routing rules, and provider-credential connections. Authenticity-policy mutation follows its schema row and requires the named independent `policy-admin` co-approval. A mailbox admin cannot read mailbox content or decide associations.
- FR75f — **Compliance scope (`compliance-admin`):** can read audit records across the tenant (subject to per-project redaction per NFR2), trigger investigations, initiate retention-window changes within A6/NFR49a bounds, and operate only the enumerated export, erasure, legal-hold, and retention workflow family in FR58 under its independent current TenantOwner approval and source-owner authority guards. It cannot operate association, conversation, Project, attachment, task-intent, AI-action, approval, outbound, or other collaboration workflow items. An independent second authorized admin must approve each security-sensitive change.
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
- FR81a — **Shared command pipeline (architectural invariant).** Every state mutation from UI, CLI, MCP, service client, AI actor, worker, or mailbox event passes through one command spine. Universal admission applies authentication, tenant binding, authorization, stable operation identity, expected revision or an A13-approved owner concurrency guard, and canonical audit-envelope construction. The central pipeline selects a closed operation/effect-class profile: AI risk and approval validation apply only to declared AI-mediated effects, while authenticity, separation-of-duty, data-rights/hold, projection, and notification guards apply to their declared classes. Adapters and handlers cannot select, replicate, or bypass stages. The domain event, idempotency state, applicable policy/approval references, and canonical audit envelope commit atomically in EventStore or a transactional outbox; otherwise nothing commits. Detailed profiles are in `addendum.md` §Shared Command Pipeline.
- FR82: Authorized CLI users can perform the singular M1 parity set: intake-status inspection, candidate review, confirm/reject/defer/correct association, attachment storage/status inspection, task-intent capture/status, AI-action approval, approved-command execution, retry, operation status, and audit lookup.
- FR83: A current `mcp-human-delegated-client` can access the delegating human's authorized M1 parity set, with user presence and `actorType=human` required for each human-decision mutation. An `mcp-tool-client` is limited to its closed actor-visible query/proposal/eligible-low-risk set and is structurally denied association decisions, `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, approved execution, outbound, policy, permission, and admin mutations. MCP exposure never extends the AI-invocable allowlist.
- FR84: The system can return equivalent authorization outcomes and state transitions across UI, CLI, and MCP. **This is a verification of FR81a, not the enforcement mechanism**: if the pipeline invariant holds, equivalent outcomes follow by construction; if equivalent outcomes diverge across surfaces, the divergence is a defect against FR81a.
- FR85: The system can identify whether an action originated from UI/API, CLI, MCP, background worker, mailbox event, or AI actor. Origin is attached at the adapter boundary and travels with the Command record into the audit envelope; downstream pipeline stages cannot mutate origin.
- FR86: Contract tests must verify the FR81a invariant for each surface: given an equivalent input, each surface adapter must produce the same Command record (after canonical normalization). Test failure is an invariant violation, not a tolerance threshold. Contract-verifiable responses with stable error codes follow as a downstream consequence of FR81a; enforcement of parity is structural, not test-derived.

### Workflow State, Contracts, and Testability

- FR87: The system can define canonical lifecycle states for email intake items, participant resolution, attachment handling, approvals, AI actions, command executions, and audit projection.
- FR88: The system can validate inbound and outbound workflow state transitions against an explicit state model.
- FR89: The system can reject invalid state transitions and record the rejected transition, actor, reason, and correlation context.
- FR90: The system can expose stable `operation_id` values for durable mutations, one authoritative `decision_slot_id` per human-decision subject, expected revisions for concurrency, and stable resource IDs for mailbox events, messages, attachments, approvals, retries, outbound communication, and audit records.
- FR91: The system can separate immutable source records from derived project projections and rebuild derived projections from source records when needed.
- FR91a — **Correction propagation contract (M0/M1).** `CorrectEmailProjectAssociation` freezes a versioned correction impact manifest covering every ChatBot-derived store, source-owned Conversations/Folders record, and effectful consequence of the prior association, including approved/executed AI actions, appended messages, converted intents, sent mail, external/tool effects, and file disclosures. ChatBot orchestrates only owner-supported commands: Conversations and Folders retain mutation authority, and their required reassignment/compensation commands and authenticated acknowledgements remain an open A13 M0 gate. Every item records repair/rebuild completion or an explicit irreversible-effect disposition of `contained`, `compensation-required`, or `cannot-repair`. The canonical path is `Associated -> Correcting -> CorrectionDelayed | Corrected`; `Corrected` is terminal only after the complete manifest is dispositioned. AI actions cannot use any affected source/destination context until completion, and audit preserves the predecessor, manifest, owner acknowledgements, and outcomes.
- FR92: Authorized product or QA users can maintain internal evaluation datasets derived from consented, redacted, or synthetic project examples with expected outcomes, redaction expectations, and regression result history.
- FR93: The system can provide tenant-scoped test fixtures or sandbox data for validating mailbox intake, association, authorization, attachment handling, approval, AI mediation, command execution, and audit behavior.
- FR94: The system can expose measurable operational outcomes for ingestion latency, association latency, approval latency, command execution latency, retry exhaustion, duplicate suppression, and audit projection lag. Exposure surface and SLO targets are defined in NFR42a (OpenTelemetry metrics published to the tenant operational dashboard in M2; intermediate exposure via the FR67 operational queues in M0/M1).
- FR95: The system can simulate or replay representative mailbox events for authorized QA or support investigation without sending external communication or mutating production project state.
- FR95a — **Replay isolation contract (M2).** Replay uses a replay-only composition root with no production credentials or locators, replay-safe mail/model/tool/command/file/state/queue/outbound adapters, and default-deny egress. Every replay carries `replay_run_id`, remains excluded from production audit-completeness measurement, and proves before/after invariance for all production stores and external-resource ledgers. Credential composition, egress, or invariance failure blocks M2. Detailed mechanism is in `addendum.md` §Replay Isolation.
- FR96: The system can make recorded correction decisions available as future association evidence only when tenant policy permits, the evidence remains explainable, and users can inspect why it influenced a match.

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
- NFR8: AI actors must operate only through explicitly authorized Project scope, files, tools, commands, and policy-defined authority. External/retrieved content and model/tool output cannot create authority, alter system/tool instructions, approve a proposal, or expand a command scope.
- NFR9: AI prompts, retrieved context, generated outputs, tool results, and summaries must be tenant/Project scoped, redacted where policy requires, logged according to retention policy, and blocked from training, telemetry, or reuse outside authorized boundaries unless explicitly configured. Validation must prove every context fragment retains immutable origin/trust and instruction/data-boundary labels, and every AI context package contains tenant ID, Project ID, source evidence references, policy snapshot ID, redaction decision, retention class, and provider reuse setting before model or tool invocation. A5 adversarial fixtures must prove instruction-like text in email, threads, attachments, filenames, retrieved context, and tool results cannot redefine policy/authority or bypass review.
- NFR9a — **Derived-store cross-tenant isolation (M0+).** Each derived store must be tenant-partitioned below the application and pass native-store/API negative isolation tests in the first increment that introduces it. M0/M1 gates cover their record classes before pilot or machine-surface exposure; M2 covers vector/embedding/prompt-cache stores and adds nightly recurring probes. Any cross-tenant read or unproven store blocks its increment and multi-tenant use. See FR55a.
- NFR10: Logs, metrics, traces, support bundles, and test artifacts must pass secret and sensitive-data redaction checks before export or external sharing.
- NFR11: Cross-tenant isolation testing must have zero tolerance for unauthorized data exposure across project candidates, evidence, files, summaries, prompts, CLI output, MCP payloads, logs, metrics, traces, and audit views.
- NFR12: Data residency and region boundaries must be defined for stored email content, attachments, AI context, audit records, logs, backups, and evaluation datasets before tenant onboarding when a tenant or deployment profile specifies residency; release validation must verify that each persisted data class is mapped to an approved region or explicitly marked not residency-constrained.

### Reliability and Data Integrity

- NFR13: Mailbox intake, attachment capture, association/approval decisions, command execution, outbound communication, retries, and audit projection must converge for repeated equivalent inputs. Durable mutations use lifetime-stable `operation_id` values; human decisions use one authoritative `decision_slot_id`; expected revisions resolve concurrency unless A13 records an owner-accepted equivalent uniqueness/concurrency contract for an append-only operation.
- NFR13a — **Per-operation idempotency contract.** Durable identities, equivalence rules, and typed conflict responses are specified in `addendum.md` §Idempotency Keys. Time-window hashes may suppress non-mutating proposals but never allow a durable mutation or decision to execute twice. New operation classes extend the table before shipping.
- NFR14: Duplicate mailbox delivery must not create duplicate project messages, attachments, task intents, approvals, commands, notifications, outbound emails, or audit decisions.
- NFR15: Invalid workflow state transitions must be rejected before mutation with deterministic error behavior and an audit event. If audit storage is unavailable, **every** state-mutating transition must fail closed. This rule applies to all mutations because the system cannot reliably classify security sensitivity before execution; otherwise, a misclassified operation could mutate without an audit record. The enumerated code paths and fail-closed contract are in NFR15a.
- NFR15a — **Fail-Closed Contract (invariant, not behavior).** Every durable write uses the FR81a atomic boundary: domain event, idempotency state, applied policy/approval references, and canonical audit envelope all commit or none commit. The path inventory and fail-closed conditions are:

  | Code path | State written | Fail-closed condition |
  |---|---|---|
  | M365 mailbox intake/authenticity | authenticity record and, only when accepted, message/attachment intake plus audit | tenant scope unresolved · malformed provider identity · authenticity evidence unavailable · canonical audit durability unavailable · attachment scanner down (quarantine only when audit durability is available); strict anomaly enters review, paranoid anomaly enters blocked, and neither creates association state |
  | Association decision (deterministic) | association record, audit event | AssociationScorer error · authorization failure · canonical audit durability unavailable |
  | Association decision (ambiguous, user) | association record, audit event | user lacks Project authority · candidate evidence expired · canonical audit durability unavailable |
  | Correction | correction record, complete immutable correction impact manifest, owner-command requests and authenticated acknowledgements/dispositions, derived-context invalidation, audit event | current authority on either source or destination Project unavailable · required Conversations/Folders owner contract unavailable · complete manifest cannot be frozen and committed atomically with correction start · manifest persistence/isolation/A6 treatment unavailable · invalidation queue unavailable · canonical audit durability unavailable · acknowledgement/disposition is missing, invalid, stale, unauthorized, or not a member of the frozen manifest |
  | Participant resolution | participant-resolution state, audit event | current Party/tenant evidence unavailable · reviewer lacks authority · canonical audit durability unavailable |
  | Attachment outcome | scan/storage state, governed file reference, audit event | scanner verdict unavailable · Project/file authority unavailable · canonical audit durability unavailable |
  | Task-intent disposition | task-intent state, successor link, audit event | detector artifact invalid · reviewer lacks authority · canonical audit durability unavailable |
  | Governed chat admission/stop/cancel | attempt state, response/proposal reference, audit event | actor/Project authority unavailable · stale attempt revision · canonical audit durability unavailable |
  | AI action proposal | proposal record, audit event | deployed classifier artifact missing/invalid · risk classifier indeterminate · audit durability unavailable; classifier failure returns `classifier-indeterminate`, writes no proposal/idempotency state, and may retain only the separately typed non-mutating attempt record |
  | Approval decision | approval record, audit event | reviewer lacks authority · stale proposal revision · canonical audit durability unavailable |
  | Command execution | command result, projection, audit event | command not exposed to actor/surface · stable operation check fails open · authorization failure · canonical audit durability unavailable |
  | Outbound send (M1+) | outbound record, audit event | sender authority mismatch · adapter not approved · canonical audit durability unavailable |
  | Tenant policy mutation | policy snapshot, audit event | actor lacks policy scope · schema/invariant failure · canonical audit durability unavailable |
  | Allowlist mutation (M1+) | allowlist version, audit event | actor lacks security authority · qualification gate not passed · canonical audit durability unavailable |
  | Service-client grant/revocation | permission version, audit event | tenant/resource authority unavailable · scope/expiry invalid · canonical audit durability unavailable |
  | Emergency safety control or ChatBot admin-role grant | control/grant version, audit event | initiating or independent owner authority unavailable · control/role grammar invalid · stale subject revision · canonical audit durability unavailable |
  | Queue/admin control | partition or assignment state, audit event | opaque partition scope invalid · per-item Project authority unavailable · canonical audit durability unavailable |
  | Resolution annotation or decision supersession | annotation/successor decision link, audit event | target record unavailable · actor/revision invalid · canonical audit durability unavailable |
  | Data export/erasure/legal hold/retention | request, hold, disposition, audit event | A6 policy/authority unavailable · legal-hold conflict · canonical audit durability unavailable |
  | Notification routing/delivery | routing version or delivery state, audit event | recipient authority/redaction/routing unavailable · canonical audit durability unavailable |

  No path has an "audit unavailable -> continue" branch. Canonical audit durability failure returns typed `AuditUnavailable`, writes no domain/idempotency state, retains only a non-authoritative caller intent where permitted, and alerts operators. Rebuildable audit projections may lag after a successful atomic commit and are governed separately by projection SLOs.
- NFR16: Risky AI actions, external sends, command execution, and Project-file context packaging must not execute unless approval state and lifetime, proposal/content/resource digests, policy snapshot, requester and human-approver authority, approver independence, sender evidence, input contract validation, target revision, and audit readiness are current. Approval TTLs and material-drift invalidation follow `addendum.md` §Approval Freshness.
- NFR17: Partial failures must leave affected workflow items in visible, recoverable states such as pending, retryable, failed, quarantined, or needs review.
- NFR17a — **Correction propagation latency.** Every correction-impact-manifest item reaches an owner acknowledgement or irreversible-effect disposition within **p95 ≤ 10 minutes** for M0/M1 (no vector index dependency) and **p95 ≤ 60 minutes** for M2 (including vector reindex). Outstanding items beyond the SLO enter `CorrectionDelayed` with the responsible owner role and next safe action; the workflow cannot reach `Corrected`. Failure of any ChatBot store or source-owner repair/compensation acknowledgement is a P2 incident. [ASSUMPTION A11: M2 vector-reindex SLO is a starter value calibrated against pilot data volumes during M2.]
- NFR18: Every workflow uses the versioned safe-default retry profile in `addendum.md` §Retry Profiles or a stricter approved tenant/deployment profile. The System Architect and Test Architect approve each profile before the workflow's first increment gate; conformance tests verify retryable/terminal reasons, maximum attempts, exponential backoff with jitter, dead-letter/exhaustion behavior, manual recovery command, immutable predecessor/successor links, and no repeated committed effect.
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
- NFR40: Degraded, blocked, failed, and waiting states must be communicated in user-appropriate language with next-action guidance drawn from the versioned message catalog defined in FR77. Every degraded, blocked, failed, or waiting state surfaced to a user must resolve to a message-catalog entry. Production telemetry must count uncategorized states, including any raw error text shown to a user. The count must be `0` for each release; any nonzero count blocks release.
- NFR41: Degraded dependencies must be isolated to the narrowest identified scope among tenant, mailbox, project, operation, service client, workflow item, or command surface; incident status must state the affected scope and dependency within 5 minutes of detection when monitoring is available.
- NFR42: During degraded operation, every authorized user-facing surface displays, at minimum: the current state enum (per FR67), the affected scope (tenant / mailbox / project / operation, per NFR41), the responsible owner role for resolution, and the next safe action affordance (per FR76). The display refreshes within the bounded staleness in NFR6. Observable: synthetic checks assert that degraded-state surfaces render all four elements; missing any element fails the synthetic.
- NFR42a — **SLOs published.** SLOs for ingestion latency, candidate generation latency, ambiguous-resolution time, command latency (per command class), audit projection lag, retry exhaustion rate, duplicate suppression rate, mailbox failure rate, approval queue p95 age, and AI mediation latency must be published in the per-tenant operational view and promoted from the normative backlog in `addendum.md` §Operating Baselines only after A11-M2 passes. Each SLO has target, measurement window, error budget, alert threshold, live-signal provenance, route, and exact-candidate burn evidence; unsupported rows block the M2 claim.
- NFR43: Alerting and synthetic health checks must be non-invasive, tenant-safe, and tied to documented thresholds for mailbox subscriptions, Graph permissions, ingestion backlog, approval aging, retry exhaustion, duplicate spikes, authorization failure spikes, audit projection, command execution, and AI mediation; default MVP thresholds must include subscription expiry within 7 days, retry exhaustion, audit projection lag above 5 minutes, approval items older than 2 business days, and authorization failure spikes above the tenant baseline.
- NFR44: Runbook-ready diagnostics for any single workflow item must include, at minimum: correlation ID, tenant ID, mailbox ID, workflow item ID, current state, last transition (timestamp + actor + from-state), retry count, failure reason code (from the FR77 message catalog), and the next safe action affordance. Observable: each week, randomly sample `100` workflow items. Every sampled item must render a complete diagnostic; any item missing a required field is a defect. "Runbook-ready" means an on-call engineer with no prior context can reach the correct next step from the diagnostic alone.
- NFR45: Support diagnostics must be shareable through redacted support bundles that preserve correlation, state, and reason context without exposing restricted tenant, project, participant, file, message, or audit evidence.
- NFR46: The system must prevent approval fatigue with concrete, measurable mechanisms:
  - **Prioritization:** the approval queue orders items by `(risk-class × authority-of-affected-party × time-in-queue)`, configurable through `approval.priority-weights` (see `addendum.md` §Tenant Policy Schema).
  - **Grouping:** grouping is presentation-only by default. One UI submission may carry several per-item decisions only when every item exposes identical frozen security-relevant values for requester, command, Project, recipients, files, content digest, sender authority, tool target, effect classification, policy version, approval revision, and expected resource revision. Irreversible, external-send, file-exposing, external-tool, and on-behalf actions are excluded from one-click batch approval. Every item is individually visible, authorized, revision-checked, decided with its own `decision_slot_id`, and atomically audited; one failed item cannot authorize another.
  - **Suppression / rate ceiling:** a per-user notification rate ceiling of `<= 8` push notifications per hour and `<= 30` per day (starter values per A11), with the remainder rolled into a digest. Equivalent non-mutating proposals within `proposal.replay-window` suppress automatically; durable decisions and mutations never expire from deduplication.
  - **Backlog SLO:** if any individual reviewer has `> 25` open approval items, the system surfaces a load alert to the tenant admin per NFR43.
  - **Observable:** median and p95 time-in-queue plus SM-C3. More than `15%` rubber-stamp approvals in a rolling 7-day window triggers routing, grouping, notification, or staffing review; mandatory-approval effects cannot be downgraded.
- NFR47: Risky automation must distinguish reversible, supersedable, compensating, and irreversible actions before approval.
- NFR48: Every surfaced evidence reference (association evidence, mailbox permissions, policy snapshots, AI context packages, audit projections) carries a visible freshness indicator: the snapshot timestamp and a state enum `fresh` / `stale` / `expired` derived from the bounded staleness window defined in NFR6 for that evidence class. Reviewers cannot approve an action against `expired` evidence; the approval surface disables `approve` with reason `evidence-expired`. `stale` is permitted but visually flagged. Observable: the AI action approval surface renders a per-evidence freshness chip; chip count must equal evidence-reference count on every approval render.

### Auditability, Compliance, and Data Governance

- NFR49: Audit records must be tamper-evident, retention-governed, redaction-aware, reconstructable, and protected by restricted modification/deletion controls limited to authorized retention workflows.
- NFR49a — **Tamper-evident audit and data-protection gate (M0+).** Canonical audit envelopes are append-only and hash-linked within each aggregate command stream under the concurrency contract in `addendum.md` §Shared Command Pipeline. Signed per-tenant checkpoints anchor stream heads without pretending to be part of each aggregate transaction. Fork, reordering, or checkpoint verification failure alerts Security within 5 minutes and triggers P1. Exact retention, legal-hold precedence, key granularity, backup propagation, crypto-erasure, and surviving metadata are governed by A6. Until A6 and A13 are approved, pilot onboarding and any claim of GDPR satisfaction or tamper-evident completeness are blocked; later increments revalidate both approvals.
- NFR50: Canonical mutation envelopes and security-sensitive non-mutating-attempt records must include tenant, actor, actor type, command/query, resource, decision, reason, correlation, timestamp, policy snapshot reference, source evidence references, state-transition history, redaction decisions, stable operation/decision identity where applicable, and resulting command, projection, or outbound outcome. Automated tests verify required field presence for `100%` of durable mutations and `100%` of sampled denials, restricted reads, and service-client failures in the validation dataset.
- NFR50a — **Canonical completeness (M0+) and projection availability (M2).** `100%` of committed durable mutations must have an atomically committed canonical audit envelope; absence is an invariant violation, not an allowed error budget. The rebuildable investigation view must reconstruct complete chains for `>= 99.5%` of operations per rolling 7-day tenant window at M2; lower availability triggers P1 and projection rebuild. Replay events are excluded per FR95a.
- NFR51: Audit and diagnostic records must preserve enough context to reconstruct who acted, what was attempted, which policy applied, what evidence was used, what state transitions occurred, what was redacted, and what outcome occurred.
- NFR52: The system must minimize retained email content, attachment content, prompts, outputs, diagnostics, and support bundles to the data required for the authorized workflow, audit, and tenant retention policy.
- NFR53: Tenant data retention, export, and deletion workflows must distinguish data classes including source email, metadata, attachments, derived projections, AI prompts and outputs, approvals, policy snapshots, logs, backups, evaluation datasets, and audit records.
- NFR54: Audit evidence must respect retention boundaries and redaction rules so evidence preservation does not become uncontrolled data storage.
- NFR54a — **Recovery evidence publication boundary.** The current-run diagnostic artifact is metadata-only and has neither story-completion nor A10 authority. Published story-completion authority contains only the independently validated, policy-allowed canonical evidence and excludes raw producer output and tenant diagnostics. Retained scheduled/release operational evidence follows the separate A10 retention/freshness policy; none of these three evidence classes may substitute for another.
- NFR55: Where tenant policy or regulatory profile requires it, the system must record consent or lawful-basis metadata for external participants, retained content, attachments, and AI processing.

### Recovery and Continuity

- NFR56: Source email records, attachments, approvals, commands, policy snapshots, and audit records target RPO <= 15 minutes and RTO <= 4 hours. These targets remain provisional under A10 and block M2 production/release-candidate approval until fresh qualifying evidence exists; stricter tenant profiles remain permitted.
- NFR57: Derived projections must be rebuildable from immutable source records and audit history within the default MVP recovery target of 4 hours for the baseline validation dataset without requiring mailbox re-ingestion.
- NFR58: Dependency outages must degrade only the affected tenant, mailbox, operation, service client, command surface, or workflow item when dependency ownership and routing can identify that scope; outage tests must prove no unrelated tenant or mailbox is blocked for Graph, identity, AI provider, command execution, audit store, and attachment-processing failures.
- NFR59: Resilience validation must prove degraded Graph access, expired subscriptions, AI provider outage, command failure, audit-durability unavailability, and partial attachment failure do not cause cross-tenant leakage, unauthorized/unaudited mutation, or silent loss. Each store also carries first-increment native isolation proof and M2 recurring probes.

### Accessibility and Usability Quality

- NFR60: Each increment's delivered UI surfaces must meet WCAG 2.2 AA before release. This phased scope aligns the requirement with the surfaces delivered in that increment and available frontend capacity. CLI and MCP are outside WCAG scope. In-scope surfaces by increment:
  - **M0 surfaces (must conform before M0 release):** inbound authenticity review, ambiguous association review, AI action approval, and Project conversation view.
  - **M1 surfaces (must conform before M1 release):** governed chat composer `S1a` including streaming/stop/cancel/retry/failure states, correction, outbound approval, approval-policy configuration, and the M1 admin view.
  - **M2 surfaces (must conform before M2 release):** operational dashboards, compliance investigation, and admin queue operations.
  - Validation per increment must include automated checks plus keyboard-only and screen-reader review of each in-scope surface before that increment's release. Surfaces marked "post-MVP" or "vision" are not in NFR60 scope. If accessibility consultancy is engaged later, the bar can be widened — but the per-increment scoping enumerated here is the floor.
- NFR61: Accessibility validation must include keyboard-only review flows, screen-reader labels, focus order, non-color status indicators, and error recovery for ambiguous association and approval workflows.
- NFR62: Status, failure, refusal, and authorization messages must be understandable without exposing restricted evidence or relying only on color.
- NFR63: Users resolving ambiguous associations or approvals must be able to identify the next available action without reading raw audit logs.
- NFR64: The UI must distinguish source evidence from AI-generated summaries so users can make review decisions from authoritative context.

### Validation and Quality Gates

- NFR65: Increment and production releases must meet the gate table in §Minimum Release Slice, including tenant isolation at store introduction, authorization/redaction, stable idempotency, executable transitions, non-downgradable approvals, authenticity, duplicate suppression, atomic audit, accessibility, evidence ownership, and explicit disable conditions. M0/M1 permit only their stated pilot claims; M2 is the first production/release-candidate claim.
- NFR65a — **Recovery completion evidence integrity.** A transition-declared completion check must bind to the exact candidate revision and approved evidence policy and accept exactly one current-run recovery producer. It must fail if planning, production, timeout/no-test, restoration, cleanup-receipt finalization, evidence projection, attestation, independent validation, or publication fails. Diagnostic, completion-authority, and retained operational artifacts use disjoint channels. The accepted Epic 12 contract remains inactive until its stated activation preconditions are independently verified; no artifact produced before activation may claim its authority, and retained operational evidence cannot satisfy the story-completion gate.
- NFR66: Performance validation must prove mailbox backlog processing, queue usability, retry behavior, audit projection lag, and throttled Microsoft Graph behavior against documented tenant or deployment baselines.
- NFR67: Security validation must include negative authorization tests for UI/API, CLI, human-delegated MCP, AI/tool MCP, background workers, mailbox events, service clients, and AI actors, including proof that AI/tool/service credentials cannot approve, reject, request revision, or self-approve an AI-originated proposal.
- NFR68: Evaluation datasets and test fixtures must use consented, redacted, or synthetic examples with versioning, reproducibility, redaction verification, expected outcomes, and regression result history for association, authorization, duplicate handling, retry, approval, refusal, and audit behavior.
- NFR69: Replay/simulation must exclude production credentials/resources by construction, replace every effectful adapter, enforce default-deny egress, label and tenant-scope artifacts, and prove before/after invariance for production stores and external-resource ledgers.
- NFR70: Every externally visible operation must define expected state transition, audit event, user-visible response, redaction behavior, and retry/idempotency result.

## Open Assumptions and Decisions

These assumptions are explicit so UX, architecture, and story creation can confirm or replace them before implementation locks in downstream commitments.

| ID | Assumption or decision | Owner | Revisit condition |
| --- | --- | --- | --- |
| A1 | The MVP pilot can use Microsoft 365 / Exchange as the first controlled mailbox integration without requiring another mailbox provider. | Product / Architecture | Revisit if the first pilot tenant cannot grant acceptable M365 permissions or primarily uses another mailbox provider. |
| A2 | The minimum release slice may use one controlled mailbox pattern before supporting multiple aliases, shared mailbox edge cases, forwarded-thread inference, or complex mailbox histories. | Product / Architecture | Revisit after the deterministic and ambiguous association paths pass pilot validation. |
| A3 | M1 exit requires the singular parity set in §Cross-surface parity outcomes. An earlier association/status/audit proof may de-risk implementation but cannot satisfy the M1 gate. | Product / Developer Experience | Revisit only through an explicit PRD scope decision; do not silently reduce the exit set. |
| A4 | Default tenant/deployment operating baselines in NFR23-NFR30 are acceptable until a tenant-specific baseline is approved. | Architecture / Operations | Revisit during tenant onboarding, capacity planning, or quarterly baseline review. |
| A5 | AI provider telemetry, training reuse, retention, and region behavior can be configured to satisfy tenant policy before live AI invocation. Approval additionally requires evidence that instruction/data boundaries and immutable origin labels survive context extraction, model invocation, tool results, and output, with adversarial prompt-injection fixtures for email bodies, quoted threads, attachments, filenames, retrieved Project context, and tool results. | Security / Architecture | Revisit before selecting or enabling an AI provider for tenant data or when the model, extraction/context pipeline, tool surface, or provider policy changes. |
| A6 | **Pre-pilot blocker:** Compliance/Data Protection and Architecture must approve an evidenced data-class contract. Evidence must cover purpose and legal basis; retention and legal-hold precedence; key granularity and production key custody; EventStore payload protection and erasure; the Parties adapter; backup and restore propagation; surviving metadata; export and deletion; and audit proof. Contract interfaces alone do not close the gate, and the pinned Parties development key backend is not approved for regulated personal data. The PRD makes no GDPR-satisfaction claim until the decision and runtime evidence are attached. | Compliance / Data Protection + Architecture; Parties and EventStore owners supply runtime evidence | Must resolve before any pilot data or live external-party PII is onboarded; reopen on retention, hold, key/backend, backup, erasure, or adapter change. |
| A7 | External participants do not need authenticated portal access in MVP; party resolution and email governance are sufficient for the first workflow. | Product | Revisit if pilot users need external review, approval, or correction actions from outside email. |
| A8 | The deny-by-default AI allowlist v1 contains exactly `Project.AppendConversationMessage` and `ChatBot.ExecuteLowRiskAssistance`; tenant policy may disable or pin but cannot add members or weaken mandatory approval. | Product / Architecture / Security | A new member requires a new PRD decision, immutable allowlist version, security sign-off, and qualification. |
| A9 | The evaluation dataset can be built from consented, redacted, or synthetic examples that represent realistic mailbox ambiguity and authorization failures. | Test Architect (single named owner; Product Lead consults on sampling representativeness) | Revisit if dataset results fail to predict pilot behavior or miss important edge cases. |
| A9a | Separate offline qualification partitions cover association (`deterministic-match`, `ambiguous-match`, `no-match`, `unauthorized-project`, `cross-tenant-reference`, `duplicate`, `attachment-only`, `inbound-authenticity-anomaly`), task intent (`informational`, `request-information`, `request-action`, `request-decision`), and action risk/reviewer disagreement. Cardinality is at least `500` messages by M0 and `2000` by M1, with at least `20` new adversarial examples per cycle. Exact TaskIntentDetector and ActionRiskClassifier record fields, denominators, samples, thresholds, expiry/drift, and M0/M1/M2 stop-ship behavior are normative in `addendum.md` §Detector and Classifier Qualification Records. M2 revalidates the M1 qualification thresholds and production-sampled result against the exact deployed release candidate and dependencies rather than introducing a new threshold. | Test Architect | Revisit if any contract misses its target, a record expires/drifts, the artifact, dataset, candidate, or dependency binding changes, or production sampling diverges; failure invalidates the affected artifact qualification and blocks its increment use, including M2 production release. |
| A10 | RPO <= 15 minutes and RTO <= 4 hours remain provisional. The accepted Epic 12 recovery-provenance architecture is `activation: pending`; its current-run story-completion check, diagnostic artifact, and completion-authority artifact are not A10 evidence. No fresh hosted bundle identified by this update passes the current four-job gate; the 2026-08-27 bundle is historical, expired, lacks controlled-loss evidence, and cannot exercise the four-hour boundary. Current run details live in `qualification-evidence.md`. | Architecture / DevOps | M2 stop-ship until the recovery architecture activation preconditions are independently verified and a separate fresh exact-candidate hosted controlled-loss artifact plus an RTO-capable full-window or separately retained production-shaped drill pass independent validation. |
| A11 | **Split qualification.** A11-M1 is an M1 stop-ship gate: before its observation window, an approved-current gate record freezes every mandatory M1 metric definition, denominator, supported-request mix, target, minimum sample/window, evidence source, owner, and pass/fail rule. The mandatory A11-dependent M1 measures SM8, SM16, SM-C3, and SM-C5 cannot pass M1 until that record exists; SM12 and SM15 retain their defined count/use contracts but are recorded in the same evidence bundle. A11-M2 is the additional production gate requiring every M2 success measure and SLO row to have its final target/denominator plus numeric SLO target, error budget, live-signal provenance, alert route, and passing exact-candidate burn test. | Product Lead + Test Architect for A11-M1; Product Lead + Architecture/Operations/Test Architect for A11-M2 | Reopen A11-M1 on metric, supported-mix, dataset, or pilot-profile change; reopen A11-M2 on SLO, signal, route, candidate, or burn-test change. Unsupported records block the associated M1 or M2 claim. |
| A12 | `IdentityEvolved` is a proposed external dependency, not a binding event, until each producer accepts its versioned schema, authority, ordering, replay/idempotency, compatibility, reconciliation query, and fallback. | System Architect + producer teams | Resolve before relying on automatic cross-context identity migration. |
| A13 | **Pre-pilot cross-context contract blocker:** current sibling revisions do not supply every claimed execution guarantee. Before M0, owners must accept the Conversations append mapping and lifetime idempotency/concurrency rule; Conversations ownership of conversation-to-Project assignment; Conversations and Folders correction reassignment/compensation commands plus authenticated acknowledgements for FR91a; a closed ChatBot-role-to-Tenants/EventStore/Project-permission mapping; EventStore ownership or an approved alternative for atomic canonical mutation audit; and supported-write-path ACL/fencing evidence. The original nine-context review baseline is `reconcile-full-sibling-a13-2026-09-14.md`; `source-manifest.md` supplies the refreshed current revisions, hashes, and open correction-contract rows. Neither artifact claims owner acceptance. | System Architect + Conversations, Folders, Projects, Tenants, and EventStore owners; Security validates authority/fencing | M0 stop-ship until every mapping is versioned, owner-accepted, contract-tested, and recorded in the manifest; reopen on consumed-contract change. |


## Normative Reference

### Shared Workflow Contract

Inbound authenticity is a distinct pre-association workflow. `CaptureMailboxEvent` records provider/header/delegation evidence and produces one of `AuthenticityAccepted`, `AuthenticityReviewRequired`, or `AuthenticityBlocked`. The safe `strict` profile sends anomalies to `AuthenticityReviewRequired`; `paranoid` sends anomalies to terminal `AuthenticityBlocked`. A current `mailbox-admin` initiates `ResolveInboundAuthenticityReview`, and an independent current `policy-admin` approves the exact evidence digest and decision before the command reaches `AuthenticityAccepted` or terminal `AuthenticityRejected`. Accepted intake starts the association workflow in `Received`; blocked/rejected intake never exposes candidate Projects or enters association. `ReprocessInboundAuthenticity` requires initiation by a current `mailbox-admin`; approval by an independent current `policy-admin`; changed policy/provider evidence; the terminal record's expected revision; and a stable operation ID. On success, it creates one new audit-linked intake instance. Replay returns that successor, and the command never reopens a terminal record in place. Evidence visibility is mailbox-scoped and redacted. Retention is bound by A6. Every state exposes a stable reason, owner, and next safe action.

The journeys share a common association lifecycle across UI, CLI, and MCP:

`Received -> Associated | NeedsReview | Failed | Skipped`

`NeedsReview -> Associated | Rejected | Deferred`; `Deferred -> NeedsReview`

`Associated -> Correcting -> Corrected`; `Correcting -> CorrectionDelayed`; `CorrectionDelayed -> Corrected`

Correction is a superseding workflow that preserves the original association, every affected owner record, and every downstream effect in audit history. `Corrected` is reached only after every frozen correction-impact-manifest item has an owner acknowledgement or explicit irreversible-effect disposition.

Canonical state definitions:

| State | Meaning | Terminal |
| --- | --- | --- |
| `Received` | The mailbox event or replayed message has been captured with tenant, mailbox, and source message identity. | No |
| `Associated` | An automatic or human-confirmed association links the email-derived context to one project. | No |
| `Rejected` | The reviewer rejected all candidate associations for the message. | **Yes (terminal).** Reprocessing creates a new `Received` workflow with a new ID and bidirectional supersession links; the original record remains unchanged. |
| `Deferred` | The reviewer chose not to decide yet; the item remains visible with next action and owner. | No |
| `NeedsReview` | The item requires human review because evidence, party identity, authorization, dependency status, or policy context is incomplete. | No |
| `Failed` | Processing reached a terminal failure that requires manual intervention or explicit reprocessing. | **Yes (terminal).** Reprocessing follows the same rule as `Rejected`: a new workflow instance is created with a new ID; the original `Failed` record is preserved. |
| `Skipped` | The item is intentionally not processed, such as duplicate suppression or out-of-scope mailbox rule. | Yes |
| `Correcting` | The correction and its frozen impact manifest are recorded, but one or more owner repairs, compensations, derived-store rebuilds, or irreversible-effect dispositions remain outstanding. AI actions cannot use any affected source/destination context. | No |
| `CorrectionDelayed` | The correction exceeded the NFR17a SLO with manifest items outstanding. The state exposes the responsible owner and next safe action and triggers a P2 incident. | No (incident-flagged) |
| `Corrected` | Every frozen manifest item has an immutable owner acknowledgement or an explicit `contained`, `compensation-required`, or `cannot-repair` disposition; the prior association and effects remain audit-visible. | **Yes for that correction workflow.** A later correction creates a new audit-linked workflow from the current authoritative association. |

The following versioned matrix is the authoritative M0/M1 association transition contract. Every mutating command carries a stable `operation_id`, expected workflow revision, actor authority, reason, and canonical audit envelope.

| Source | Command / actor | Guard or reason | Destination | Increment | Concurrency and audit / successor rule |
| --- | --- | --- | --- | --- | --- |
| none | `CaptureMailboxEvent` / mailbox client | The paired authenticity workflow reaches `AuthenticityAccepted` | `Received` | M0 | Accepted intake and `MessageReceived` commit atomically; review/blocked/rejected authenticity outcomes create no association workflow |
| `Received` | `AssociateEmailToProject` / association worker | `AssociationScorer >= T_high`, required deterministic evidence, no conflict, authorized target | `Associated` | M0 | Expected revision; scorer/evidence/policy versions in `AssociationSelected` |
| `Received` | `ProposeEmailProjectAssociation` / association worker | Authorized candidate evidence exists but auto-association guard is not met | `NeedsReview` | M0 | Expected revision; `AssociationReviewRequested`; candidate ranking persists with typed reason and visible-candidate rule |
| `Received` | `MarkEmailAssociationNeedsReview` / worker | No candidate, conflict, scorer error, or stale/unauthorized evidence | `NeedsReview` | M0 | Expected revision; `AssociationReviewRequired`; reason code required and unauthorized candidates omitted. Only `ResumeEmailAssociationReview` may resume `Deferred`. |
| `Received` | `SkipEmailAssociation` / worker or authorized reviewer | Duplicate or declared out-of-scope mailbox rule | `Skipped` | M0 | Terminal `AssociationSkipped`; reprocess creates a successor workflow |
| `NeedsReview` | `ConfirmEmailProjectAssociation` / authorized Project actor | Candidate/evidence fresh and actor can access target Project | `Associated` | M0 | One `decision_slot_id`; `AssociationConfirmed`; competing decision is `decision-conflict`; direct confirmation from `Deferred` is `state-not-permitted` |
| `NeedsReview` | `RejectEmailProjectAssociation` / authorized reviewer | Explicit reject-all reason | `Rejected` | M0 | `AssociationRejected`; terminal; direct rejection from `Deferred` is `state-not-permitted`; reprocess creates audit-linked successor |
| `NeedsReview` | `DeferEmailProjectAssociation` / authorized reviewer | Owner and revisit condition supplied | `Deferred` | M0 | Active item; expected revision and `AssociationDeferred` |
| `Deferred` | `ResumeEmailAssociationReview` / assigned reviewer | Revisit condition met and evidence refreshed | `NeedsReview` | M0 | Expected revision; `AssociationReviewResumed`; prior defer remains immutable |
| `Received` | `MarkEmailAssociationFailed` / association worker | Association processing exhausted retries or reached a non-retryable typed failure before decision | `Failed` | M0 | Expected revision; `AssociationFailed`; later review-command failures leave the prior state unchanged |
| `Rejected`, `Failed`, or `Skipped` | `ReprocessEmailAssociation` / authorized reviewer | Reason supplied and source evidence retained | new `Received` workflow | M0 | `AssociationReprocessed`; original terminal state does not change and successor links commit atomically |
| `Associated` | `CorrectEmailProjectAssociation` / Project owner | Current authority proven on source and destination Projects; predecessor revision current; complete impact manifest frozen | `Correcting` | M0 | `AssociationCorrectionStarted`; manifest covers ChatBot stores, Conversations/Folders records, approved/executed actions, appended messages, task-intent conversions, sent mail, external/tool effects, and file disclosures; predecessor remains immutable |
| `Correcting` or `CorrectionDelayed` | `AcknowledgeAssociationCorrectionStore` / worker or authenticated owner adapter | One manifest item records owner repair/compensation acknowledgement or irreversible-effect disposition; source-owner acknowledgement requires its A13-accepted contract | unchanged or `Corrected` | M0+ | The stable command name is retained, but it acknowledges any manifest item, not only a ChatBot store; `AssociationCorrectionImpactAcknowledged`; the final item emits `AssociationCorrected`; AI context remains blocked until completion |
| `Correcting` | `MarkAssociationCorrectionDelayed` / SLO monitor | NFR17a exceeded and manifest items remain outstanding | `CorrectionDelayed` | M0+ | Expected revision; `AssociationCorrectionDelayed`; P2 remains open until every item is dispositioned and the state reaches `Corrected` |

Deferred items are not skipped. `Rejected`, `Failed`, and `Skipped` never reopen in place; authorized reprocessing creates a new workflow ID with `supersedes_workflow` / `superseded_by_workflow` links.

Other owned workflow families use the compact authoritative contracts below. In each transition, the named catalog command is the only public mutator; internal pipeline stages are named explicitly. Every transition requires expected revision, actor authority, stable operation/decision identity, and an atomic canonical audit event. A command failure before commit leaves the source state unchanged unless the row explicitly names a `Failed` destination.

| Family | States and allowed transitions | Terminal/successor rule | Increment |
| --- | --- | --- | --- |
| Participant resolution | none → `Resolved` by `LinkOrResolveEmailParticipant`/worker when a known Party match and current authority are proven (`ParticipantResolved`); none → `Unresolved` by the same command when no authorized match exists (`ParticipantResolutionRequired`); `Unresolved` → `Resolved` by the same command/authorized reviewer with fresh identity evidence (`ParticipantResolved`); `Unresolved` → `Rejected` or `Quarantined` by `MarkParticipantResolutionDisposition`/authorized reviewer (`ParticipantResolutionDispositioned`); `Quarantined` → new `Unresolved` successor by `ResumeParticipantResolutionReview` (`ParticipantResolutionResumed`) | `Resolved` and `Rejected` are terminal for that resolution workflow; resume/re-resolution creates a linked successor. Unresolved/Quarantined actors cannot access Project context. | M0 |
| Attachment handling | none → `PendingScan` by `CaptureEmailAttachment`/mailbox worker (`AttachmentCaptured`); `PendingScan` → `Stored` by `StoreEmailAttachmentInProjectFolder`/attachment worker after clean scan and Project authorization (`AttachmentStored`); `PendingScan` → `Unsafe` or `Failed` by `MarkEmailAttachmentOutcome`/scanner or worker with typed result (`AttachmentUnsafe`/`AttachmentFailed`); `Failed` → new `PendingScan` attempt by `RetryAttachmentCapture`/authorized actor when the typed failure is retryable (`AttachmentRetryStarted`) | `Stored` and `Unsafe` are terminal for that capture; retry creates one linked attempt. Repeating the retry command returns the same successor ID and never exposes unscanned content. | M0 |
| Task intent | none → `Detected` or `NeedsReview` by `CaptureTaskIntent`/detector (`TaskIntentDetected`/`TaskIntentReviewRequired`); either state → `Converted`, `NotActionable`, `Duplicate`, `AlreadyHandled`, or `OutOfScope` by `MarkTaskIntentDisposition`/authorized reviewer (`TaskIntentDispositioned`); `Converted` links the one `ProposeAIAction` successor | Every disposition is terminal; duplicate links its predecessor and conversion links its proposal. `NotActionable` is the stored state for the user-facing “not actionable” decision in FR38. | M0 |
| AI action proposal | none → `AwaitingApproval` by `ProposeAIAction` for a boundary-crossing request only after a determinate `approval-required` classification (`AIActionProposed`) | Proposal carries immutable content/resource digests, policy version, classifier version/input tuple, expected target revision, one `decision_slot_id`, and approval expiry class. Indeterminate classification returns `classifier-indeterminate` with no proposal. | M0/M1 |
| AI action approval | `AwaitingApproval` → `Approved` by `ApproveAIAction`/authorized human reviewer with current authentication, recorded user presence, `actorType=human`, independent identity from the originating AI/tool/service principals, and current proposal/policy/authority/target (`AIActionApproved`); `Approved` → `Expired` internally at `expires_at` or immediately on material drift (`AIActionApprovalExpired`) | One expected-revision decision wins; replay returns the recorded result and a competing decision returns `decision-conflict`. AI/tool/service identities are structurally denied and cannot satisfy or co-sign the human gate. | M0/M1 |
| AI action rejection | `AwaitingApproval` → `Rejected` by `RejectAIAction`/authorized human reviewer with current authentication, recorded user presence, and a reason (`AIActionRejected`) | Terminal for the proposal; AI/tool/service identities are denied; resubmission creates a linked successor proposal. | M0/M1 |
| AI action revision | `AwaitingApproval` → `RevisionRequested` by `RequestAIActionRevision`/authorized human reviewer with current authentication, recorded user presence, and requested changes (`AIActionRevisionRequested`) | Terminal for this proposal; AI/tool/service identities are denied; any revision is a new linked proposal with new content/resource digests and decision slot. | M0/M1 |
| AI action cancellation | `AwaitingApproval` → `Cancelled` by `CancelAIAction`/requester or authorized reviewer before a decision/execution commits (`AIActionCancelled`) | Terminal; expected revision makes cancellation race deterministically with approval. | M0/M1 |
| AI action execution | `Approved` → `Executing` → `Succeeded` or `Failed` by `ExecuteApprovedProjectCommand`/mediator before `expires_at` and after revalidating proposal/content/resource digests, policy, requester and approver authority, sender evidence, and target revision (`AIActionExecutionStarted`/`AIActionExecutionCompleted`); retryable pre-effect `Failed` → one new linked `Executing` attempt by `RetryApprovedAIActionExecution`/authorized mediator after the same revalidation (`AIActionExecutionRetryStarted`) | Repeating either `operation_id` returns the recorded outcome/successor. Expiry or material drift moves approval to `Expired`; execution/retry is invalid and requires a new linked proposal and human decision. | M0/M1 |
| Low-risk assistance | none → `Executing` → `Succeeded` or `Failed` by `ExecuteLowRiskAssistance` when the subtype is product-eligible, tenant-enabled, and Project-authorized (`LowRiskAssistanceStarted`/`LowRiskAssistanceCompleted`) | It has no Project/file/task/tool/outbound effect. A boundary-crossing request returns `approval-required` without execution and is submitted separately through `ProposeAIAction`. | M1 |
| Governed chat attempt | none → `Admitted`, `ApprovalRequired`, `Denied`, `Unsupported`, or `Failed` by `SubmitGovernedChatMessage` (`ChatAttemptAdmitted` or typed terminal event); `Admitted` → `Cancelled` by `CancelGovernedChatRequest` before streaming (`ChatAttemptCancelled`), or → `Streaming` internally (`ChatStreamingStarted`); `Streaming` → `Completed`, `Stopped`, or `Failed` by completion or `StopGovernedChatResponse` (`ChatAttemptCompleted`/`ChatAttemptStopped`/`ChatAttemptFailed`); `Stopped` or `Failed` → new linked attempt by `RetryGovernedChatMessage` (`ChatAttemptRetried`) | Every transition uses expected revision. Cancellation vs streaming and stop vs completion are first-commit-wins; the loser receives the recorded terminal/current result. Each attempt is immutable after a terminal state; retry creates exactly one linked attempt under `chat_request_id`. `CancelAIAction` separately cancels an `AwaitingApproval` proposal. | M1 |
| Outbound email | none → `Drafted` by `CreateOutboundProjectEmailDraft`/authorized actor (`OutboundDraftCreated`); a linked mandatory proposal follows the AI-action contract; `Drafted` with a fresh `Approved` proposal → `Sending` → `Sent`, `Failed`, or `SendOutcomeUnknown` by `SendApprovedProjectEmail`/authorized mediator after content, recipient, authority-tuple, approval-lifetime, and revision revalidation (`OutboundSendStarted` plus typed outcome); `SendOutcomeUnknown` → `Reconciling` → `Sent`, `NotSent`, or `Unresolved` by `ReconcileOutboundSendOutcome`/mailbox worker or `mailbox-admin` using provider evidence (`OutboundSendReconciled`) | `Sent` never sends again; `SendOutcomeUnknown`, `Reconciling`, and `Unresolved` forbid retry. Reconciliation has a 4-hour default deadline; exhaustion yields `Unresolved` and P2 escalation. A new draft is permitted only after audited `NotSent`; changed content, recipients, authority, or expired approval always requires a new draft and human approval. | M1 |
| Command execution | `Received` → `Admitted` → `Committed`, `Rejected`, or `Failed` through internal command-spine stages (`CommandAdmitted` plus `CommandCommitted`/`CommandRejected`/`CommandFailed`); a typed retryable pre-commit `Failed` result → one new linked `Received` attempt by `RetryCommandExecution`/authorized actor (`CommandRetryStarted`); committed outcome projects as `ProjectionPending`, `Projected`, or `ProjectionLagging` | `Committed` never re-executes for its `operation_id`; retry replay returns the same successor ID and cannot follow a committed/rejected/non-retryable result. Projection failure does not undo canonical commit. | M0+ |
| Audit projection | `ProjectionPending` → `Projected`, `Lagging`, or `Failed` by projection worker (`AuditProjectionUpdated`); `Lagging` or `Failed` → `Projected` by `RebuildAuditProjection`/authorized operator after source-watermark and scope validation (`AuditProjectionRebuilt`) | Canonical envelope is already durable; rebuild uses a stable operation ID, changes only the projection, and reports freshness, owner, and next action. | M0+ |
| Service-client permission | none or `Revoked` → new `Active` grant by `GrantServiceClientPermission`/authorized admin after policy approval (`ServiceClientPermissionGranted`); `Active` → `Revoked` by `RevokeServiceClientPermission`/authorized admin (`ServiceClientPermissionRevoked`); expiry is an internal `Active` → `Expired` transition (`ServiceClientPermissionExpired`). For the first M0 grant only, two distinct current Tenants `TenantOwner` principals act as initiator/approver and any gate-required Security approval is also present. | Revoked/expired grants never reactivate; a later grant is a new immutable successor with bounded scope and expiry. M0 permits only the four enumerated M0 client classes; broader grants and the editor surface are M1. | M0/M1 |
| Resolution annotation | existing canonical record → same workflow state plus append-only annotation by `AddWorkflowResolutionAnnotation`/authorized actor (`WorkflowResolutionAnnotated`) | The annotation is non-authoritative, cannot change the decision, and cannot satisfy or repair canonical audit completeness. | M0+ |

The following table completes the authoritative contract for durable governance, administration, retry, data-subject, and notification mutations. All commands require the common command fields defined in §Command and Query Contracts, expected revision of the affected aggregate, and atomic canonical audit. Repeating a successful `operation_id` returns the prior result; a conflicting payload returns `idempotency-conflict`.

| Family | Stable command, actor, guard, transition, and event | Terminal/successor rule | Increment |
| --- | --- | --- | --- |
| Mailbox configuration | none or current `Active` version → new `Active` configuration by `ConfigureMailboxSource`/MailboxOwner or scoped TenantAdmin after current M365 authority and A6 validation (`MailboxSourceConfigured`); `Active` → `Paused` by `PauseMailboxSource` (`MailboxSourcePaused`); `Paused` → `Active` by `ResumeMailboxSource` after authority revalidation (`MailboxSourceResumed`); any non-disabled version → `Disabled` by `DisableMailboxSource` (`MailboxSourceDisabled`) | Versions are immutable successors; pause/resume/disable cannot broaden mailbox or Project authority. Re-enabling a disabled source requires a new `ConfigureMailboxSource` operation and full admission checks. | M0/M1 |
| Mailbox retry | terminal typed intake `Failed` → one new linked `Received` workflow by `RetryMailboxIntake`/authorized operator after dependency and source-retention checks (`MailboxIntakeRetryStarted`) | Only retryable pre-association failures qualify; replay returns the same successor. Association terminal states use `ReprocessEmailAssociation`. | M0 |
| Tenant policy | none or current version → one immutable successor by `UpdateTenantPolicy` after schema validation and required independent approval (`TenantPolicyUpdated`). For the first M0 snapshot only, two distinct current Tenants `TenantOwner` principals initiate/approve and all A5/A6/schema co-approvals are present; later mutations use the scoped admin roles in the schema. | The previous version remains audit-visible; rejected validation writes no version. M0 exposes only provisioning automation for M0 rows; the full editor is M1. Mandatory-effect downgrades are invalid. | M0/M1 |
| Command allowlist | current version → one immutable successor by `UpdateCommandAllowlist`/Security-authorized admin after qualification evidence and independent approval (`CommandAllowlistUpdated`) | Removal may disable commands immediately; addition is unusable until A8 allowlist qualification passes and any owner-executable target separately receives A13 acceptance. | M1 |
| Notification routing | none or current version → one immutable successor by `ConfigureNotificationRouting`/scoped TenantAdmin after destination and redaction validation (`NotificationRoutingConfigured`) | Configuration cannot expose Project detail without Project authority; removal creates a disabled successor. | M1 |
| Operational limits | current version → one immutable successor by `UpdateOperationalLimits`/scoped TenantAdmin after bounded quota/rate/circuit-breaker validation (`OperationalLimitsUpdated`) | Limits can restrict but cannot bypass authorization, audit, or safety gates. | M1 |
| Emergency safety control | any `Active` mailbox source, service client, AI actor, or command capability → `Disabled`, `Quarantined`, or `RateLimited` by `ApplySafetyControl`/the initiating and co-approving roles in the matrix below (`SafetyControlApplied`); any controlled subject → `Active` by `ReleaseSafetyControl` after current authority, remediation evidence, and independent approval (`SafetyControlReleased`) | Control and release are immutable successor versions. Disable/quarantine blocks new admission; rate limiting applies the recorded numeric bound. Existing committed effects are unchanged, and queued/investigation evidence is preserved. | M1 |
| ChatBot admin-role grant | none or `Revoked` → new `Active` grant by `GrantChatBotAdminRole`; `Active` → new `Active` successor by `ChangeChatBotAdminRole`; `Active` → `Revoked` by `RevokeChatBotAdminRole`/current TenantOwner plus independent authorized TenantOwner after validating the closed role/scope (`ChatBotAdminRoleGranted`/`Changed`/`Revoked`) | Service clients and AI actors cannot initiate or approve. The Tenants owner response must be current; replay returns the recorded version and scope-conflicting reuse fails. M0 permits bootstrap grants needed for M0 only; broader role management UI is M1. | M0/M1 |
| Queue partition control | `Active` ↔ `Paused` by `PauseQueuePartition` or `ResumeQueuePartition`/scoped TenantAdmin for an opaque tenant/mailbox/client partition (`QueuePartitionPaused`/`QueuePartitionResumed`) | Without Project authority the command cannot inspect, assign, retry, quarantine, dismiss, or mutate a queue item. | M1 |
| Queue item assignment | visible unclaimed or claimed item → `Claimed` or `Assigned` by `ClaimQueueItem` or `AssignQueueItem`/authorized Project actor (`QueueItemClaimed`/`QueueItemAssigned`) | Expected revision resolves competing claims; terminal workflow disposition uses the family-specific command, not a queue mutation. | M1 |
| Human-decision supersession | eligible reversible recorded decision → one new decision workflow by `SupersedeWorkflowDecision`/same-or-stronger authorized decision role with reason and current resource revision (`WorkflowDecisionSuperseded`) | The prior decision is immutable. Irreversible effects, sent messages, and completed external/tool actions cannot be superseded; correction or compensation uses its owner command. | M1 |
| Data export | none → `Requested` → `Completed`, `PartiallyCompleted`, `Rejected`, or `Failed` by `InitiateDataExport`/`compliance-admin` acting on a validated data-subject or tenant request, an independent current TenantOwner with `compliance-admin` grant approving scope, and owner-context exporters (`DataExportRequested`/`DataExportCompleted`); retryable `Failed` or `PartiallyCompleted` → one new linked `Requested` workflow by `RetryDataExport`/same initiating role after scope/policy/recipient revalidation (`DataExportRetryStarted`) | Owner contexts retain query/export authority. Result access is redacted, recipient-bound, auditable, and expires after 24 hours. Completed/rejected work cannot retry; an appeal after rejection requires new evidence/basis and creates a new `InitiateDataExport` request linked to the rejected predecessor. | M0+ before first persisted pilot data |
| Data erasure | none → `Requested` → `BlockedByHold`, `Completed`, `PartiallyCompleted`, `Rejected`, or `Failed` by `InitiateDataErasure`/`compliance-admin` acting on a validated request, an independent current TenantOwner with `compliance-admin` grant approving scope, and owner-context erasure workers (`DataErasureRequested`/`DataErasureCompleted`); retryable `Failed` or `PartiallyCompleted`, or `BlockedByHold` after the exact hold is released, → one new linked `Requested` workflow by `RetryDataErasure`/same initiating role after legal-hold, authority, and A6 revalidation (`DataErasureRetryStarted`) | Owner contexts retain deletion/crypto-erasure authority. Legal hold prevails; completion requires per-owner acknowledgements plus A6 surviving-metadata and backup-propagation evidence. Completed/rejected or still-held work cannot retry; an appeal after rejection requires new evidence/basis and creates a new linked `InitiateDataErasure` request. | M0+ before first persisted pilot data |
| Legal hold | none or released → new `Active` hold by `PlaceLegalHold`; `Active` → `Released` by `ReleaseLegalHold`/`compliance-admin` plus an independent current TenantOwner with `compliance-admin` grant, exact scope, lawful reason, and owner-context acknowledgement (`LegalHoldPlaced`/`LegalHoldReleased`) | Holds are immutable/versioned and block retention/erasure only for their exact scope; owner contexts enforce the hold and retain record authority. | M0+ before first persisted pilot data |
| Retention disposition | eligible expired record → `DispositionPending` → `Disposed`, `BlockedByHold`, or `Failed` by `ExecuteRetentionDisposition`/retention worker under the current A6 policy and owner-context authority (`RetentionDispositionRecorded`); retryable `Failed`, or `BlockedByHold` after the exact hold is released, → one new linked `DispositionPending` attempt by `RetryRetentionDisposition`/retention worker or `compliance-admin` after owner/hold revalidation (`RetentionDispositionRetryStarted`) | No deletion occurs if policy, owner authority, or hold evidence is unavailable. Completion requires per-owner acknowledgement; replay returns the same successor and disposed or still-held work cannot retry. | M0+ before first persisted pilot data |
| Notification delivery | none → `Queued` → `Sent`, `Suppressed`, or `Failed` by `DispatchWorkflowNotification`/worker after recipient authority, routing, and redaction checks (`NotificationQueued`/`NotificationDeliveryRecorded`); retryable `Failed` → new `Queued` attempt by `RetryWorkflowNotification`/worker or authorized operator (`NotificationRetryStarted`) | Delivery never mutates the originating workflow decision; retry replay returns the same successor and sent results never resend. | M1 |

FR74 has exactly four safety-control subject classes; outbound is governed through its command capability and is not a fifth class. Every retained cell uses the same versioned `ApplySafetyControl` command with a closed subject/mode discriminator and the same `ReleaseSafetyControl` successor command:

| Subject | Disable | Quarantine | Rate-limit | Initiator + independent approver | Release rule |
| --- | --- | --- | --- | --- | --- |
| Mailbox source | `mode=disabled`; deny new intake | `mode=quarantined`; deny intake and open investigation | `mode=rate-limited`; bounded messages/time window | `mailbox-admin` + `policy-admin` | Both roles approve after M365 authority and unsafe-activity evidence are current |
| Service client | `mode=disabled`; deny authentication/admission | `mode=quarantined`; revoke active use and open credential investigation | `mode=rate-limited`; bounded operations/time window | `policy-admin` + independent `tenant-admin` | Both roles approve after credential rotation/revocation evidence and scope validation |
| AI actor | `mode=disabled`; deny model/tool invocation | `mode=quarantined`; deny invocation and preserve proposals for investigation | `mode=rate-limited`; bounded requests/tokens/time window | `policy-admin` + independent `tenant-admin` | Both roles approve after provider/identity evidence and A5/A13 gates remain valid |
| Command capability (including outbound) | `mode=disabled`; deny admission | `mode=quarantined`; deny admission and open command investigation | `mode=rate-limited`; bounded executions/time window | `policy-admin` + Security approver | Both approve after qualification, allowlist, owner-contract, and incident evidence are current |

The `safety.controls` policy row in the appendices supplies the only allowed subject/mode/bound grammar. Missing, expired, malformed, or conflicting approval leaves the prior safer control active. Control commands never provide Project data access or permission to operate on individual queue items.

Context ownership is canonical in §Context Ownership. In summary: Hexalith.Projects owns Project identity/access; Hexalith.Conversations owns messages and conversation-to-Project assignment; Hexalith.EventStore owns its current durable command/event gateway. A13 blocks M0 until the producer owners accept the proposed atomic canonical-audit persistence boundary or Architecture assigns an equivalent approved owner. ChatBot owns orchestration, rebuildable investigation views, and the derived stores enumerated in §Data Governance Surface.
