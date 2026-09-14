# Adversarial General Review — Hexalith.ChatBot PRD

**Reviewed:** 2026-09-13

**Primary artifact:** `prd.md`

**Binding context:** `addendum.md`

**Decision/source checks:** `.decision-log.md`, `../../product-brief-Hexalith.ChatBot.md`

**Reviewer stance:** Launch-level, chain-top product approval and downstream UX/architecture/story safety.

## Verdict

**STOP — not safe for product approval or unqualified downstream handoff. Counts: Critical 4, High 9, Medium 6, Low 0.** The product thesis and many control intentions are strong, but the current source set contains four direct trust-boundary failures: tenant policy can waive approval for the six effects the PRD says always require approval; the binding AI allowlist and the deployed allowlist authorize radically different command sets; the shared pipeline can commit state before the required completion audit exists; and native-store tenant-isolation proof is deferred until after affected derived stores and machine surfaces exist. High-severity gaps then leave inbound authenticity, replay containment, idempotency/concurrency, lifecycle behavior, core Conversations ownership, classifier contracts, the added chat surface, recovery evidence, and policy defaults unsafe or non-deterministic. UX exploration can continue against the stable user journeys, but architecture and story generation should treat the Critical and High findings below as blocking contracts, not implementation details to infer locally.

## Critical findings

### C1 — Tenant policy can exempt the exact action classes declared non-negotiably approval-required

- **Severity:** Critical
- **Location:** `addendum.md` §Tenant Policy Schema, lines 65-70; `prd.md` §Measurable Outcomes, lines 157-164; §MVP Scope, lines 201-215; §Task Intent and AI Action Mediation, FR41 and its note, lines 1231-1235; product brief §Solution and §MVP Scope, lines 44 and 96.
- **Evidence:** The product brief requires human approval before AI changes project folder content or sends information externally. The PRD repeats an absolute requirement for six effect classes: state mutation, file exposure, external send, task creation/assignment, tool invocation, and acting on behalf of a participant. The binding schema nevertheless defines `ai-action.low-risk-allowed` as a per-class Boolean map over those same six risky classes, and the FR41 note explicitly instructs administrators to ratchet selected classes to `true` when approvals are routinely accepted.
- **Impact:** A tenant administrator can convert external sends, file exposure, project mutation, or participant impersonation into approval-free AI behavior while the rest of the PRD still asserts that those effects never execute without approval. Architecture could implement either interpretation and still claim compliance. This is a direct breach of the central customer promise, not a policy-tuning detail.
- **Suggested fix:** Make the approval boundary non-overridable for the six effect classes throughout MVP. Replace the map with an allowlist of explicitly enumerated **read-only, no-external-effect** action kinds. If post-MVP exemptions are desired, introduce a separate versioned policy with explicit exclusions (external send, file disclosure, authority delegation, governance mutation), two-person approval, expiry, and a signed risk decision. Update FR40/FR41, the schema, glossary, acceptance tests, and source-brief trace together.

### C2 — The binding and deployed AI allowlists disagree between “almost every command” and exactly two commands

- **Severity:** Critical
- **Location:** `addendum.md` frontmatter line 7 and §Command Allowlist v1, lines 43-50; `.decision-log.md` §2026-06-03, lines 146-160; `prd.md` §Increment M1, lines 254-269; Glossary line 1100; NFR15a command/allowlist rows, lines 1388-1391.
- **Evidence:** The approved addendum says v1 is the full command catalog minus commands explicitly tagged `disallowed-for-AI`. The later decision log says the deployed v1 contains only `Project.AppendConversationMessage` and `ChatBot.ExecuteLowRiskAssistance`, and explicitly excludes outbound, governance, admin-queue, and policy/control mutations as human-only. The PRD still directs readers to the unchanged addendum as the current versioned allowlist. It also does not cleanly distinguish the AI-command allowlist from the human UI/CLI/MCP operation catalog in NFR15a.
- **Impact:** A downstream architect following the binding addendum can expose commands that the security sign-off deliberately withheld; an implementer following the decision log can fail PRD parity stories that assume the broader catalog. Missing or stale `disallowed-for-AI` metadata becomes allow-by-default under the addendum wording, which is the unsafe direction.
- **Suggested fix:** Replace the addendum v1 section with the exact, immutable, version-identified two-command membership recorded in the latest decision. Separate three concepts in names and schemas: product operation catalog, surface exposure policy, and AI-invocable allowlist. Make unknown/missing AI metadata `disallowed` by default. Record supersession in frontmatter and add contract tests that compare the checked-in artifact, runtime catalog, and audit-reported version byte-for-byte.

