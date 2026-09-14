---
title: Reconciliation of Latest PRD Reviews - Hexalith.ChatBot
status: complete
created: "2026-09-14"
intent: update-source-extraction
sources:
  - "orient-extract.md"
  - "review-adversarial-general.md"
  - "review-rubric.md"
  - "validation-report.md"
comparedAgainst:
  - "prd.md"
  - "addendum.md"
  - ".memlog.md"
---

# Reconciliation of Latest PRD Reviews — Hexalith.ChatBot

This extraction identifies which recommendations from the 2026-09-13 review set remain actionable against the current PRD, addendum, and recovered canonical memlog. It does not change those source artifacts. The markdown validation report was sufficient; `validation-report.html` was not needed.

## Verdict

**STOP remains warranted.** All four unique Critical blockers remain present in `prd.md` / `addendum.md`. Thirteen unique High contract gaps also remain. The creation of `.memlog.md` resolves the review's narrow “canonical memlog absent” observation, but it does not resolve stale artifact metadata, source lineage, or the substantive contradictions. The safest update is a contract-reconciliation pass followed by targeted re-validation before the PRD returns to `status: final` or is handed to architecture/story generation as authoritative.

Current unresolved inventory after deduplication:

- **Critical:** 4 unresolved.
- **High:** 13 unresolved.
- **Medium:** 7 unresolved concepts; the memlog portion of the metadata/lineage finding is resolved, while the rest of that finding remains open.
- **Low:** 1 unresolved editorial/canonicality issue.

## Decision-trail comparison

The recovered `.memlog.md` contains 16 durable product decisions. It is a useful product-intent baseline, but it is intentionally thin and does not contain the exact two-command v1 allowlist, the recovery-evidence chronology, the `Skipped` increment reversal, or a complete source manifest.

The review recommendations mostly enforce those recovered decisions rather than reverse them:

- Critical C1 enforces the recovered rule that risky work requires human approval.
- Critical C3 enforces the recovered rule that audit unavailability causes no durable mutation.
- The chat-surface recommendation implements the recovered decision that interactive chat is an MVP governed write surface.
- Replay and idempotency recommendations strengthen the recovered safety and audit rules.
- Conversation ownership refines the recovered bounded-context decision without changing the orchestration/source-of-truth split.

One real conflict must be recorded as an append-only override rather than hidden:

- `.memlog.md` assigns “storage-level isolation for derived AI data” to M2, while also declaring tenant isolation non-negotiable in every increment. Review C4 correctly observes that M0 already creates tenant-derived stores and M1 adds programmable surfaces. The recommended resolution is to require partitioning and native-store negative tests when each store first appears, while retaining M2 only for the later vector/embedding/prompt-cache stores and recurring monitoring. This revises the broad M2 timing in the recovered decision to preserve the stronger non-negotiable isolation decision.

Two clarifications should also be logged before finalization:

- Preserve `Skipped` in M0 because M0 duplicate suppression and out-of-scope mailbox handling need that terminal state; explicitly record that this supersedes the legacy M1-only note.
- Pin v1 to the exact two-command AI-invocable set already named by the orientation source, rather than assuming the generic recovered “allowlisted commands” decision establishes exact membership.

## Critical findings still unresolved

### C1 — Mandatory AI approval can still be waived by tenant policy

- **Severity:** Critical.
- **Current evidence:** `prd.md` §Task Intent and AI Action Mediation, risk table, FR41, FR52, the `[NOTE FOR PM]` after FR41, NFR16, and NFR46; `addendum.md` §Tenant Policy Schema, knob `ai-action.low-risk-allowed`.
- **Why unresolved:** FR41 and the recovered memlog say that state mutation, file exposure, external send, task creation, tool invocation, and acting on behalf require approval. The PM note and schema still allow those same six classes to be turned into `low-risk-allowed` actions.
- **Decision conflict:** The current PRD/addendum conflict with recovered decision 6. The recommendation does not conflict; it restores the recovered safety rule.
- **Recommended edit:** Define low-risk AI assistance as an exact, versioned set of read-only, no-external-effect subtypes. Make the six boundary-crossing effect classes structurally non-downgradable. Replace the six-class boolean map with an allowlist of eligible read-only subtypes, reject policy snapshots that attempt to downgrade a mandatory-approval effect, and remove the FR41 PM-note ratcheting language for those six effects. Keep approval-fatigue mitigation in prioritization, grouping, and notification controls rather than approval bypass.

