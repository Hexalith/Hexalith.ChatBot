# Validation Report — Hexalith.ChatBot

- **PRD:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- **Addendum:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- **Rubric:** `/home/administrator/projects/hexalith/chatbot/.agents/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-09-14T15:32:19+02:00
- **Grade:** Poor
- **Gate:** STOP — resolve the four critical contract contradictions before product approval or unqualified downstream handoff
- **Reviewer reports:** 4 Critical · 15 High · 16 Medium · 8 Low (43 total before deduplication)
- **Consolidated findings:** 4 Critical · 11 High · 14 Medium · 7 Low (36 unique, highest severity retained)

## Overall verdict

This is an adequate, unusually rigorous chain-top PRD: the product thesis, release gates, authority boundaries, success measures, and safety invariants are strong enough to guide decisions, and the document is candid that the artifact is final while M0/M1/M2 remain evidence-gated. It is not yet a clean implementation oracle, however: correction of already-stored attachments and the pre-pilot data-rights workflows lack complete actor/owner/surface contracts, while smaller state-machine and metric gaps would force downstream teams to invent behavior.

The adversarial and source-integrity passes materially lower the release verdict. They found four normative contradictions that could yield different conforming systems: an AI-bound MCP principal can appear to approve its own proposal; `Deferred` has incompatible legal transitions; correction completion omits source-owned and irreversible effects; and M1 depends on A11 measurements that the release-status contract labels M2-only. The source lineage is otherwise strong and the declared evidence gates are honest, but these contradictions make the PRD unsafe as the sole contract for UX, architecture, stories, or release approval until corrected.

## Dimension verdicts

- Decision-readiness — strong
- Substance over theater — strong
- Strategic coherence — adequate
- Done-ness clarity — adequate
- Scope honesty — strong
- Downstream usability — adequate
- Shape fit — adequate

## Findings by severity

### Critical (4)

**[Adversarial] — An AI-bound MCP principal appears able to approve its own AI action (§Measurable Outcomes; §Service Client Permissions; FR41, FR49, FR82–FR83)**

The M1 parity set includes AI-action approval decisions, FR83 grants that set to MCP clients, and `mcp-tool-client` is bound per AI actor. The AI allowlist restriction does not structurally deny the ChatBot approval command, despite FR41/FR49 requiring human approval.

Fix: split human-delegated and AI/tool MCP principals; structurally deny approval mutations to AI/service identities, require current human presence and `actorType=human`, add a non-self-approval invariant, and prove it with negative contract tests.

**[Adversarial] — The authoritative association state machine contradicts itself at `Deferred` (§Shared Workflow Contract; FR6)**

The lifecycle summary and `MarkEmailAssociationNeedsReview` say only `ResumeEmailAssociationReview` may leave `Deferred`, while confirm/reject rows allow direct transitions from `Deferred`. UI, command guards, evidence refresh, and idempotency behavior therefore depend on which normative sentence is chosen.

Fix: select one legal path—preferably `Deferred -> NeedsReview` through resume, then confirm/reject—and update the summary, transition matrix, FR6, errors, surfaces, and acceptance tests together.

**[Adversarial + Rubric] — Correction completion omits source-owned data and irreversible effects (UJ4; §Shared Workflow Contract; §Context Ownership; FR7, FR91a; NFR17a)**

`Corrected` currently depends on ChatBot-derived-store acknowledgements, while UJ4 promises conversation and attachment remediation owned by Conversations and Folders. The contract also does not disposition already executed actions, sent mail, appended messages, converted intents, or file disclosures caused by the wrong association.

Fix: define a correction impact manifest covering every owner and effect, require authority on source and destination Projects, define owner-specific reassignment/compensation acknowledgements, and keep the workflow blocked until every repair or irreversible-effect disposition is recorded. Add required sibling producer acceptance to A13.

**[Adversarial] — M1 depends on undefined A11 measurements while A11 is labeled M2-only (§Current Release Status; SM8, SM16, SM-C3, SM-C5; M1 gate; A11)**

M1 must pass metrics whose denominator or target remains provisional under A11, but Current Release Status names A11 only as an M2 blocker. The PRD therefore permits both “M1 blocked” and “M1 may pass provisionally.”

Fix: split A11 into increment-specific decisions, freeze all M1 metric definitions and denominators before its observation window, and add A11-M1 to the release status and gate—or replace those measures with fully defined qualification criteria.

### High (11)

**[Adversarial] — Gate approval freshness is not executable (§Current Release Status; increment gates; A5, A6, A13)**

“Current,” “expired,” and “invalidated” approvals lack one normative record schema, expiry calculation, signer-independence rule, revocation method, and computed closure state.

Fix: define a machine-evaluable gate record bound to candidate/dependency revisions, evidence hashes, approvers, timestamps, expiry, reopen predicates, supersession, and status.

**[Adversarial + Source integrity] — Policy snapshots are both an M0 dependency and an M1-owned record (§Increment M0; §Shared Workflow Contract; §Data Governance Surface; FR61; NFR15a)**

M0 creates and consumes an immutable policy snapshot, yet the durable-record ownership table assigns Policy snapshot to M1. This changes M0 isolation, retention, export/erasure, and story sequencing.

Fix: assign the base record to M0 and let M1 add administration capabilities, or define two distinct record classes and map both through governance and workflow contracts.

**[Adversarial + Source integrity] — Indeterminate risk classification has incompatible durable outcomes (addendum §Risk Classifier; §Shared Workflow Contract; NFR15a)**

The classifier routes indeterminate results to approval review, which requires a durable proposal, while NFR15a and the glossary require fail-closed behavior with no durable state.

Fix: choose either a distinct durable, non-approvable classification-review state or a typed no-write denial, then align commands, events, retry/remediation, audit, and surface behavior.

**[Adversarial] — Approval freshness is referenced but undefined (§Shared Workflow Contract; FR42, FR50; NFR16, NFR36, NFR48)**

Execution rejects an expired approval, but no TTL, expiry event/state, renewal rule, or drift invalidation contract exists.

Fix: define approval lifetime by effect class, `Approved -> Expired`, digest and policy/authority rechecks, and linked reapproval after expiry or material drift.

**[Adversarial] — Association-quality gates use conflicting populations and a gameable headline metric (SM1, SM7, SM-C1; A9a; addendum §Confidence Thresholds)**

SM7 and the addendum disagree about the recall population, while SM1 combines correct association and safe abstention so aggressive abstention can inflate success.

Fix: publish one versioned evaluation protocol with separate association, abstention, and wrong-association measures; define sampling, class minima, adjudication, formulas, and confidence bounds.

**[Rubric + Adversarial] — Pre-pilot data-rights workflows lack actors, queries, and a delivery surface (§Shared Workflow Contract; §RBAC Matrix; §UI Surface Inventory; FR58)**

Export, erasure, legal hold, and retention are M0+ prerequisites, but the “authorized data-subject operator” is unmapped, no M0/M1 surface is named, and status/result query contracts are missing.

Fix: define initiating/reviewing roles, owner authority, status/result/redaction queries, result delivery, partial completion, hold precedence, retry and appeal behavior, plus an explicit pre-pilot API/CLI/admin surface and journey or acceptance contract.

**[Adversarial] — Unknown outbound-send outcome has no canonical workflow (§Outbound email; addendum §Retry Profiles; FR47–FR50, FR65)**

The retry contract introduces `investigation-required`, but the state model has only Sent/Failed and provides no reconciliation operation or resolver.

Fix: add unknown/reconciling states, provider-evidence contracts, authorized reconciliation operations, timeout/escalation rules, and terminal Sent/NotSent/Unresolved outcomes.

**[Adversarial] — Untrusted email and attachment content is not governed as an AI instruction source (§Key Product Risks; System Journey; FR27, FR33, FR39–FR46; A5)**

Authorization and malware controls exist, but prompt-injection and instruction/data-boundary rules for external and retrieved content do not.

Fix: define content-origin labeling, immutable instruction authority, suspicious-content review behavior, and adversarial fixtures for bodies, threads, attachments, filenames, retrieved context, and tool results; include them in A5.

**[Adversarial] — The universal command spine applies AI-only stages to every mutation (FR81a; addendum §Shared Command Pipeline)**

Literal application forces risk classification and approval onto mailbox, policy, legal-hold, notification, and projection operations; loose interpretation lets handlers choose their own controls.

Fix: define a closed admission-stage matrix per operation/effect class, keeping universal controls central while applying risk/approval and domain-specific guards only to declared classes.

**[Adversarial] — Microsoft 365 sender authority conflates API permission with mailbox delegation (§Microsoft 365 / Exchange Permission Constraints; addendum §Authority class mapping)**

The mapping does not require the full intersection of token/client permission, mailbox ACL/delegation, membership, sender identity, tenant, and ChatBot authorization.

Fix: define a provider-neutral evidence tuple and versioned M365 mapping for every authority class; any missing or stale element must produce typed denial.

**[Adversarial] — Authenticity `block` has no canonical intake outcome (FR48a–FR48d; Tenant Policy Schema; addendum §Inbound Message Authenticity)**

`strict` routes anomalies to review while `paranoid` “blocks,” but the workflow defines no blocked state, retention rule, release path, or reason mapping.

Fix: add an authenticity state family with exact transitions, actors, visibility, retention, terminality, and reprocessing rules before association begins.

### Medium (14)

**[Rubric] — M1 machine-surface success proves one use, not business value (§Business Success; SM15; §Risk Mitigation Strategy).** A single CLI/MCP call can pass. Fix: require repeated governed use over a defined window, a benefit measure, and a maintenance/governance counter-metric.

**[Rubric + Adversarial] — FR6 grants a user transition reserved to a worker (FR6; §Shared Workflow Contract).** Fix: remove the user capability or add an authorized user-escalation command with states, guards, reason, event, and parity exposure.

**[Adversarial] — Audit reconstructability can use the artifact whose omissions it should detect (M2; NFR50a; §Audit Requirements).** Fix: derive the denominator from an independent command/event index and test that deliberate projection loss degrades the metric.

**[Adversarial] — Rubber-stamp approval rate is undefined (SM-C3; NFR46).** Fix: define classification signal, formula, denominator, sample size, exclusions, adjudication, and owner—or replace it with observable proxies.

**[Adversarial] — NFR41 waives its five-minute incident-scope obligation when monitoring is unavailable.** Fix: make monitoring a prerequisite; an unsupported signal blocks the claim rather than waiving the target.

**[Adversarial] — Threshold-change wording conflicts with schema hard ranges (addendum §Confidence Thresholds; Tenant Policy Schema).** Fix: make bounds absolute for the version or define an explicit versioned exception mechanism.

**[Adversarial] — Rejected policy changes are mislabeled canonical mutation envelopes (Tenant Policy Schema; NFR15a; NFR50).** Fix: reserve mutation envelopes for commits and use a separately typed rejected-attempt record and metric.

**[Adversarial] — Security-attempt auditing lacks completeness and overload contracts (Technical Success; FR55; NFR50/NFR50a).** Fix: define attempt-event durability, independent loss/lag measures, bounded deduplication, tenant isolation, retention/redaction, alerting, and fail-closed behavior.

**[Adversarial] — Tenant checkpoints cannot prove the tamper boundary without cadence and coverage (§M2; NFR49a; addendum §Audit ledger topology).** Fix: define maximum cadence, canonical inventory, signer/key ownership, verification, loss/rebuild, and deletion tests.

**[Adversarial] — Noisy-neighbor protection is optional under “where technically possible” (NFR30).** Fix: name isolated resources and fairness/saturation measures and require time-bounded Architecture/Operations exceptions.

**[Adversarial] — NFR70 cannot literally apply state transitions to queries (§Command and Query Contracts).** Fix: separate mutation, query, and sensitive-read contract obligations.

**[Source integrity] — The `current` source manifest trails the live checkout (`source-manifest.md`; §Project Classification).** Inspected deltas do not presently change consumed contracts, but three dependency advances are undisclosed. Fix: refresh current identities or mark the file as a reviewed snapshot and add a current-checkout recheck record.

**[Source integrity] — Governed-chat response vocabulary does not round-trip to canonical states (FR28b; §Shared Workflow Contract; NFR32).** Fix: use canonical names or define an explicit response-to-state mapping, especially for `needs-review`.

**[Source integrity] — “Project Association context” is absent from the ownership map (§Technical Architecture Considerations; §Context Ownership).** Fix: declare it a ChatBot subcontext/capability or add a formal bounded-context ownership row and API/deployment boundary.

### Low (7)

**[Rubric] — Assumptions Index roundtrip is incomplete.** A4, A7, and A12 appear only in the index. Fix: cite them inline at operative statements or reclassify them as standalone decisions.

**[Rubric] — `Approval` glossary wording includes cancellation and denial despite distinct workflow concepts.** Fix: define the review decision family and cancellation/denial separately.

**[Rubric] — The addendum attributes contract tests to FR82–FR86 although only FR86 defines them.** Fix: cite FR86 or call the range parity requirements.

**[Adversarial] — M0 is called human-driven despite automatic qualified association.** Fix: say humans drive ambiguous association and risky AI decisions while qualified deterministic association is automatic and audited.

**[Adversarial] — The `medium` complexity label understates the governance and assurance surface.** Fix: classify as high/enterprise-critical or state that the label excludes assurance and staffing implications.

**[Adversarial] — NFR42a says the Operating Baselines appendix will be created during M2 although it already exists.** Fix: say it is promoted from backlog to a candidate-bound published catalog after A11 passes.

**[Source integrity] — The four-job recovery gate does not locate all four job identities (A10; addendum §Recovery Qualification; `qualification-evidence.md`).** Fix: list the four stable IDs and governing repository/policy locator in the mutable evidence artifact.

## Mechanical notes

- Base FR/NFR/SM IDs are contiguous and unique; letter-suffixed additions are unique.
- Every explicit FR, NFR, and A-token reference resolves; all 15 A11 metric rows pair one-to-one between normative targets and qualification evidence.
- Every named human journey has a named protagonist; the System Journey is appropriately separate.
- A4, A7, and A12 do not round-trip from the Assumptions Index to inline operative statements.
- Association-state shorthand omits `Correcting` and `Correction-delayed`; this is consolidated into Critical C2.
- The direct product brief and Epic 12 architecture hashes match the manifest; the latter remains correctly marked `activation: pending`.

## Reviewer files

- `review-rubric.md` — primary seven-dimension rubric
- `review-adversarial-current.md` — adversarial release-readiness
- `review-source-integrity-current.md` — source and downstream integrity
- `orient-validation-current.md` — validation source extract
