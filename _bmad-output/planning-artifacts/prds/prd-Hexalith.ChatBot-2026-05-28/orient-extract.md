---
title: Orientation and Source Extraction - Hexalith.ChatBot PRD
status: complete
created: "2026-09-13"
intent: validation-source-orientation
sources:
  - "prd.md"
  - "addendum.md"
  - ".decision-log.md"
  - "../../product-brief-Hexalith.ChatBot.md"
---

# Orientation and Source Extraction — Hexalith.ChatBot PRD

This is a source extraction for PRD validation, not a review. It records what the source set says, where the product commitments came from, and which source conflicts or omissions a reviewer must keep in view. It does not modify or recommend changes to the PRD.

## Source set and provenance

### Direct PRD sources

- `prd.md` is a final PRD authored by Jerome, created 2026-05-10. Its frontmatter names exactly one input document: `D:/Hexalith.ChatBot/_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md`.
- The current-workspace equivalent of that input is `_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md`. The brief is marked complete and dated 2026-05-10.
- `addendum.md` is declared binding implementation context for confidence thresholds, risk classification, command allowlists, tenant policy, the shared command pipeline, idempotency, replay isolation, identifier evolution, inbound authenticity, and operating baselines. Its frontmatter says it was created/updated 2026-05-28 and approved 2026-06-09.
- No `.memlog.md` exists. The workspace instead has `.decision-log.md`, a legacy decision/audit trail covering 2026-05-28 through 2026-08-28. It records validation-driven decisions, explicit reversals, source-reconciliation decisions, command-allowlist deployment, and recovery-evidence status.

### Product-brief source chain

The product brief lists eight inputs. They appear to be repository/platform documentation rather than customer interviews or external research:

1. `README.md` — present at the repository root.
2. `Hexalith.EventStore/README.md` — present in the current workspace as `references/Hexalith.EventStore/README.md`.
3. `Hexalith.EventStore/docs/concepts/architecture-overview.md` — present as `references/Hexalith.EventStore/docs/concepts/architecture-overview.md`.
4. `Hexalith.Parties/_bmad-output/planning-artifacts/product-brief-Hexalith.Parties-2026-03-01.md` — the exact cited artifact is not present. The current submodule contains `references/Hexalith.Parties/_bmad-output/project-context.md` and `references/Hexalith.Parties/README.md`.
5. `Hexalith.Tenants/_bmad-output/planning-artifacts/product-brief-Hexalith.Tenants-2026-03-06.md` — the exact cited artifact is not present. The current submodule contains `references/Hexalith.Tenants/_bmad-output/project-context.md` and `references/Hexalith.Tenants/README.md`.
6. `Hexalith.Folders/_bmad-output/planning-artifacts/product-brief-Hexalith.Folders.md` — present as `references/Hexalith.Folders/_bmad-output/planning-artifacts/product-brief-Hexalith.Folders.md`.
7. `Hexalith.Memories/README.md` — present as `references/Hexalith.Memories/README.md`.
8. `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Cli/README.md` — present under `references/Hexalith.FrontComposer/`.

The PRD later names repository project-context artifacts for Conversations, Projects, Folders, Parties, Tenants, EventStore, FrontComposer, Memories, and Commons as source context “available ... on 2026-05-28.” Those documents are not listed as direct frontmatter inputs: `documentCounts.projectContext` is `0`, and no paths or revisions are pinned. Conversations, Projects, and Commons are also absent from the product brief's input list.

### Later change and evidence sources referenced from the source set

- `sprint-change-proposal-2026-06-09.md` is referenced by a PRD note as the approval source for adding the governed interactive chat surface and FrontComposer shell adoption.
- `docs/adrs/live-recovery-validation-drivers.md` and `_bmad-output/implementation-artifacts/deferred-work.md` are referenced by the decision log for recovery-validation and residual-risk evidence.
- The decision log cites hosted GitHub Actions run `33066358280`, commit `17aa94d48a79b5260be919e97060290403924fe2`, and evidence-run locator `01M11EYSDMP1ZF38B7KZA1A6FA` as the 2026-08-27 live-recovery bundle.
- Prior validation/reconciliation artifacts are retained beside the PRD, including `reconcile-product-brief.md`, rubric and adversarial reviews, and generated validation reports. These are review history rather than original product inputs.

