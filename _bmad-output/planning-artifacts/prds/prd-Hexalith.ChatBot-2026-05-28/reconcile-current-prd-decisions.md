---
title: Reconciliation - Current Hexalith.ChatBot PRD Decisions
created: "2026-09-14"
sources:
  - prd.md
  - addendum.md
  - .memlog.md
---

# Current PRD Decision Reconciliation

## Verdict

The product baseline is strong and unusually explicit about safety, ownership, and staged delivery. It is not yet safe to update by simple accretion: the current artifacts contain several material conflicts about which AI actions may bypass approval, what M0 and M1 must prove, and where the approved governed chat surface belongs. Those conflicts should be resolved before new scope is merged into the PRD.

This reconciliation treats `.memlog.md` as the current durable decision baseline, `prd.md` as the capability and quality contract, and `addendum.md` as binding downstream mechanism detail. Where they disagree, the conflict is surfaced rather than silently choosing one source.

## Current Product Decision Baseline

1. **Product and user promise.** Hexalith.ChatBot is an email-first governed AI project workspace for B2B project teams. Email is the initial wedge because external collaborators already use it and should not need an authenticated Hexalith portal for the MVP.

2. **MVP thesis.** The product must prove one end-to-end email-to-governed-action loop: capture authorized project email, resolve project context, govern attachments and task intent, mediate AI work, execute approved work through service commands, and preserve a reconstructable outcome.

3. **Governed AI actor.** AI is not a privileged assistant or side channel. It receives explicit tenant, project, requester, evidence, file, policy, approval, tool, command, and destination scope. Unsafe, unauthorized, or unresolved requests fail closed.

4. **Association before action.** Deterministic association evidence takes precedence over learned or AI inference. Automatic association is allowed only above the configured threshold with required deterministic evidence and no conflicting signal. Ambiguity, unresolved identity, and authorization uncertainty route to visible human review.

5. **Authorization boundary.** Tenant, actor, role, project, and resource authorization is enforced at command and query boundaries for UI, CLI, MCP, service clients, workers, mailbox events, and AI actors. Unauthorized resource existence, project names, candidate evidence, files, and audit details remain redacted.

6. **Bounded-context ownership.** ChatBot owns orchestration and the security-sensitive derived records enumerated in the Data Governance Surface. Projects, Parties, Folders, Tenants, EventStore, Conversations, and mail integration retain their source-of-truth responsibilities. Durable mutations go through versioned commands and events rather than duplicated authority.

7. **Release shape.** The MVP is one release window delivered in fixed dependency order: M0, then M1, then M2. Scope breadth may shrink under pressure, but tenant isolation, authorization, fail-closed behavior, idempotency, audit completeness, and safe AI approval are the non-negotiable floor.

8. **M0 decision.** M0 proves the UI-only vertical for one configured tenant and one controlled Microsoft 365 or Exchange mailbox pattern. It includes deterministic association, evidence-based review, governed attachments, one allowlisted AI action, identity, failure handling, audit, and the M0 accessibility surfaces.

9. **M1 decision.** M1 adds governed CLI and MCP access, service-client permissions, outbound draft-and-send, full approval/admin policy, inbound authenticity handling, and a versioned command allowlist. Every state mutation enters one shared command spine, making parity structural rather than adapter-specific.

10. **M2 decision.** M2 closes MVP production readiness with operational dashboards, recovery evidence, replay isolation, per-operation idempotency, a tamper-evident audit chain, measurable audit completeness, and store-level isolation of AI-derived tenant data.

11. **Governed interactive chat.** The approved 2026-06-09 change makes interactive chat an MVP governed write surface delivered through Epic 10 and the FrontComposer shell. Messages enter through the command gateway; risky requests become governed proposals, and no ungoverned freeform write path is permitted.

12. **Workflow history and correction.** `Rejected` and `Failed` association workflows are terminal. Reprocessing creates a new audit-linked workflow instance. Corrections preserve the predecessor decision, invalidate every affected derived store, and prevent AI use until propagation completes.

13. **Reliability and audit.** State mutation requires audit readiness and idempotency. Audit unavailability returns a typed failure and writes no durable state. Duplicate and retried inputs converge on the same observable result without duplicate project artifacts.

14. **Data governance.** ChatBot-owned derived records are tenant-scoped, provenance-bearing, retention-governed, and redaction-aware. Audit evidence must remain reconstructable without becoming an unrestricted secondary content store.

15. **Deliberate exclusions.** The MVP excludes autonomous project creation, general email-client replacement, full task lifecycle management, broad document intelligence, unrestricted commands, cross-tenant suggestions, extra messaging channels, broad non-email triggers, and commercial packaging.