### C3 — “Fail closed when audit is down” is impossible under the specified post-commit audit sequence

- **Severity:** Critical
- **Location:** `addendum.md` §Shared Command Pipeline, lines 78-85; `prd.md` FR81a, line 1310; NFR15/NFR15a, lines 1377-1393; NFR49a/NFR50a, lines 1445-1448.
- **Evidence:** The pipeline performs a pre-commit audit **gate**, then EventStore command execution, event publication, projection update, and only afterward “post-commit audit emission.” NFR15/NFR15a simultaneously require every state-mutating path to write no durable state when the audit writer is unavailable. No atomic transaction, transactional outbox, canonical-event-as-audit rule, or compensation semantics joins the source mutation to the post-commit audit. NFR50a then tolerates up to 0.5% of state mutations being non-reconstructable, weakening what NFR15a calls an invariant.
- **Impact:** A failure after EventStore commit but before completion-audit persistence necessarily yields either unaudited state, duplicate re-execution, or an attempted rollback of an event-sourced commit. None matches the declared contract. The most important safety invariant therefore cannot be implemented or fault-tested consistently.
- **Suggested fix:** Define one atomic durability boundary. Prefer committing the domain event, idempotency record, policy/approval references, and canonical audit envelope in one EventStore append; project the investigation view asynchronously through an outbox. Specify the observable state for projection/audit-view lag without claiming the mutation failed. If a separate audit store remains mandatory, define prepare/commit/recovery semantics and prove every crash point. Make audit reconstructability 100% for admitted state mutations; use an SLO only for projection availability/lag.

### C4 — Store-level tenant isolation is scheduled after tenant-derived stores and machine surfaces are already released

- **Severity:** Critical
- **Location:** `prd.md` §Minimum Release Slice, lines 232-252 and 268-283; §Data Governance Surface, lines 500-517; FR55a, line 1261; NFR9a, line 1367; §Resource Risks, lines 1082-1088.
- **Evidence:** Candidate rankings and evidence snapshots exist in M0, candidate-ranking caches are explicitly covered by FR55a/NFR9a, and M1 exposes the workflow through CLI, MCP, and service clients. Yet FR55a/NFR9a are labeled M2, including the decisive guarantee that a native-store query without an application tenant filter must still fail. The PRD also says each increment is releasable and that tenant isolation is a non-negotiable floor in every increment.
- **Impact:** M0/M1 can be declared releasable while relying on application filtering for stores that hold project evidence and AI context. M1 adds the highest-risk programmable surfaces before the required lower-layer isolation probe exists. A single missing filter can disclose another tenant’s project names, evidence, or cached context during a supposedly safe pilot release.
- **Suggested fix:** Move store-level partitioning and negative native-API isolation tests into the first increment that creates each derived store. Candidate/evidence/approval caches belong in M0; their CLI/MCP access tests belong in M1; only newly introduced vector/embedding stores may wait until M2, and they must be gated before first write. Add explicit increment exit criteria that prohibit any multi-tenant deployment before the relevant native-store isolation suite passes.

## High findings

### H1 — M0 trusts external email before inbound-authenticity controls exist