## Product intent

The product is an enterprise, project-centered AI collaboration application built on the Hexalith platform. Its promise is to turn external project email from disconnected correspondence into durable, governed project context: messages become conversations, attachments become governed files, requests become human or AI action intent, risky actions pass through approval, and outcomes return to the workspace as auditable events and projections.

The user-voice anchor carried from the product brief is: “I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context.”

The differentiating thesis is not email summarization. It is that an email can become authorized project work while the system can prove:

- which tenant and project the work belongs to;
- which participant requested it;
- which source messages and files were used;
- which policy and authority applied;
- which human approval permitted it, when required;
- which governed command executed; and
- which result and downstream state were recorded.

The PRD explicitly treats this as an internal product thesis pending pilot validation, not a market claim backed by competitive research.

## Problem and intended value

The source problem is fragmented project collaboration across email, chat, file stores, task lists, and individual AI assistants. Context is scattered, attachments are duplicated, requests and decisions are implicit, and teams repeatedly reconstruct the latest project truth. AI can draft and summarize but is unreliable for enterprise execution when project, file, participant, authority, and audit boundaries are not durable.

The intended value is “controlled acceleration”:

- external participants continue using email rather than being forced into a new portal;
- internal contributors see email, attachments, decisions, task intent, approvals, failures, and AI outcomes in one project context;
- deterministic association and evidence-led review reduce manual searching and context re-entry;
- governed AI assists without silently crossing project, tenant, file, tool, or outbound-communication boundaries;
- developers and AI agents can automate through the same command model as human users; and
- operators and reviewers can reconstruct decisions without relying on screenshots or informal explanations.

## Stakeholders and actors

### Stakeholders carried from the product brief

- **Business teams:** coordinate with customers, partners, suppliers, and internal specialists; success means email discussions, documents, and action items become organized project work.
- **Project managers / delivery leads:** need visibility into requests, AI actions, file changes, and work outcomes; success means less coordination overhead and fewer manual follow-ups.
- **Developers and automation builders:** need UI/CLI/MCP access to a common governed operation model without one-off integrations.
- **Platform operators and administrators:** need tenant isolation, identity, access control, service composition, observability, and operational evidence.

### Roles made explicit by the PRD

- Human roles: TenantAdmin and finer-grained mailbox/policy/compliance/operations admins; ProjectAdmin/ProjectOwner; ProjectMember/Contributor; MailboxOwner; Auditor/Compliance Reviewer.
- External collaborator: a tenant-scoped party who participates through email and does not require portal authentication in MVP under A7.
- Machine roles: mailbox-ingestion, audit-projection, background-retry, CLI automation, MCP tool, and AI-action service clients; UI, CLI, and MCP clients; background workers; mailbox events; and governed AI actors.
- Named decision owners: Product Lead; System Architect; Test Architect; Security/Architecture; Compliance/Architecture; Architecture/DevOps; Developer Experience; and tenant-authorized human approvers.

The tenant admin is explicitly not a superuser: aggregated operational visibility does not automatically grant project-content visibility or bypass authorization/audit requirements.

## Success outcomes and evidence targets

### User and business outcomes

- Correct project association, with deterministic matches when sufficient and explicit review when ambiguous.
- Fewer manual project updates and less time between email receipt and usable governed project context.
- Ambiguity resolved from evidence already presented by the product, without searching other systems or re-entering context.
- Repeated pilot use rather than a one-time import/demo.
- Governance that feels like part of the email workflow rather than separate administrative work.
- Practical use of at least one CLI or MCP path by an automation/AI workflow without governance bypass.

### Quantified association and pilot targets

