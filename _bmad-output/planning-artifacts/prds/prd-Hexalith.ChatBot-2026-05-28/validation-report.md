# Validation Report — Hexalith.ChatBot

- **PRD:** /home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
- **Addendum:** /home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
- **Rubric:** /home/administrator/projects/hexalith/chatbot/.agents/skills/bmad-prd/assets/prd-validation-checklist.md
- **Run at:** 2026-09-13T12:59:10+02:00
- **Grade:** Poor
- **Gate:** STOP — not safe for product approval or unqualified downstream handoff

## Overall verdict

The product thesis, named journeys, staged scope, measurable outcomes, and non-negotiable trust floor are unusually substantive. The current PRD/addendum pair is not safe to use as the sole implementation or release authority, however: two binding AI-governance rules permit readings that contradict mandatory approval and least privilege, while late chat-surface and recovery changes have not been reconciled through the full requirement set. Treat the product strategy as sound but the requirements contract as blocked until the critical and high findings below are resolved.

The adversarial reviewer independently confirmed the stop decision and materially widened the risk picture. In addition to the two AI-governance contradictions, it found that the post-commit audit sequence cannot satisfy the stated fail-closed invariant and that native-store tenant-isolation proof arrives after affected stores and programmable surfaces ship. The two reviews reported 6 Critical, 17 High, 9 Medium, and 1 Low findings before deduplication; the six Critical reports describe four unique blockers.

## Dimension verdicts

- Decision-readiness — broken
- Substance over theater — adequate
- Strategic coherence — strong
- Done-ness clarity — thin
- Scope honesty — thin
- Downstream usability — thin
- Shape fit — adequate

## Findings by severity

Counts below are reviewer-reported and intentionally not deduplicated. Repeated findings show independent confirmation; the source label identifies the full review.

### Critical (6 reports; 4 unique blockers)

**[Rubric / Decision-readiness] — The approval-fatigue override contradicts the safety invariant (§Task Intent and AI Action Mediation; FR41; NFR16; addendum §Tenant Policy Schema)**

FR41 requires approval for state mutation, file exposure, external send, task creation, tool invocation, and acting on behalf, while the PM note and schema permit those same classes to be marked low-risk-allowed.

Fix: Restrict low-risk-allowed to enumerated read-only action subtypes; make all six boundary-crossing classes structurally non-downgradable and reject policy snapshots that attempt otherwise.

**[Rubric / Decision-readiness] — M1's AI-command allowlist is denylist-shaped and can include privileged mutations (§Command and Query Contracts; FR19, FR43, FR75a–FR75g; addendum §Command Allowlist v1)**

The binding v1 definition is the full catalog minus commands tagged disallowed-for-AI, but no exact membership or complete tag set exists; the catalog includes outbound, identity, and governance mutations.

Fix: Use an exact versioned, deny-by-default AI-invocable set; enumerate approval metadata for every member and mark outbound, identity, policy, allowlist, and admin mutations human/service-only.

**[Adversarial / C1] — Tenant policy can exempt the exact action classes declared non-negotiably approval-required (addendum §Tenant Policy Schema; PRD FR41 and related MVP commitments)**

Independent confirmation of the rubric's first Critical finding. A tenant administrator can interpret the current schema as authorizing approval-free external send, file exposure, project mutation, or acting on behalf.

Fix: Make the six effect classes non-overridable throughout MVP; allow exemptions only for explicitly enumerated read-only, no-external-effect actions.

**[Adversarial / C2] — Binding and deployed AI allowlists disagree between almost every command and exactly two commands (addendum §Command Allowlist v1; decision log §2026-06-03)**

Independent confirmation of the rubric's second Critical finding. The addendum allows the full catalog minus exclusions, while the later deployed decision allows only Project.AppendConversationMessage and ChatBot.ExecuteLowRiskAssistance.

Fix: Replace the addendum rule with the exact two-command, immutable, version-identified set; separate the product operation catalog, surface exposure policy, and AI-invocable allowlist.

**[Adversarial / C3] — Fail closed when audit is down is impossible under the specified post-commit audit sequence (addendum §Shared Command Pipeline; FR81a; NFR15/NFR15a; NFR49a/NFR50a)**