- **Severity:** High
- **Location:** `prd.md` §Increment M0, lines 236-249; §Increment M1, line 269; Journey 3, lines 349-359; M1 must-haves, lines 1008-1021; FR48a-FR48d, lines 1247-1250; `addendum.md` §Inbound Message Authenticity, lines 124-131.
- **Evidence:** M0 ingests external project email, resolves the sender by email identity, auto-associates using project/thread identifiers, stores attachments, derives task intent, and supplies project context to AI. DMARC/DKIM/SPF verdict capture, header discrepancy handling, delegated-sender disambiguation, and external-sender posture do not arrive until M1. Journey 3 nevertheless promises that spoofing/mismatch is detected and fails closed.
- **Impact:** A spoofed or replayed external message containing a plausible project identifier or thread token can contaminate a project conversation, attachment store, and AI context during M0. Human approval of the later AI action does not repair poisoned source context.
- **Suggested fix:** Bring a minimum authenticity floor into M0: provider verdict capture, external/unresolved sender flagging, header inconsistency evidence, replay checks, and a policy that forbids auto-association for failing/absent authenticity evidence. Otherwise redefine M0 as trusted internal-mailbox-only and prohibit external pilot traffic until M1.

### H2 — Replay isolation proves absence in the wrong store and ignores most side effects

- **Severity:** High
- **Location:** `addendum.md` §Replay Isolation, lines 104-110; `prd.md` FR95/FR95a, lines 1329-1330; NFR69, line 1480.
- **Evidence:** The addendum test asserts that replay has never produced a record in a production tenant’s **outbound-trace store**. But the test-mode adapter is what writes “would-have-sent” trace records; a misrouted production adapter can send real email without writing that test trace at all. The contract intercepts outbound email, while NFR69 also promises isolation from production mutation, live AI tools, and live command execution.
- **Impact:** The nightly probe can stay green while replay sends real email, invokes an external tool, calls a live model, or mutates a production project through a wrongly bound resource. This is a false safety signal around a deliberately adversarial execution mode.
- **Suggested fix:** Deny production credentials and production resource identifiers in replay at composition time; replace **all** effectful adapters (mail, tools, model calls with retention effects, commands, file mutation) with test implementations; enforce outbound network policy; assert a production-state before/after diff; and use a canary external sink that proves zero egress. Test deliberate tenant/adapter misbinding, not only the happy test-tenant route.

### H3 — Idempotency keys do not prevent delayed duplicate mutations or conflicting human decisions

- **Severity:** High
- **Location:** `addendum.md` §Idempotency Keys, lines 87-102; `prd.md` §Shared Workflow Contract, lines 439-453; NFR13/NFR13a/NFR14, lines 1374-1376; §Dependency Failure Handling, lines 879-883.
- **Evidence:** Command execution deduplicates only `tenant + command_name + input_hash + requester_id` for 60 seconds. The same mutation can execute again after 60 seconds or when a retry changes requester identity. Association and approval decision keys include both `decision_actor` and `decision_kind`, so two reviewers—or one reviewer choosing different decision kinds—produce different keys for the same workflow decision. The PRD mentions stale confirmation/version conflict but defines no expected revision, unique active-decision constraint, or first-writer rule.
- **Impact:** Network retries, worker handoff, delayed clients, or simultaneous reviewers can append duplicate messages, execute a command twice, or persist both approval and rejection. The absolute NFR14 “no duplicates” promise is not achieved by the stated keys.
- **Suggested fix:** Give every proposed business operation a stable `operation_id`/`decision_id` that remains idempotent indefinitely for state-changing effects. Require aggregate expected revision and a unique decision slot for association/approval transitions. Define the exact response to concurrent approve/reject/correct commands and test cross-actor, cross-surface, delayed, and post-timeout retries.

### H4 — The core conversation bounded context disappears from the canonical architecture boundary