- Association calibration target: at least 95% precision and 90% recall for non-ambiguous messages on the A9a dataset.
- Critical unauthorized-project false positives: zero.
- Dataset size: at least 500 labeled messages by M0 and 2,000 by M1, with monthly pilot and quarterly post-pilot refresh and at least 20 new adversarial examples per calibration cycle.
- Pilot use: at least one tenant for four consecutive weeks, at least two monitored mailbox patterns, and at least five active projects represented.
- At least 70% of ambiguous decisions resolved from presented evidence without manual context re-entry.
- At least 40% reduction in median time from email receipt to governed project context, and at least 30% reduction in manual email-sourced project updates, measured against an A11 baseline.
- At least 10 governed AI action reviews in M0 and 30 across M0+M1.
- Approved allowlisted AI-action execution success: at least 95%.
- Attachment auto-handling into the correct governed folder without manual intervention: at least 90%.

The pilot, AI-action, and attachment numbers are starter assumptions under A11; the Product Lead must replace or confirm them after a two-to-four-week tenant baseline window. No completed A11 baseline result is present in the examined source set.

### Safety, audit, and operability outcomes

- Zero tolerance for cross-tenant access or exposure across candidates, evidence, files, commands, caches/indexes, logs, traces, and audit views.
- Every ambiguous message requires human confirmation before association.
- Unresolved or unauthorized actors cannot access files, create task requests, execute commands, or send outbound messages.
- Risky AI actions require approval; low-risk read-only assistance requires policy and authorization.
- State-mutating operations fail closed when audit readiness or another enumerated safety precondition is unavailable.
- Audit field presence is required for all security-sensitive events in the validation dataset.
- Production audit-chain reconstructability target is at least 99.5% per tenant over a rolling seven-day window; a miss is a P1 incident.
- User-facing lookups target p95 <= 2 seconds; association candidate generation p95 <= 10 seconds; long-running CLI/MCP operations return identity/status p95 <= 5 seconds.
- WCAG 2.2 AA applies incrementally to every enumerated UI surface, with automated, keyboard-only, and screen-reader validation.
- Recovery targets remain provisional: source records RPO <= 15 minutes and RTO <= 4 hours; projections rebuild <= 4 hours.

## Product commitments and constraints

### Product and architecture boundary

- ChatBot is an orchestration layer, not the source of truth for projects, parties, folders/files, tenants, identity, or event history.
- Hexalith.Projects owns project identity, membership, and project conversation boundaries; Parties owns participant identity; Folders owns governed file storage; Tenants owns tenant facts/policy/authorization context; EventStore supplies command/event flow and projections; mail integration owns provider message capture/delivery state.
- ChatBot does own security-sensitive derived state: associations, candidate rankings, evidence snapshots, AI proposals, approval records, policy snapshots, lifecycle state, projections, workflow predecessor maps, AI-context/vector stores, replay traces, and operational queue projections.
- All mutable operations use the shared command spine: authentication, tenant binding, authorization, risk classification, approval gate, coarse idempotency, pre-commit audit gate, EventStore execution/fine idempotency, event publication, projection update, and post-commit audit.
- UI, CLI, MCP, service-client, AI, and worker adapters may translate input but may not reimplement or bypass pipeline stages.

### Release and scope boundary

- The PRD calls the MVP a single release window delivered through three fixed, dependency-ordered pilot increments.
- **M0 — Vertical Thesis Path:** one controlled M365/Exchange mailbox pattern for one tenant; deterministic association; UI ambiguity review; governed attachments; one AI task/action path; one allowlisted state-mutating command; identity, isolation, failure handling, and audit.
- **M1 — Cross-Surface Parity & Full Governance:** CLI/MCP adapters, service clients, outbound draft/send, broader lifecycle and approval policies, tenant-admin scopes, versioned allowlist, inbound authenticity, and broader actor isolation.
- **M2 — Operations, Recovery, Continuity:** dashboards, replay isolation, per-operation idempotency, WORM/hash-chain audit, audit completeness, derived-store isolation, recovery validation, and published SLOs.
- The dependency order M0 -> M1 -> M2 is fixed. A constrained team may trim dashboards, advanced inference, policy breadth, document intelligence, and polish, but may not trim tenant isolation, authorization, fail-closed behavior, idempotency, audit completeness, or safe AI approval.
- The named delivery shape includes product ownership, a system architect, multiple backend/service engineers, one frontend engineer, one CLI/MCP engineer, one security/identity engineer, a QA/test architect, and DevOps support.
- MVP completion requires M2 even though each increment may be released to the pilot cohort.