The pipeline commits and publishes before post-commit audit emission, but the NFRs require no durable mutation when the audit writer is unavailable. No atomic boundary, outbox, compensation, or canonical-event-as-audit rule closes the crash gap.

Fix: Define one atomic durability boundary for the domain event, idempotency record, policy/approval references, and canonical audit envelope; treat investigation-view projection lag as an SLO, not mutation failure.

**[Adversarial / C4] — Store-level tenant-isolation proof is scheduled after affected stores and machine surfaces release (§Minimum Release Slice; §Data Governance Surface; FR55a; NFR9a)**

M0 creates candidate/evidence/approval stores and M1 exposes programmable clients, yet the native-store no-filter isolation guarantee and nightly probe are assigned to M2.

Fix: Move partitioning and negative native-API isolation tests into the first increment that creates each store, with explicit exit gates before multi-tenant use.

### High (17)

**[Rubric / Decision-readiness] — M2 cannot receive an evidence-based recovery release decision (§Increment M2; A10; NFR56–NFR59; addendum §Recovery-validation commitments)**

The hosted run expired under the document's eight-day rule, predates controlled-loss validation, and cannot test a four-hour RTO inside a 180-second lane.

Fix: Define the stop-ship gate and require fresh exact-commit controlled-loss evidence plus a full-window or separately evidenced pre-production RTO drill.

**[Rubric / Decision-readiness] — The M2 SLO catalog fails NFR42a's completion contract (NFR42a; addendum §Operating Baselines)**

Multiple targets and nearly all error budgets remain calibration-pending, and only audit projection lag has a live signal.

Fix: Make A11 calibration and live-signal wiring an M2 gate, or provide bounded provisional values and ratcheting rules for every required field.

**[Rubric / Done-ness clarity] — Three classifiers are conflated into one undefined kernel (addendum §§Confidence Thresholds, Risk Classifier; FR35; A9a)**

Association scoring, task-intent detection, and action-risk classification have incompatible inputs, numeric/categorical outputs, and label taxonomies.

Fix: Define separate versioned contracts, datasets, calibration targets, and failure states for AssociationScorer, TaskIntentDetector, and ActionRiskClassifier.

**[Rubric / Done-ness clarity] — Below-T_low association has incompatible lifecycle outcomes (§Technical Success; §Shared Workflow Contract; addendum §Confidence Thresholds)**

The PRD says deferred or rejected, while the addendum says NeedsReview; scorer failure also maps to NeedsReview with different evidence.

Fix: Publish one canonical score/outcome/state table covering every band and failure case.

**[Rubric / Done-ness clarity] — Acceptance readiness is specified for only four FR groups (§Functional Acceptance Guidance; §Functional Requirements)**

Participant/identity, conversation/context, files, outbound, admin/operations, and parts of recovery remain dependent on downstream invention.

Fix: Complete the group-level acceptance matrix or explicitly mark every uncovered group blocked with an owner and prerequisite artifact.

**[Rubric / Done-ness clarity] — The binding tenant-policy schema is complete only for M0 (addendum §Tenant Policy Schema; FR52; FR73–FR75g; NFR35)**

M1/M2 controls are prose categories rather than typed knobs with defaults, sensitivity, validation, and migration behavior.

Fix: Enumerate every M1/M2 knob and cross-knob invariant in the declared schema format.

**[Rubric / Scope honesty] — The approved interactive-chat scope exists only as a PM note (§Vision; §UI Surface Inventory; §Functional Requirements)**

The composer is absent from the surface inventory, increment scope, FRs, accessibility scope, and traceability.

Fix: Reconcile the change into scope, a named surface, journeys, FRs, NFR60, commands, failure semantics, and traceability.

**[Rubric / Downstream usability] — The brownfield source baseline cannot be reproduced (frontmatter inputs/counts; §Project Classification; material-change protocol)**

The only direct input uses a machine-specific D:/ path, project context is counted as zero, and sibling sources lack exact paths/revisions.

Fix: Use repository-relative paths, revisions/dates, and a versioned source/re-check manifest.

**[Adversarial / H1] — M0 trusts external email before inbound-authenticity controls exist (§Increment M0; Journey 3; FR48a–FR48d; addendum §Inbound Message Authenticity)**