- **Severity:** High
- **Location:** Product brief §Executive Summary, line 25; `prd.md` §Project Classification, line 92; §Shared Workflow Context Ownership, lines 489-496; canonical §Context Ownership, lines 676-687; §Integration List, lines 853-869; `addendum.md` §Command Allowlist v0, lines 35-40.
- **Evidence:** The brief states that Hexalith.Conversations manages conversations, and the PRD initially names it as a dependency. The canonical ownership list later assigns “project conversation boundaries” to Hexalith.Projects and omits Hexalith.Conversations entirely. The MVP integration list also omits it. The sole M0 AI command is named `Project.AppendConversationMessage` while the addendum says it writes to Hexalith.Conversations.
- **Impact:** Downstream architecture cannot determine who owns conversation identity/messages, which service accepts the append command, which contract/version is required, or which failure/recovery boundary applies. Teams can duplicate conversation state in ChatBot or Projects while believing they followed the PRD.
- **Suggested fix:** Establish one explicit boundary: name the owner of conversation identity, threads, and message append; use the actual versioned command name; add Hexalith.Conversations to integrations, dependency failures, source revisions, and contract tests; and state what Projects owns by reference rather than overlapping authority.

### H5 — Three security-relevant classifiers are conflated into one undefined “kernel”

- **Severity:** High
- **Location:** `addendum.md` §Confidence Thresholds, lines 14-23, and §Risk Classifier, lines 25-33; `prd.md` FR26, lines 1198-1201; FR35, lines 1226-1230; A9a, line 1348; NFR15a AI-proposal row, line 1386.
- **Evidence:** The addendum says the same scoring kernel produces numeric association confidence and categorical action-risk classification, but the two sections list unrelated inputs and outputs. FR26 then says informational/actionable classification comes from that same risk kernel. FR35 assigns task intent a numeric score “in the same domain as Risk Classifier,” although the risk classifier has no numeric domain. FR35 calibrates against an `actionable` label absent from A9a’s declared taxonomy. NFR15a additionally makes runtime proposal admission depend on evaluation-dataset availability even though the dataset is described as offline calibration material.
- **Impact:** Implementers can accidentally couple association, content classification, and execution authorization, causing a model/calibration change in one concern to alter a safety decision in another. Acceptance targets cannot be reproduced because the label and score contracts are incomplete.
- **Suggested fix:** Define separately versioned `AssociationScorer`, `TaskIntentClassifier`, and `ActionRiskClassifier` contracts, each with inputs, outputs, failure behavior, calibration dataset labels, and runtime dependencies. Remove the “same kernel/domain” claims unless a formal multi-head contract is actually intended. Add `actionable` to A9a or remove it, and state that calibration data is never a live availability dependency.

### H6 — The lifecycle has no single executable transition contract

- **Severity:** High
- **Location:** `prd.md` §Technical Success, line 128; §Increment M0, line 246; §Shared Workflow Contract, lines 429-453; §Association Lifecycle, lines 805-823; `.decision-log.md` post-review tightening, lines 103-110; `addendum.md` §Confidence Thresholds, lines 20-23.
- **Evidence:** Below `T_low`, the PRD says messages are deferred or rejected, while the addendum says they enter `NeedsReview`. The decision log says only `Skipped` was M1, but the current PRD puts `Skipped` in M0 without a later recorded reversal. Line 821 claims the canonical state table defines M0/M1 scope, yet the table has no increment column. The transition expression provides no path out of `Deferred` or `NeedsReview`, and `Skipped` is called terminal while also allowing an unspecified “superseding decision.”
- **Impact:** UX, API, persistence, and tests will invent incompatible state machines. The same score can produce different queues; a deferred item may be impossible to resolve; and replay/reprocess may mutate a terminal instance on one surface while creating a successor on another.
- **Suggested fix:** Publish one versioned transition table with source state, command, actor/authority, guards, destination, terminal flag, increment introduced, concurrency rule, audit event, and reprocess/supersede semantics. Make threshold bands map to exactly one initial state. Update the decision log for the `Skipped` reversal and delete all duplicate prose definitions.