### Mail, identity, AI, and data constraints

- Microsoft 365 / Exchange is the first controlled mailbox assumption. Generic mail is allowed only if it meets the same stable-identity, tenant-authority, attachment, authenticity-evidence, idempotency, audit, and fail-closed contract.
- Deterministic evidence has priority over AI inference. Conflicts and insufficient context route to review/failure states rather than silent association.
- External participants use email and are represented as tenant-scoped Parties; they are not authenticated portal users in the MVP unless A7 changes.
- Provider-supplied DMARC/DKIM/SPF is recorded rather than independently reverified by ChatBot. Header disagreement and delegated/on-behalf-of authority are retained as evidence.
- Risk classes are low-risk read-only, approval-required, denied, and unsupported; mixed requests inherit the strictest applicable class.
- M0's AI allowlist is exactly `Project.AppendConversationMessage`, approval-required by default. The 2026-06-03 decision-log entry records deployed v1 as that command plus `ChatBot.ExecuteLowRiskAssistance`; external-effect and governance commands remain human-only/disallowed for AI.
- Message, decision, command, outbound, proposal, correction, and retry operation classes have explicit idempotency-key composition, equivalence, replay-window, and conflict rules.
- GDPR/EU expectations apply to messages, participants, attachments, prompts/outputs, approvals, audit, derived state, exports, deletion, retention, residency, and redaction.

### Explicit non-goals / deferred scope

- General email-client replacement, portfolio/budget/resource project management, full task lifecycle, deep document co-authoring/intelligence, broad knowledge management, unrestricted AI/commands, broad workflow builder, arbitrary third-party integrations, subscription/billing/packaging enforcement, and cross-tenant discovery.
- Teams, WhatsApp, and other messenger channels are post-MVP.
- Scheduled-time, file-addition, and standing/explicit user-instruction automation are post-MVP; the MVP proves conversation/email-triggered work first.
- Generic email beyond the controlled mailbox contract is not a separate MVP channel.

## Binding implementation detail carried by the addendum

- Association thresholds default to `T_high = 0.90` and `T_low = 0.60`, with security-sensitive change controls and calibration against A9a.
- The action-risk classifier is tag-and-heuristic in M0/M1; an optional M1 LLM explanation cannot change the decision. Indeterminate classification routes to approval-required.
- Tenant policy is a closed, versioned schema with safe defaults and security-sensitive change controls; tenants can set declared values but cannot add knobs.
- Idempotency rules differ by operation class, from indefinite provider-message identity to short command/proposal replay windows.
- Replay uses a separate test tenant, intercepts outbound effects, tags events with `replay_run_id`, and excludes replay events from production audit-completeness measurement.
- Historical identifiers are preserved in immutable audit and resolved through appended identity-migration records rather than rewritten.
- Audit is append-only/WORM with hash-chained per-tenant envelopes; redaction appends records and uses projection tombstoning/key shredding rather than mutating history.
- The M2 operating-baseline catalog contains several targets still marked `calibration-pending`; the addendum says only audit projection lag presently has a live error-budget signal.

## Decision chronology that affects interpretation

- **2026-05-28:** the inherited PRD was normalized and marked final. Major decisions were backfilled: email-first MVP, first controlled M365 assumption, deterministic/fail-closed association, boundary authorization, governed AI, audit-first execution, UI/CLI/MCP parity, and one end-to-end release slice.
- **2026-05-28, validation remediation:** a prior big-bang MVP was split into M0/M1/M2. CLI/MCP moved from M0 to M1; WCAG scope was tied to built surfaces; the addendum was created; fail-closed, shared-pipeline, data-governance, admin-scope, idempotency, replay, authenticity, and audit-completeness contracts were added.
- **2026-05-28, source reconciliation:** the brief's multi-channel/generic-mail/automation scope reductions were recorded as deliberate. User voice and narrower replacement success metrics were restored. The finalization entry states that remaining A9a/A10/A11 markers and strategic PM notes were deferred with owners and revisit conditions rather than treated as blockers.
- **2026-06-03:** AI allowlist v1 was deployed with two AI-invocable commands, defaulting unpinned tenants to the M0 floor.
- **2026-06-09:** the PRD body cites an approved sprint-change proposal that brings an interactive chat composer and FrontComposer shell adoption into Epic 10 as a governed write surface.
- **2026-08-01 to 2026-08-28:** recovery evidence advanced from local-only to a genuine hosted bundle, but A10 stayed provisional. A later controlled-loss mechanism was implemented and locally verified, with no hosted artifact cited.