### C2 — AI allowlist v1 remains allow-by-default and contradicts the deployed two-command set

- **Severity:** Critical.
- **Current evidence:** `addendum.md` §Command Allowlist v1; `prd.md` §Increment M1, §Command and Query Contracts, FR19, FR43, FR75a–FR75g, and A8.
- **Why unresolved:** The addendum still defines v1 as the full operation catalog minus exclusions. That catalog includes outbound, service-client, and governance mutations. Neither the addendum nor PRD pins exact AI-invocable membership.
- **Decision conflict:** There is no exact-membership conflict in the recovered memlog because it only says execution is allowlisted. Orientation records a later deployed decision for exactly `Project.AppendConversationMessage` and `ChatBot.ExecuteLowRiskAssistance`; that specific decision must be recovered into the canonical memlog.
- **Recommended edit:** Replace v1 with an immutable, version-identified, deny-by-default set containing exactly those two AI-invocable commands. Give each member explicit effect, risk, required approval, actor/scope, and idempotency metadata. Separate three concepts throughout the PRD: the complete product operation catalog, per-surface exposure policy, and AI-invocable allowlist. Explicitly mark outbound, identity, tenant-policy, allowlist, permission, and admin mutations human/service-only and never AI-invocable in MVP.

### C3 — The post-commit audit sequence cannot satisfy fail-closed mutation

- **Severity:** Critical.
- **Current evidence:** `addendum.md` §Shared Command Pipeline; `prd.md` FR81a, NFR15, NFR15a, NFR49a, and NFR50a.
- **Why unresolved:** The specified pipeline commits and publishes before post-commit audit emission, while NFR15a and the memlog require no durable mutation if the audit writer is unavailable. A pre-commit readiness check cannot prevent a crash or writer failure between domain commit and completion-audit durability.
- **Decision conflict:** The pipeline contradicts recovered decision 14. The recommended edit implements that decision.
- **Recommended edit:** Define one atomic durability boundary for the domain event, idempotency record, applied policy/approval references, and canonical audit envelope—normally through the EventStore transaction/stream or a transactional outbox whose record is committed with the domain event. Treat the investigation-view/audit projection as rebuildable and subject to a projection-lag SLO; projection delay must not retroactively turn a committed command into an unaudited mutation. Rewrite FR81a and the addendum pipeline order to distinguish canonical audit durability from post-commit audit projection.

### C4 — Tenant isolation is proven after affected stores and machine surfaces ship

- **Severity:** Critical.
- **Current evidence:** `prd.md` §Minimum Release Slice, §Data Governance Surface, §Security and Isolation Acceptance Test Matrix, FR55a, NFR9a, NFR59, and NFR65.
- **Why unresolved:** M0 creates association, candidate, evidence, proposal, approval, projection, lifecycle, and workflow-map stores; M1 exposes programmable surfaces. FR55a/NFR9a and the native-store probe are still labeled M2.
- **Decision conflict:** Moving isolation earlier revises recovered decision 11's broad M2 assignment, but is required by recovered decision 8's every-increment safety floor.
- **Recommended edit:** Require tenant partitioning and negative native-store/API isolation tests in the first increment that creates each record class. Move the M0/M1 derived-store obligations into their respective exit gates. Retain M2 scope for vector/embedding/prompt-cache stores introduced in M2 and for recurring production probes. State that no store may enter multi-tenant use, and no CLI/MCP/service surface may expose it, until its below-application isolation test passes.

## High findings still unresolved

### H1 — Recovery cannot support an M2 release decision

- **Targets:** `prd.md` §Increment M2 and A10; NFR56–NFR59; `addendum.md` §Recovery-validation commitments.
- **Status:** Unresolved. The cited hosted bundle is expired, predates the required controlled-loss job, is not for the reviewed checkout, and cannot exercise the four-hour RTO boundary.
- **Decision interaction:** No reversal; recovered decision 11 requires recovery in M2 but does not claim the evidence is current.
- **Recommended edit:** Make fresh exact-commit hosted controlled-loss evidence a stop-ship M2 gate, and require either a full-window lane or a separately retained production-shaped/pre-production RTO drill. Keep A10 provisional until both exist. Move run IDs, commit-specific results, freshness calculations, and current measurements to a versioned qualification-evidence artifact; keep only targets, evidence kinds, owners, freshness rule, and gate state in the PRD/addendum.

### H2 — The M2 SLO catalog does not meet its own completeness contract