M0 associates external mail and admits attachments/context before DMARC/DKIM/SPF evidence and delegated-sender controls arrive in M1.

Fix: Bring a minimum authenticity floor into M0 or limit M0 to trusted internal mailboxes.

**[Adversarial / H2] — Replay isolation proves absence in the wrong store and ignores most side effects (addendum §Replay Isolation; FR95/FR95a; NFR69)**

The probe can remain green while a misbound production adapter sends email, calls live tools/models, or mutates production state.

Fix: Deny production credentials/resources at composition time, replace every effectful adapter, enforce egress controls, and assert production-state before/after invariance.

**[Adversarial / H3] — Idempotency keys do not prevent delayed duplicate mutations or conflicting human decisions (addendum §Idempotency Keys; §Shared Workflow Contract; NFR13/NFR14)**

State-changing command deduplication lasts only 60 seconds and actor/decision-kind fields allow conflicting decisions to produce different keys.

Fix: Use stable business operation/decision IDs with indefinite mutation idempotency, expected revisions, and a unique decision slot.

**[Adversarial / H4] — The core Conversations bounded context disappears from the canonical ownership boundary (§Context Ownership; §Integration List; addendum §Command Allowlist v0)**

The brief assigns conversations to Hexalith.Conversations, while the canonical PRD omits it, assigns conversation boundaries to Projects, and names an inconsistent append command.

Fix: Name one owner for conversation identity/messages, add the integration and contract version, and define the Projects reference boundary.

**[Adversarial / H5] — Three security-relevant classifiers are conflated into one undefined kernel (addendum §§Confidence Thresholds, Risk Classifier; FR26; FR35; A9a)**

Independent confirmation of the rubric classifier finding, including the absent actionable label and an improper live dependency on calibration data.

Fix: Separate and version all three classifier contracts; make calibration data offline rather than a runtime availability dependency.

**[Adversarial / H6] — The lifecycle has no single executable transition contract (§Technical Success; §Shared Workflow Contract; §Association Lifecycle; decision log; addendum §Confidence Thresholds)**

Threshold results, Skipped increment ownership, paths out of Deferred/NeedsReview, and terminal/superseding semantics conflict.

Fix: Publish a versioned transition table with commands, actors, guards, destinations, increments, concurrency, audit events, and supersession rules.

**[Adversarial / H7] — The chat-surface scope change was not integrated as a product contract (product brief; PRD §Vision note, surfaces, FR21–FR28, command contracts, NFR60; sprint-change proposal)**

Independent confirmation of the rubric chat-surface finding, extended to streaming, stop/cancel, risky-request conversion, retry/idempotency, and audit outcomes.

Fix: Add the versioned surface and complete behavioral, command, failure, telemetry, accessibility, and traceability contract.

**[Adversarial / H8] — M2 production readiness uses expired, commit-stale recovery evidence (§Increment M2; A10; NFR56–NFR59; addendum recovery commitments; decision log)**

Independent confirmation of the rubric recovery finding, with the added mismatch between evidence commit 17aa94d and reviewed checkout f0ba70e.

Fix: Require a fresh exact-commit hosted controlled-loss bundle plus production-shaped/full-window recovery evidence before MVP-complete claims.

**[Adversarial / H9] — The master tenant-policy schema omits most required knobs (addendum §Tenant Policy Schema; FR48d; NFR12; NFR46)**

Independent confirmation of the rubric schema finding, including authenticity, authority, allowlist, routing, residency, retention, replay, and idempotency controls.

Fix: Publish the full closed schema with types, safe defaults, authorizers, sensitivity, validation dependencies, failure behavior, and drift tests.

### Medium (9)

**[Rubric / Scope honesty] — General user-upload ingestion disappeared without an explicit disposition (§MVP; §Files and Attachments; §Out of scope)**

The brief included user uploads, while the PRD covers mailbox attachments and neither commits nor defers general upload.

Fix: Assign it to an increment or explicitly mark it post-MVP/non-goal with rationale.

**[Rubric / Downstream usability] — Success outcomes have no stable IDs (§Measurable Outcomes; §Traceability Overview)**

Strong numeric metrics cannot be cited stably by stories or release evidence.