## Source conflicts, overrides, and omissions for reviewer orientation

These are provenance facts and cross-source differences, not validation verdicts.

1. **No canonical memlog.** `.memlog.md` is absent. The available `.decision-log.md` contains the history, including post-final changes and explicit reversals, but is not the canonical file expected by the current workflow.

2. **Freshness metadata lags content.** The PRD frontmatter says `updated`/`lastEdited: 2026-05-28`, while its body includes the approved 2026-06-09 chat-surface change and 2026-08-28 recovery wording. The addendum says `updated: 2026-05-28` although its operating-baseline section includes evidence and decisions through 2026-08-28. The decision log is the only examined source whose chronology reaches those changes explicitly.

3. **The direct source path is machine-specific.** The PRD records a `D:/...` product-brief path even though the current source is under the repository's `_bmad-output/` tree.

4. **The transitive source chain is partially unresolvable.** Exact Parties and Tenants product briefs named by the product brief are absent from the current checked-out submodules. Current project-context files exist, but they are different artifacts. The PRD's broader “current module planning context” claim is not represented in its input list or counts and does not pin exact paths/revisions.

5. **No external discovery evidence is named.** PRD frontmatter reports zero research, brainstorming, project-doc, and project-context inputs. The product brief is based on internal repository/platform documents. The PRD itself disclaims external competitive claims; no customer transcript, market research, pilot baseline, or completed A9a/A11 dataset report appears in this source set.

6. **Product-brief scope was deliberately narrowed.** The brief placed Microsoft 365 and generic mail side by side, included schedule/file-addition triggers, user-upload file ingestion, and treated chatbot UI/CLI/MCP as concurrent MVP surfaces. The PRD makes M365 the first controlled mailbox, defers schedule/file/user-instruction triggers, explicitly covers mailbox-attachment ingestion but does not explicitly carry a general user-upload requirement, and sequences CLI/MCP to M1. The decision log records most of these as intentional scope reductions.

7. **The later chat-surface decision is only partially integrated into the examined PRD.** The 2026-06-09 scope change appears in a note that assigns interactive chat to Epic 10. It is not reflected in frontmatter inputs/edit history, the `.decision-log.md` chronology, or a separately enumerated UI surface/FR in the examined PRD; the existing project conversation surface may be intended to contain it, but that mapping is not stated in these sources.

8. **Single-release language coexists with independently releasable increments.** Frontmatter says `releaseMode: single-release`; the body says M0, M1, and M2 are each releasable to the pilot cohort while MVP completion requires M2. The body resolves this as one “release window,” but reviewers should use that defined meaning rather than assume one deployment event.

9. **`Skipped` increment ownership differs across sources.** The 2026-05-28 tightening entry says only `Skipped` was M1 after restoring `Rejected` to M0. The current PRD says `Skipped` is part of the M0 command-spine contract and lists it in M0. No later decision-log entry explicitly records this reversal.

10. **Association scoring and action-risk classification are conflated in the addendum.** Confidence Thresholds says the deterministic-signals scorer is described in Risk Classifier and that the “same scoring kernel” produces association confidence and risk classification. Risk Classifier instead describes a tag/heuristic decision over command, effect surface, policy, and requester authority with categorical output. FR35 also refers to a numeric confidence score in the same domain as Risk Classifier. The source set does not state a single unambiguous contract connecting these two mechanisms.

11. **Below-threshold association behavior is expressed differently.** The PRD says messages below `T_low` are deferred or rejected. The addendum says scores below `T_low` fail closed to `NeedsReview` with an empty/full candidate-list rule depending on whether scoring failed. These lead to different initial lifecycle states unless architecture supplies an explicit mapping.