### H7 — The approved chat-surface scope change was added as a note, not integrated as a product contract

- **Severity:** High
- **Location:** Product brief §MVP Scope, line 89; `prd.md` §Vision note, line 317; §UI Surface Inventory, lines 519-539; FR21-FR28, lines 1191-1202; §Command and Query Contracts, lines 752-803; NFR60, lines 1464-1468; `sprint-change-proposal-2026-06-09.md` lines 54-56, 80-82, 151-179, and 218-228.
- **Evidence:** The later approved proposal classifies the governed composer as a major MVP-shape change and explicitly leaves a PM decision to add a surface/FR. The PRD only rewrites a narrative note. The inventory still has no composer/streaming surface, the conversation FRs are read/display requirements, the command catalog has no user chat-message submit/stop/cancel contract, NFR60 does not scope composer accessibility, and no increment is assigned beyond “Epic 10.”
- **Impact:** UX and stories must source core behavior from a sprint proposal and implementation epics rather than the chain-top PRD. Message admission, streaming, cancellation, partial output, risky-request conversion, retry/idempotency, and accessibility can diverge from the governed pipeline without violating any explicit PRD FR.
- **Suggested fix:** Integrate the approved change fully: add a versioned UI surface, FRs and command/query contracts for submit/respond/stream/stop/cancel/failure, increment ownership, NFR60 coverage, telemetry and audit outcomes, and traceability to the journeys. Record the change in PRD frontmatter and the decision log, then revalidate affected architecture/UX/stories.

### H8 — M2 “production readiness” depends on recovery evidence that is expired, commit-stale, and still cannot test the stated targets

- **Severity:** High
- **Location:** `prd.md` §Increment M2, lines 271-283; M2 must-haves, lines 1023-1033; A10, line 1349; NFR56-NFR59, lines 1455-1460; `addendum.md` §Recovery-validation commitments, lines 189-201; `.decision-log.md` lines 207-225 and 259-277.
- **Evidence:** The only hosted bundle was fresh through 2026-09-04 and predates the now-required controlled-loss job; on this review date it cannot carry ratification. It belongs to commit `17aa94d`, while the checked-out repository is `f0ba70ed9c76a88d0204e1d228562b476742c1b5`. Positive-loss RPO has only local evidence. The 180-second harness ceiling cannot falsify a four-hour RTO, and the source set explicitly leaves external M365, durable WORM, production control, and provider-scale residuals open. A10 remains provisional.
- **Impact:** The PRD calls M2 the increment that makes the system operable in production, but there is no currently valid evidence or achievable gate for its headline recovery commitments. Product approval could be misread as release approval despite known inability to verify NFR56/NFR57 at the required altitude.
- **Suggested fix:** Make M2 exit conditional on a fresh exact-commit hosted bundle including controlled loss, plus a full-window/pre-production RTO drill capable of observing both pass and miss. Require production-shaped M365/WORM/control-plane evidence or explicitly narrow the supported deployment claim. Keep A10 provisional in planning, but prohibit “production-ready/MVP complete” status until the named evidence gate passes.

### H9 — The “master” tenant policy schema omits most knobs that requirements rely on

- **Severity:** High
- **Location:** `addendum.md` §Tenant Policy Schema, lines 52-76; `prd.md` §Data Governance Surface, lines 502-517; FR48d, line 1250; NFR12, line 1370; NFR46, lines 1434-1439.
- **Evidence:** The addendum says every knob has type, allowed values, safe default, sensitivity, increment, and validation rule, but only the M0 subset is concretely enumerated. M1/M2 are prose placeholders. Requirements nevertheless depend on `mailbox.authenticity-strictness`, outbound authority toggles, `allowlist.version-pin`, approval routing and priority weights, `data.residency`, `audit.retention`, `ai-context.retention`, replay toggles, and idempotency windows without complete declarations. Several are security-sensitive and their safe invalid/unset behavior is not defined.
- **Impact:** Teams will invent defaults and sensitivity classes locally. An absent or malformed setting can broaden sender authority, retention, AI access, replay behavior, or command membership in one service while another fails closed.
- **Suggested fix:** Publish the full closed schema now, not in M2 prose: stable name, type/range, default, sensitivity, authorizer, two-person requirement, increment, validation dependencies, failure behavior, and audit event for every referenced knob. Add schema/code drift tests and require unknown knobs/values to fail closed.