16. **Accessibility.** WCAG 2.2 AA is a release gate for UI surfaces introduced by each increment. CLI and MCP are outside WCAG scope, but their security, status, and error semantics remain part of parity.

## Open Assumptions, Notes, and Questions

### Explicit assumptions

| IDs | Current assumption | Owner / required evidence | Update significance |
| --- | --- | --- | --- |
| A1-A2 | The first pilot can use Microsoft 365 or Exchange and begin with one controlled mailbox pattern before broader mailbox cases. | Product and Architecture; confirm pilot permission and mailbox shape. | Do not broaden provider or mailbox scope implicitly. Resolve the direct conflict with A11's two-pattern target. |
| A3 | CLI or MCP belongs in MVP, but an initial parity proof may cover only association, status, and audit. | Product and Developer Experience; pilot machine-surface usage. | The M1 exit contract and the full MVP parity set are currently ambiguous. |
| A4 | Default operating baselines are acceptable until tenant-specific baselines exist. | Architecture and Operations; onboarding, capacity, and quarterly reviews. | Preserve defaults as assumptions, not contractual tenant promises. |
| A5 | An AI provider can satisfy tenant policy for telemetry, training reuse, retention, and region behavior. | Security and Architecture before enabling live AI. | Provider selection or enablement remains gated. |
| A6 | Audit reconstructability can coexist with GDPR/EU retention, export, correction, and erasure duties. | Compliance and Architecture before pilot onboarding. | The WORM, redaction-record, and key-shredding design remains provisional against legal validation. |
| A7 | External participants do not need portal authentication in MVP. | Product; revisit if external review or approval is required. | Preserve the email-continuity promise unless the product decision changes explicitly. |
| A8 | A fixed, versioned allowlist is sufficient for MVP AI execution. | Product and Architecture; unsupported-but-valid request frequency. | The exact safe M1 allowlist needs reconciliation with the current broad v1 definition. |
| A9-A9a | A representative evaluation dataset can be built from consented, redacted, or synthetic examples, reaching 500 labeled messages for M0 and 2,000 for M1. | Test Architect; production sampling and monthly/quarterly refresh. | Thresholds, intent detection, and classifier calibration remain evidence-dependent. |
| A10 | RPO at or below 15 minutes and RTO at or below 4 hours are provisional. | Architecture and DevOps; fresh hosted controlled-loss proof plus a full-window or pre-production recovery drill. | The cited hosted evidence expired on 2026-09-04 and predates the required controlled-loss job. No current source ratifies either target. |
| A11 | Pilot adoption, time-reduction, automation, approval-volume, and attachment metrics are starter values pending a 2-4 week tenant baseline. | Product Lead after baseline and at every increment release. | Many success targets and SLO error budgets remain calibration-pending. |

All inline `[ASSUMPTION]` markers in the current PRD map to A9a or A11. There is no separate Open Questions section and no unowned inline assumption marker, but the questions below remain unresolved by the artifacts.

### PM notes and unresolved questions

1. **CLI/MCP timing:** M0 is deliberately UI-only, but M1 must ship before pilot exit to avoid incompatible terminology, error, or approval expectations. What exact operation set is the M1 release gate?

2. **Interactive chat placement:** the 2026-06-09 decision is approved, but the increment, UI inventory entry, FR group, and accessibility gate for the composer are not explicit. Is the governed composer part of M0, M1, or a later MVP increment?

3. **Approval fatigue:** the PRD schedules a policy revisit at the M1-to-M2 boundary when rubber-stamp approvals exceed 15 percent over seven days. Which action classes, if any, may ever be reclassified without violating the immutable risky-action approval promise?

4. **Resource model:** the dependency order survives team growth or shrinkage. Schedule changes must be recorded rather than absorbed by dropping trust controls.

5. **Recovery evidence:** A10 is still provisional. Is there a fresh hosted run after DW-52, and is there a separate drill able to measure the four-hour RTO rather than only a 180-second ceiling?

6. **Multi-tenant rollout:** M0 explicitly uses one configured tenant and excludes multi-tenant rollout; M2 adds store-level derived-data isolation. Which increment owns the first multi-tenant pilot and its release gate?

7. **SLO completion:** the M2 catalog still contains `calibration-pending` targets and error budgets, and only audit projection lag has a live burn signal. Which values and live signals must exist before M2 can close?

## Internal Inconsistencies

### 1. Critical - risky actions can both require approval and be downgraded to low-risk

- The executive summary, FR41, the glossary, the increment safety floor, and `.memlog.md` say actions that mutate state, expose files, send externally, create tasks, invoke tools, or act on behalf of a participant require human approval.
- `addendum.md` defines `ai-action.low-risk-allowed` as a per-class map over those same six risky categories, and the FR41 PM note proposes setting classes to `true` when reviewers usually approve them.
- This permits the policy schema to erase a control declared non-negotiable elsewhere.