Fix: Add SM identifiers, identify metric/counter-metric pairs, and reference them from traceability and increment gates.

**[Rubric / Shape fit] — Requirements are mixed with mutable implementation evidence (A10; PM note; addendum §§Operating Baselines, Recovery-validation commitments)**

Commit/run status and code-as-authority statements make the requirements artifact age with implementation evidence and invert product authority.

Fix: Keep targets and gates in the PRD; move run locators and current measurements to a versioned qualification-evidence artifact.

**[Adversarial / M1] — GDPR erasure and immutable WORM retention remain an assumption (§Compliance; Data Governance; A6; NFR49a/NFR53/NFR54)**

Seven-year retention and crypto-erasure lack DPO/legal approval, key granularity, backup propagation, legal-hold precedence, and surviving-metadata rules.

Fix: Obtain a data-protection decision and define purpose, legal basis, hold precedence, key scope, destruction propagation, and proof.

**[Adversarial / M2] — Brief reductions are not governed as a signed scope change; user upload silently disappears (product brief; PRD scope; decision log)**

This independently confirms the upload gap and notes that other reductions have decisions but no named product approver.

Fix: Create a signed retained/deferred/dropped mapping, decide upload, and mark the brief superseded or updated.

**[Adversarial / M3] — Final metadata and source lineage are stale (PRD/addendum frontmatter; decision log; missing .memlog.md)**

May timestamps coexist with June/August content, the direct source path is machine-specific, and later decisions live only in the legacy log.

Fix: Recover the canonical memlog, pin repository-relative inputs/revisions, refresh metadata, and distinguish final-planning from release-ratified.

**[Adversarial / M4] — The operating-baseline catalog has missing targets and only one live signal (addendum §Operating Baselines; NFR42a/NFR43; A11)**

This is the adversarial counterpart to the rubric's High SLO finding.

Fix: Make A11 calibration an entry/exit gate and require numeric catalog rows, live provenance, routing, and burn tests.

**[Adversarial / M5] — ID evolution invents an unowned cross-repository dependency (addendum §ID Evolution Contract; §Integration Contracts; §Context Ownership)**

The required IdentityEvolved event is not established in sibling contracts and lacks owners, versioning, ordering, idempotency, and reconciliation.

Fix: Secure producer acceptance and define the versioned event/reconciliation contract before treating it as binding.

**[Adversarial / M6] — Single release and independently releasable increments lack enforceable gate semantics (frontmatter; §Minimum Release Slice; §Project Scoping)**

Pilot, release, production, and MVP-complete are used without a release-state model, evidence authority, or rollback/disable conditions.

Fix: Define environments/audiences, safety gates, evidence owners, approvers, rollback rules, and allowed claims for M0/M1/M2.

### Low (1)

**[Rubric / Substance over theater] — Repeated risk and ownership sections obscure canonicality (§Key Product Risks; §Risk Mitigations; §Shared Workflow Contract; §Context Ownership)**

Repeated material makes it harder to know which section is authoritative.

Fix: Keep one canonical risk register and one canonical ownership section; turn repetitions into concise references.

## Mechanical notes

- PRD and addendum frontmatter still report 2026-05-28 despite approved June changes and August recovery evidence.
- The current workflow's canonical .memlog.md is absent; material later decisions live in the legacy .decision-log.md.
- The PRD places Skipped in M0, while the decision log says it is M1 and records no later reversal.
- Base FR1–FR96 and NFR1–NFR70 IDs are unique and contiguous; letter-suffixed additions are unique.
- Range references should state whether letter-suffixed IDs are included.
- Assumption markers resolve, but A9a does not define the actionable label used by FR35.
- All human journeys have named protagonists; the system journey is appropriately system-focused.
- The addendum sends classifier error-rate measurement to NFR50a, which defines audit-chain completeness instead.
- Historical May review variants remain beside the current reviews. They were read for context but are not treated as selected reviewers for this run.

## Reviewer files

- review-rubric.md — current rubric walker
- review-adversarial-general.md — current adversarial reviewer
- orient-extract.md — current source orientation

Historical context retained but excluded from this run's counts:

- review-rubric-v2.md
- review-adversarial-v2.md