## Medium findings

### M1 — GDPR erasure and immutable WORM retention remain an assumption, not an approved data-governance contract

- **Severity:** Medium
- **Location:** `prd.md` §Compliance & Regulatory, lines 564-570; Data Governance row defaults, lines 502-517; A6, line 1344; NFR49a/NFR53/NFR54, lines 1445-1453.
- **Evidence:** The PRD defaults some audit-linked records to seven years and says deletion is impossible at the WORM layer; erasure is represented by projection tombstones and shredding a separate redaction key while the original envelope and hash-chain metadata remain. A6 still assumes this can satisfy GDPR, with no legal/DPO sign-off, subject-key granularity, backup/key-copy treatment, legal-hold rules, or proof that surviving envelope fields/hashes are non-personal.
- **Impact:** Architecture can lock in a retention/encryption topology that cannot honor an applicable erasure request without destroying tenant-wide reconstructability, or can over-redact evidence needed for legal obligations.
- **Suggested fix:** Obtain a data-protection decision before pilot onboarding. Define purpose and legal basis per data class, retention/hold precedence, envelope-key granularity, backup/key-destruction propagation, surviving metadata, and audit proof after crypto-erasure. Convert A6 into an approved decision or a release blocker.

### M2 — Material reductions from the approved brief are logged but not governed as a signed product-scope change; user upload silently disappears

- **Severity:** Medium
- **Location:** Product brief §Solution, lines 39-44, and §MVP Scope, lines 89-100; `prd.md` §MVP Scope, lines 201-228; §Growth Features, lines 298-309; `.decision-log.md` source-reconciliation entry, lines 115-121.
- **Evidence:** The brief includes generic email, user-upload file ingestion, scheduled/file-addition triggers, and concurrent UI/CLI/MCP capability. The PRD conditions generic email, defers triggers, and sequences machine surfaces; those reductions are at least noted. General user upload is neither an MVP requirement nor an explicit non-goal/growth item, and the decision log does not record its disposition. The May reconciliation records decisions but no named product approver or acceptance of the changed success thesis.
- **Impact:** Stakeholders can approve the brief believing upload and trigger value remains in MVP while delivery follows a narrower email-attachment product. Success metrics were rewritten around the reduction, so later comparison to the brief will be misleading.
- **Suggested fix:** Issue a signed scope amendment linking every brief commitment to retained/deferred/dropped status and rationale. Explicitly decide user upload. Update the brief or mark it superseded, and name the approving product owner/date for the reductions.

### M3 — “Final” metadata and source lineage are stale enough to mislead automated and human consumers

- **Severity:** Medium
- **Location:** `prd.md` frontmatter, lines 1-50, and §Project Classification source context, lines 94-98; `addendum.md` frontmatter, lines 1-7; workspace `.decision-log.md`; missing workspace `.memlog.md`.
- **Evidence:** The PRD and addendum say `updated: 2026-05-28`, while their bodies include approved June scope change and August recovery evidence. The sole direct input path is machine-specific (`D:/...`), source counts report zero project-context inputs, and the later claim to use multiple sibling planning artifacts pins neither path nor revision. The current workflow’s canonical `.memlog.md` is absent; later decisions live only in a legacy decision log.
- **Impact:** Downstream agents can treat stale `status: final` metadata as current approval, fail to load the real input, or miss later overriding decisions. Re-validation and source-fidelity checks are not reproducible.
- **Suggested fix:** Migrate/recover the decision history into the canonical memlog without discarding the legacy log; use repository-relative source paths plus commit/revision identifiers; update `inputs`, counts, `updated`, and edit history for every material change; and distinguish `final-planning` from `release-ratified` status.