**Required reconciliation:** reserve `low-risk` for genuinely read-only, boundary-preserving operations. Keep the six boundary-crossing classes approval-required, or record an explicit product-level reversal of the current safety decision with new acceptance and audit rules.

### 2. High - three different scoring and disposition models are conflated

- The confidence-threshold addendum says one numeric deterministic-signals kernel produces both association confidence and risk classification.
- The risk-classifier addendum is categorical and uses command tags, effect surface, tenant policy, and authority; it outputs only `low-risk` or `approval-required`, with disallowed commands rejected before classification.
- The PRD risk table presents four classes: `low-risk`, `approval-required`, `denied`, and `unsupported`.
- FR35 says task-intent confidence uses the same domain as the risk classifier even though that classifier defines no numeric confidence domain.

**Required reconciliation:** define three separate contracts: association confidence scoring, task-intent detection confidence, and AI-action classification. Treat `denied` and `unsupported` as dispositions unless the classifier is deliberately expanded.

### 3. High - M0 supports one mailbox pattern while the M0 pilot target requires two

- M0, the must-have list, A2, and `.memlog.md` specify one controlled or monitored mailbox pattern.
- A11 says the pilot baseline before M0 release uses at least two monitored mailbox patterns.

**Required reconciliation:** either make two patterns an M0 capability or move the two-pattern adoption target to M1 or a post-M0 measurement window.

### 4. High - the M1 parity exit contract is not singular

- The measurable-outcomes parity set includes ingest/status, candidate review, association decisions, attachment storage/status, task capture, approval, retry, audit, and status lookup.
- The M1 scope lists inspect, associate, reject, defer, correct, retry, approve, execute, status, and audit lookup, omitting or collapsing ingestion, attachment storage, and task capture.
- A3 permits the first parity proof to cover only association, status, and audit, while other passages describe M1 as full operation parity.

**Required reconciliation:** publish one enumerated M1 exit set and distinguish it from any earlier proof-of-concept subset. Remove the duplicate `status` entry in the measurable-outcomes list.

### 5. High - the approved chat surface is not integrated into increment scope

- The approved June change makes governed interactive chat an MVP write surface through Epic 10 and FrontComposer.
- M0 scope and UI Surface Inventory still enumerate only the project conversation rendering view, association review, and AI approval; no composer surface or composer acceptance contract is named.
- NFR60 therefore has no explicit accessibility gate for the newly approved interactive write surface.

**Required reconciliation:** assign the composer to an increment, add it to the UI inventory and journey/FR trace, define its governed admission behavior, and place it under that increment's WCAG gate.

### 6. High - Command Allowlist v1 is broader than the least-privilege decision

- A8 and the PRD require a fixed allowlist and prohibit unrestricted command execution.
- The v1 addendum includes the full Command and Query Contracts command catalog unless a tenant-policy tag marks an item `disallowed-for-AI`.
- That catalog includes governance-sensitive operations such as service-client grant and revoke, while FR75 explicitly forbids AI actors and service clients from performing admin assignment and policy-sensitive administration.

**Required reconciliation:** define v1 as a positive, product-controlled list of AI-callable downstream commands with explicit effects and authority. Administrative workflow commands should be excluded by construction, not merely by an optional tenant tag.

### 7. High - multi-tenant rollout has no owning increment

- The product is defined as strict multi-tenant B2B SaaS.
- M0 is one configured tenant and explicitly excludes multi-tenant rollout.
- M1 expands actor isolation and tenant governance, while M2 introduces store-level derived-data isolation, but neither explicitly declares the first multi-tenant release gate.

**Required reconciliation:** name the owning increment and its minimum isolation evidence. Do not infer that M1 rollout may precede the M2 store-layer isolation decision without an explicit risk acceptance.

### 8. Medium - audit ownership and workflow memory have competing authorities

- The non-canonical Shared Workflow block says audit/compliance owns immutable event records; the canonical Context Ownership block assigns EventStore supporting responsibility but does not name the owner of the WORM audit chain; the addendum refers to ChatBot's own audit store.
- `prd.md` and `addendum.md` direct durable decisions and deployment records to `.decision-log.md`, while the active BMad workflow defines `.memlog.md` as canonical memory. The recovered memlog does not contain the later operational decisions recorded only in `.decision-log.md`.

**Required reconciliation:** name the audit-chain bounded-context owner and define the role of EventStore. Separately, migrate or summarize still-binding `.decision-log.md` decisions into `.memlog.md`, then update artifact references so future updates have one canonical workflow memory.