12. **A9a label use is not fully aligned.** A9a enumerates labels including `risky-ai-candidate` but not `actionable`; FR35 says task-intent precision/recall is calibrated against `risky-ai-candidate` and `actionable` labels. The source set does not define whether `actionable` is a separate label, a derived state, or a renamed taxonomy member.

13. **The deployed M1 allowlist is a later narrowing/clarification.** The binding addendum describes v1 as the full command catalog minus commands tagged `disallowed-for-AI`; the later decision log records an actual two-command v1 and declares external-effect/governance commands human-only. For deployed behavior, the later decision entry is the more specific source, while the addendum's broad description remains unchanged.

14. **Recovery evidence is intentionally provisional and is no longer fresh for this validation date.** The 2026-08-27 hosted artifact was citable only through 2026-09-04 under the eight-day freshness rule. On 2026-09-13, it remains historical evidence but cannot carry a current ratification claim. It is also tied to commit `17aa94d`, whereas the checked-out repository HEAD observed for this extraction is `f0ba70e`. The subsequent DW-52 controlled-loss mechanism has local verification only and no cited hosted artifact. A10, NFR57, and NFR41 therefore remain provisional/not established exactly as the later source text states.

15. **Recovery evidence limitations are explicit.** The hosted run demonstrated two no-loss RPO values of 0 seconds and short recovery times inside a 180-second measurement ceiling. It cannot prove positive controlled-loss RPO or falsify a four-hour RTO. External M365, durable WORM, production-control, provider-scale, evidence-kind, and current-hosted-run residuals remain open in the cited source trail.

16. **Operating targets are not all calibrated.** The addendum's M2 SLO table contains multiple `calibration-pending` targets/error budgets, and A11 remains an assumption. The code catalog is named as the single source of truth for SLO metric names, but no catalog extract or completed baseline run is part of this orientation source set.

## Reviewer evidence map

| Review concern | Primary evidence in source set |
| --- | --- |
| Product value and differentiation | PRD Executive Summary, What Makes This Special; product brief Problem, Solution, What Makes This Different |
| User and stakeholder fit | Product brief Who This Serves; PRD UJ1–UJ8 and System Journey; RBAC Matrix |
| Scope and deliverability | PRD Product Scope and Project Scoping; decision-log 2026-05-28 validation remediation |
| Source fidelity | Product brief; PRD Measurable Outcomes/Growth Features; decision-log post-review reconciliation |
| Association safety | PRD FR1–FR12, A1/A2/A9/A9a, NFR13–NFR22; addendum Confidence Thresholds |
| AI safety and approval | PRD FR35–FR50, NFR8/NFR9/NFR16/NFR46; addendum Risk Classifier and Command Allowlists |
| Tenant/authorization boundary | PRD Tenant Model, Permission Model, RBAC Matrix, FR13–FR20, FR51–FR63, NFR1–NFR12 |
| Cross-surface parity | PRD FR81–FR86 and parity outcomes; addendum Shared Command Pipeline |
| Data governance and audit | PRD Data Governance Surface, FR55–FR63, NFR49–NFR55; addendum replay/ID-evolution/audit details |
| Reliability and recovery | PRD FR64–FR80, A10, NFR13–NFR22, NFR56–NFR59; addendum Operating Baselines; decision-log 2026-08 entries |
| UX/accessibility handoff | PRD journeys, UI Surface Inventory, NFR60–NFR64; 2026-06-09 chat-surface note |
| Assumptions needing current evidence | PRD A1–A11; especially A9a, A10, A11; addendum `calibration-pending` baselines |

## Orientation summary

The PRD is a high-stakes, implementation-facing B2B SaaS definition whose dominant product promise is governed email-to-project execution. Its trust floor—tenant isolation, deterministic/evidence-led association, command-boundary authorization, fail-closed mutation, human approval for risky AI, idempotency, and reconstructable audit—is explicitly non-negotiable. The product brief supplies the user promise and original breadth; the PRD and addendum narrow that breadth into a three-increment delivery and add detailed contracts. The legacy decision log is essential because it records the intentional scope departures and the later command/recovery evidence. Reviewers should interpret the current artifact with the provenance, late-change, scoring-contract, lifecycle-state, and evidence-freshness differences above in view.