- **Targets:** §Increment M2, A11, NFR23, NFR42a, NFR43; `addendum.md` §Operating Baselines.
- **Status:** Unresolved. Several targets and nearly all error budgets are `calibration-pending`, and only audit projection lag has a live signal.
- **Decision interaction:** No conflict with the memlog; this makes recovered M2 operability testable.
- **Recommended edit:** Make completion of A11 calibration, numeric catalog values, live signal provenance, alert routing, and burn tests an explicit M2 entry/exit gate. Until evidence exists, label unavailable rows unsupported and block the corresponding production-readiness claim rather than publishing `unknown` as completion. Keep metric names and product targets authoritative in planning; do not make code authoritative over the approved requirement.

### H3 — Association, task-intent, and action-risk classifiers are conflated

- **Targets:** FR26, FR35, FR39, A9a, NFR15a; `addendum.md` §§Confidence Thresholds and Risk Classifier.
- **Status:** Unresolved. The artifacts still share a “kernel/domain” across incompatible numeric and categorical contracts, and `actionable` is absent from A9a.
- **Decision interaction:** Compatible with recovered decisions 4 and 6; separation clarifies them.
- **Recommended edit:** Define three separate versioned contracts: `AssociationScorer`, `TaskIntentDetector`, and `ActionRiskClassifier`. For each, specify inputs, output type, label taxonomy, version, dataset partition, calibration target, and failure state. Add `actionable`/`informational` to the task-intent taxonomy or replace FR26/FR35 labels with an explicitly named taxonomy. Treat calibration datasets as offline qualification inputs, not runtime dependencies whose outage blocks a proposal.

### H4 — Below-`T_low` behavior and scorer failure have inconsistent lifecycle outcomes

- **Targets:** `prd.md` §Technical Success, §Shared Workflow Contract, §Association Lifecycle and States, FR4–FR6, FR9–FR10, FR23; `addendum.md` §Confidence Thresholds.
- **Status:** Unresolved. The PRD says below `T_low` is deferred or rejected, while the addendum says `NeedsReview`; scorer failure also maps to `NeedsReview` but with different evidence.
- **Decision interaction:** Recovered decision 4 favors fail-closed authorized review, so `NeedsReview` is the consistent destination.
- **Recommended edit:** Publish one canonical score/outcome/state table. Use `NeedsReview` for below-threshold, no-candidate, deterministic-conflict, scorer-error, stale-evidence, and unauthorized-evidence cases, with distinct reason codes and candidate-list visibility rules. Reserve `Deferred` and `Rejected` for explicit authorized human decisions. Reference this table from Technical Success, FRs, lifecycle, and addendum.

### H5 — Acceptance readiness covers only four FR groups

- **Targets:** `prd.md` §Functional Acceptance Guidance; uncovered groups FR13–FR38, FR47–FR54, FR64–FR80, and FR90–FR96.
- **Status:** Unresolved. The existing matrices cover only FR1–FR12, FR39–FR46, FR55–FR63, and FR81–FR89.
- **Decision interaction:** No conflict.
- **Recommended edit:** Add group-level acceptance matrices for participants/identity, conversation/task intent, files, outbound, admin/operations, and workflow/recovery. Each matrix must cover happy path, authorization, ambiguity/failure, idempotency/concurrency, audit, redaction, and increment gate. If any matrix cannot be completed, mark that group blocked with an owner and prerequisite rather than calling all stories ready.

### H6 — Tenant Policy Schema is typed only for M0 and is unsafe even there

- **Targets:** `addendum.md` §Tenant Policy Schema; FR48d, FR52, FR73–FR75g; NFR12, NFR23, NFR35, NFR38, NFR46, NFR49a, NFR53, and NFR54.
- **Status:** Unresolved. M1/M2 knobs are prose categories without types, defaults, authorizers, dependencies, failure behavior, or migration rules; the M0 low-risk knob is itself unsafe under C1.
- **Decision interaction:** No reversal. It completes the recovered full-governance and safety decisions.
- **Recommended edit:** Publish a closed, versioned table for every M0/M1/M2 knob, including approval routing, authenticity, outbound authority, allowlist pin, classifier behavior, notification/priority, residency, retention, replay, idempotency, dashboard visibility, and operational limits. For each row define type/range, safe default, sensitivity, authorized mutator, increment, validation rule, dependency/failure behavior, and migration. Add cross-knob invariants and drift/invalid-policy tests.

### H7 — The approved interactive chat surface is still only a narrative note