### M4 — The operating-baseline catalog is published with missing targets and only one live signal

- **Severity:** Medium
- **Location:** `addendum.md` §Operating Baselines, lines 151-187 and line 203; `prd.md` §Increment M2, lines 273-283; NFR42a/NFR43, lines 1430-1431; A11, line 1350.
- **Evidence:** Multiple M2 SLO targets, error budgets, and alerts are `calibration-pending`, and the addendum says only audit projection lag currently has a live error-budget signal. NFR42a requires every listed SLO to have a target, window, budget, and alert threshold; M2 is described as the production-operability increment. No completed A11 baseline is in the source set.
- **Impact:** M2 can be declared complete with dashboards showing `unknown` and no defensible mailbox, duplicate, approval, or AI mediation budget. Operations cannot distinguish a healthy launch from an unmeasured one.
- **Suggested fix:** Turn A11 calibration into an M2 entry/exit gate. Require complete numeric catalog rows, live signal provenance, alert routing, and burn tests for every mandatory SLO; mark unavailable metrics explicitly unsupported and block the corresponding production claim.

### M5 — The ID-evolution contract invents an unowned cross-repository event dependency

- **Severity:** Medium
- **Location:** `addendum.md` §ID Evolution Contract, lines 112-123; `prd.md` §Integration Contracts, lines 871-875; §Context Ownership, lines 676-687.
- **Evidence:** The addendum requires every sibling context to emit a specific `IdentityEvolved` event covering rename/split/merge/deprecate, but the reviewed source chain does not establish that those repositories expose that contract. No producer owner, schema/version, ordering/idempotency rule, missed-event reconciliation, authorization semantics, or deployment compatibility requirement is named.
- **Impact:** ChatBot audit queries may silently resolve stale IDs or fail after a sibling migration. Architecture could force unauthorized changes into multiple repositories or build a consumer for events that do not exist.
- **Suggested fix:** Treat this as an explicit external dependency decision: verify each producer contract and owner, version the event, define ordering/replay/idempotency and a reconciliation query, and specify fallback behavior when a producer cannot support it. Do not call the addendum contract binding until counterpart teams accept it.

### M6 — “Single release” and “independently releasable increments” have no enforceable deployment/gate semantics

- **Severity:** Medium
- **Location:** `prd.md` frontmatter lines 37-40; §Minimum Release Slice, lines 230-235; §Project Scoping, lines 945-961; §Resource Risks, lines 1082-1088.
- **Evidence:** Frontmatter declares `single-release`; the body says M0, M1, and M2 are each releasable to a pilot cohort; M1 must be in production before M2 starts; and MVP completion requires M2. “Pilot,” “release,” “production,” and “MVP complete” are used as different gates without environments, entry/exit criteria, rollback rules, or who accepts each gate.
- **Impact:** Teams can ship an unsafe intermediate as “only a pilot,” or block all learning because “not MVP” is interpreted as not releasable. Recovery and isolation obligations may be applied to different milestones by different disciplines.
- **Suggested fix:** Define a release-state model: environment/audience for M0/M1/M2, mandatory safety gates, evidence owner, approval authority, rollback/disable conditions, and allowed claims. Reflect that model in frontmatter rather than relying on the overloaded `single-release` label.

## Gate recommendation

Do not approve this PRD as the current chain-top implementation contract. Resolve C1-C4 first, then H1-H9 before architecture/story generation is allowed to freeze interfaces or release gates. M1-M6 may be carried as explicit, owned pre-pilot decisions only if their owners and acceptance evidence are added to the canonical decision trail. A targeted re-validation should then verify exact allowlist membership, immutable approval boundaries, crash-atomic audit behavior, per-increment storage isolation, executable lifecycle transitions, integrated chat requirements, and fresh recovery evidence.