### 9. Medium - classifier observability cites the wrong requirement

- The risk-classifier addendum says classifier misclassification rate is tracked per NFR50a.
- NFR50a defines audit-chain reconstructability at or above 99.5 percent, not classifier accuracy, and the published SLO catalog contains no classifier-error metric.

**Required reconciliation:** add or cite the correct classifier-quality requirement and observable without weakening NFR50a.

### 10. Medium - an evaluation corpus is described as a runtime fail-closed dependency

- NFR15a blocks AI action proposal when the evaluation dataset is unavailable for the classifier kernel.
- The addendum describes the runtime classifier as tag-and-heuristic and the evaluation dataset as calibration evidence.

**Required reconciliation:** state whether a versioned classifier artifact, rather than the source evaluation corpus, is the runtime dependency. Retain fail-closed behavior for an invalid or missing deployed classifier version.

### 11. Medium - document metadata and provenance lag the actual content

- `prd.md` frontmatter remains `updated: 2026-05-28`, `lastEdited: 2026-05-28`, and `status: final`, despite approved June scope changes and August recovery evidence.
- `addendum.md` remains `updated: 2026-05-28` despite content through 2026-08-28.
- PRD source context is dated 2026-05-28, and `inputDocuments` lists only a Windows-path product brief even though the artifact now relies on later sprint, architecture, code-catalog, and hosted-recovery inputs.

**Required reconciliation:** refresh dates, status, edit history, and input provenance during the update. Apply the material-change re-check protocol to sibling bounded contexts before finalizing.

## Constraints Any Recommended Update Must Preserve

1. **Do not weaken the trust floor by implication.** Tenant isolation, authorization, redaction, fail-closed mutation, audit readiness, idempotency, and approval for genuinely risky boundary-crossing actions remain release gates unless the user explicitly reverses a product decision.

2. **Keep deterministic association authoritative.** Learned or AI signals may rank and explain but must not override tenant filters, deterministic conflicts, confidence thresholds, or human review requirements.

3. **Keep one command spine.** UI, CLI, MCP, service clients, workers, mailbox events, and AI adapters translate inputs only; they do not duplicate or bypass authentication, tenant binding, authorization, risk, approval, idempotency, or audit stages.

4. **Preserve bounded-context authority.** ChatBot may own derived workflow records but must not become the source of truth for projects, parties, files, tenants, conversations, identity, or source mail. New capability must identify its owner and contract.

5. **Preserve historical reconstructability.** Rejections, failures, corrections, policy snapshots, superseded decisions, retries, and reprocessing links remain immutable and queryable under retention and redaction rules.

6. **Preserve correction safety.** An association correction must invalidate every derived consumer and block AI use of contaminated context until propagation completes or enters an explicit delayed incident state.

7. **Preserve increment dependency order.** M0 proves the vertical thesis, M1 extends governed surfaces and policy, and M2 closes production operations. A scope update may change contents or dates, but not silently reorder safety dependencies.

8. **Preserve the email-first external journey.** External participants continue through controlled email without mandatory portal adoption unless the update deliberately changes the MVP wedge.

9. **Preserve least privilege and positive allowlisting.** Mailbox permissions, service-client grants, sender authority, tools, files, recipients, and commands must be explicit, scoped, expiring where applicable, and auditable.

10. **Preserve tenant-scoped data governance.** Every new derived record must define tenant key, provenance, derivation version, retention class, redaction sensitivity, isolation surface, and owner increment.

11. **Preserve replay and test isolation.** Evaluation, replay, and simulation cannot mutate production projects, send external communication, or contaminate production audit-completeness measurements.

12. **Preserve accessibility at the point a surface ships.** Any newly introduced UI surface must join the UI inventory and the WCAG 2.2 AA gate for its owning increment.

13. **Preserve stable requirement identity.** Update existing FR/NFR text under stable identifiers where intent is unchanged; add new globally unique IDs only for genuinely new requirements, and repair all trace and addendum references.

14. **Keep capability in the PRD and mechanism in the addendum.** The PRD should state actors, outcomes, policy boundaries, states, and acceptance. Detailed algorithms, schemas, command lists, key composition, storage mechanisms, and operational evidence belong in the addendum.

15. **Keep assumptions explicit and owned.** Do not convert A1-A11 starter values into facts without cited evidence. Any deferred non-blocker needs an owner and revisit condition; any phase blocker must be resolved before UX, architecture, or story handoff.

16. **Refresh evidence before claiming readiness.** A10 recovery evidence must satisfy its own freshness and shipped-gate rules; A11 metrics and SLO budgets require the pilot baseline; provider, GDPR, and evaluation-dataset assumptions remain gates until their owners close them.