- **Targets:** §Product Scope, §Increment M1, UJ1/System Journey, §UI Surface Inventory, §Journey Requirements Summary, §Traceability Overview, §Project Conversation and Context, FR21–FR28, FR35–FR46, FR81/FR81a, NFR60–NFR64; remove/supersede the §Vision `[NOTE FOR PM]` framing.
- **Status:** Unresolved. No named surface or FR defines submit/admission, AI response, risky-request proposal conversion, streaming, stop/cancel, retry/idempotency, failure state, audit outcome, or accessibility.
- **Decision interaction:** The recommendation directly implements recovered decision 12 and extends recovered WCAG decision 16 to the added UI.
- **Recommended edit:** Assign the governed composer explicitly to M1 (after the M0 spine, within the MVP release), extend S1 or create a stable surface ID, and add stable FRs for message submission through CommandGateway, admission outcome, safe AI response streaming, stop/cancel, proposal conversion for risky requests, retries/idempotency, typed failure outcomes, and audit attribution. Add NFR60 coverage and journey/trace links. Keep transport/mechanism details in the addendum or architecture artifact.

### H8 — M0 accepts external email before an authenticity floor exists

- **Targets:** §Increment M0/M1, UJ3, FR48a–FR48d, NFR31, and `addendum.md` §Inbound Message Authenticity.
- **Status:** Unresolved. The core M0 journey accepts external participants and attachments, but authenticity evidence and policy controls remain M1.
- **Decision interaction:** Moving the minimum floor earlier supports recovered decision 7's controlled-M365/external-party commitment.
- **Recommended edit:** Move the minimum provider-verdict passthrough, header-discrepancy capture, delegated-sender evidence, external-sender posture, safe default, and fail-closed routing into M0. Leave only advanced tenant tuning and broader provider compatibility in M1. If delivery cannot support this, restrict M0 explicitly to trusted internal mailboxes and remove the external-party M0 claim; the first option better preserves the product thesis.

### H9 — Replay isolation covers the wrong evidence and only one side-effect path

- **Targets:** FR95, FR95a, NFR69; `addendum.md` §Replay Isolation; M2 exit gate.
- **Status:** Unresolved. A clean production outbound-trace store does not prove no email, tool/model call, command, or state mutation occurred.
- **Decision interaction:** Strengthens recovered M2 replay isolation and no-mutation decisions.
- **Recommended edit:** Deny production credentials/resources at composition time for replay; replace every effectful adapter (mail, tools/models, commands, files/state, queues) with replay-safe implementations; enforce egress denial; and assert before/after invariance of all relevant production stores/resources. Make these proofs part of the M2 release gate.

### H10 — Mutation and human-decision idempotency is time-windowed and conflict-prone

- **Targets:** `addendum.md` §Idempotency Keys and §Shared Workflow Contract; FR90; NFR13, NFR13a, NFR14, and NFR19.
- **Status:** Unresolved. A 60-second command hash allows delayed mutation duplicates; actor and decision-kind fields allow competing decisions to generate different keys.
- **Decision interaction:** Implements recovered decision 14 more rigorously.
- **Recommended edit:** Introduce stable business `operation_id` / `decision_slot_id` values with indefinite deduplication for durable mutations and one unique authoritative decision slot per workflow/action. Use expected aggregate/workflow revision for concurrency, define duplicate-equivalent versus semantic-conflict outcomes, and retain request hashes as evidence rather than identity. Keep short replay windows only for non-mutating proposals where safe.

### H11 — The canonical bounded-context map omits Hexalith.Conversations

- **Targets:** §Context Ownership (canonical), §Shared Workflow Contract ownership, §Integration List, §Integration Contracts, §Command and Query Contracts, and `addendum.md` §Command Allowlist v0 / §ID Evolution Contract.
- **Status:** Unresolved. The PRD gives Projects conversation-boundary ownership while the addendum writes `Project.AppendConversationMessage` to Hexalith.Conversations.
- **Decision interaction:** Refines recovered decision 3. It preserves the source-of-truth split but adds the missing owner/dependency.
- **Recommended edit:** Name Hexalith.Conversations as owner of conversation identity/messages, add it to the integration list with a pinned contract/version, define Projects as referencing conversation IDs within project membership/boundaries, and use the owning context's canonical append command name. Update the memlog with the clarified ownership decision.

### H12 — The lifecycle has no single executable transition contract

- **Targets:** §Shared Workflow Contract, §Association Lifecycle and States, §Minimum Release Slice, FR67, FR76–FR80, FR87–FR91a, NFR13–NFR19, and `addendum.md` §Confidence Thresholds / §Idempotency Keys.
- **Status:** Unresolved. State definitions exist, but commands, actors, guards, increment availability, expected revisions, audit events, and successor rules are not combined into one authoritative matrix.
- **Decision interaction:** Builds on recovered decision 13. Preserve `Skipped` in M0 and log that choice as the explicit resolution of the legacy M1-only conflict.
- **Recommended edit:** Add a versioned transition table with source state, command, actor/authority, guard/reason, destination, increment, expected revision/concurrency behavior, audit event, and terminal/supersession semantics. Include `Deferred`/`NeedsReview` exit paths, below-threshold/scorer-error mapping, correction substates, and terminal reprocessing.

### H13 — The reproducible brownfield source baseline is still absent

- **Targets:** PRD frontmatter `inputDocuments` / `documentCounts`, §Project Classification and material-change re-check protocol; addendum frontmatter/provenance note.
- **Status:** Unresolved. `.memlog.md` now exists, but the PRD still uses a machine-specific `D:/...` path, reports zero project-context sources, and does not pin sibling paths/revisions. Some transitive source artifacts are absent.
- **Decision interaction:** No conflict.
- **Recommended edit:** Replace the machine-specific path with a repository-relative path; enumerate direct and transitive source artifacts with repository-relative paths plus revision/date; identify missing/unverifiable inputs; and add a versioned source/re-check manifest. Update the material-change protocol and addendum audit-trail wording to use `.memlog.md`, not `.decision-log.md`.

## Medium findings still unresolved

### M1 — General user upload lacks an explicit disposition and scope reductions lack a canonical approval mapping

- **Targets:** §Product Scope, §Growth Features/Post-MVP, §B2B SaaS Non-Goals, §Files and Attachments (FR29–FR34), frontmatter source/change history.
- **Status:** Unresolved. The brief included user upload; the PRD addresses only mailbox attachments and does not explicitly retain, defer, or drop upload.
- **Decision interaction:** Explicit post-MVP deferral is consistent with recovered decision 15's narrow email wedge.
- **Recommended edit:** Mark general user-upload ingestion post-MVP, with rationale that MVP proves governed mailbox attachment capture first. Add a retained/deferred/dropped mapping for material brief scope and name the product approver/source decision; mark the older brief superseded for MVP scope where appropriate.

### M2 — Success outcomes have no stable IDs

- **Targets:** §Measurable Outcomes, §Traceability Overview, and M0/M1/M2 exit gates.
- **Status:** Unresolved.
- **Decision interaction:** No conflict.
- **Recommended edit:** Assign stable `SM1...` IDs, explicitly pair primary metrics with counter-metrics (for example association precision with unauthorized false positives/correction rate), and cite the SM IDs from journeys, traceability, and increment gates.

### M3 — Mutable implementation evidence is embedded in the product contract

- **Targets:** A10, the chat PM note, `addendum.md` §§Operating Baselines and Recovery-validation commitments.
- **Status:** Unresolved.
- **Decision interaction:** No product-decision reversal.
- **Recommended edit:** Create a versioned qualification-evidence artifact for run locators, commits, freshness, current measurements, implementation/story status, and code drift. Retain only invariant targets, evidence requirements, owners, expiry/recheck policy, and current gate state in the PRD/addendum. Requirements remain authoritative over code catalogs.

### M4 — GDPR erasure versus immutable WORM retention is not an approved contract

- **Targets:** §Compliance Requirements, §Data Governance Surface, A6, NFR49a, NFR53, and NFR54.
- **Status:** Unresolved and not safe to auto-author as fact. Legal basis, hold precedence, key granularity, backup propagation, and surviving metadata require an accountable data-protection decision.
- **Decision interaction:** No recovered decision settles these details.
- **Recommended edit:** Mark A6 as a pre-pilot phase blocker owned by Compliance/Data Protection + Architecture. Require an approved data-class contract covering purpose/legal basis, retention, legal-hold precedence, key scope, crypto-erasure propagation (including backups), surviving metadata, and proof/audit. Do not claim GDPR satisfaction until that decision is attached.

### M5 — Artifact freshness and lineage remain misleading

- **Targets:** PRD and addendum frontmatter, edit history, status, author date, source manifest, and addendum audit-trail statement.
- **Status:** Partially resolved: `.memlog.md` now exists and contains 16 recovered decisions. Unresolved: May timestamps, stale `status: final`, absent later change history, machine path, and legacy `.decision-log.md` references.
- **Decision interaction:** No conflict.
- **Recommended edit:** Set PRD status to `draft` for the update, refresh `updated`/`lastEdited`, append the June/September change history and source manifest, update addendum metadata/provenance, and append every chosen override/change to `.memlog.md` through the required memlog script. Return to `final` only after reconciliation and reviewer gates pass.

### M6 — ID evolution assumes an unaccepted cross-repository event

- **Targets:** `addendum.md` §ID Evolution Contract; §Integration Contracts; §Context Ownership; A new external-dependency assumption/open item.
- **Status:** Unresolved. `IdentityEvolved` is treated as binding without verified producers, owners, schema/version, ordering, authorization, replay/idempotency, or reconciliation.
- **Decision interaction:** No conflict; recovered ownership requires contracts but does not establish this one.
- **Recommended edit:** Demote `IdentityEvolved` from binding fact to an owned external dependency until producer teams accept it. Define event schema/version, authorization, ordering/replay/idempotency, compatibility rollout, missed-event reconciliation query, and consumer fallback. Record producer acceptance before restoring binding status.

### M7 — The one-release/three-increment model lacks enforceable gate semantics

- **Targets:** frontmatter `releaseMode`, §Minimum Release Slice, §Project Scoping, increment must-haves, and NFR65.
- **Status:** Unresolved.
- **Decision interaction:** Preserve recovered decision 8: one MVP release with fixed M0 → M1 → M2 order.
- **Recommended edit:** Define M0 as a controlled pilot-preview deployment, M1 as the governed cross-surface pilot, and M2 as the MVP production/release-candidate gate. For each, specify audience/environment, mandatory safety/evidence gates, owner and approver, rollback/disable conditions, and permitted claims. Clarify that pilot deployment does not equal MVP/production approval.

## Low finding still unresolved

### L1 — Repeated risk and ownership material obscures canonical sections

- **Targets:** §Key Product Risks to Validate Early, §Risk Mitigations, §Innovation Risk Mitigation, §Risk Mitigation Strategy, §Shared Workflow Contract ownership, and §Context Ownership.
- **Status:** Unresolved.
- **Recommended edit:** After substantive fixes, keep one canonical risk register and one canonical ownership section. Replace duplicate prose elsewhere with short references and only retain genuinely local implications.

## Recommended edit sequence

1. **Reopen and normalize provenance:** mark the PRD draft, refresh dates/history, create the source/re-check and qualification-evidence companions, update `.memlog.md` references, and append the exact allowlist, `Skipped`, and storage-isolation timing decisions.
2. **Close the four Critical blockers:** make mandatory-approval classes non-downgradable; pin the exact two-command AI set; define atomic canonical audit durability; move per-store isolation proof to store introduction.
3. **Repair the security timing/contracts:** move the M0 authenticity floor earlier, strengthen replay containment, and replace time-window mutation idempotency with stable business-operation/decision identities.
4. **Publish deterministic product contracts:** split the three classifiers, define the canonical confidence-to-state mapping, and add the executable lifecycle transition table.
5. **Reconcile product shape:** integrate the governed chat surface as an M1 MVP surface with FR/NFR/traceability coverage; correct Conversations ownership; explicitly defer general user upload.
6. **Complete downstream decision inputs:** finish acceptance matrices, add stable success-metric IDs/counter-metrics, type the full tenant-policy schema, and define increment/release gates.
7. **Keep evidence and unresolved governance honest:** leave A10/A11 as gated evidence work, make the data-protection decision a pre-pilot blocker, and demote unaccepted `IdentityEvolved` dependencies.
8. **Polish only after resolution:** consolidate repeated risk/ownership prose, audit memlog coverage, reconcile every source, and run targeted rubric + adversarial re-validation. Do not restore `status: final` until all Critical and High findings are closed or explicitly accepted by an authorized decision-maker with owner and revisit condition.

## Minimum targeted re-validation

The next reviewer pass should directly verify:

- exact AI allowlist membership and non-downgradable approval effects;
- crash-atomic canonical audit durability;
- isolation proof at every store's first increment;
- M0 inbound-authenticity floor;
- executable score/state and lifecycle transitions;
- stable mutation and decision idempotency;
- integrated chat behavior and accessibility;
- full closed tenant-policy schema;
- fresh exact-commit controlled-loss/RTO evidence and complete live SLOs; and
- reproducible source lineage plus current memlog coverage.
