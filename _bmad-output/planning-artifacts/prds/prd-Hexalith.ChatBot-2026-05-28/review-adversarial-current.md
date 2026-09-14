# Adversarial Release-Readiness Review — Hexalith.ChatBot PRD

## Verdict

**Not release-ready and not yet safe as the sole contract for UX, architecture, or story decomposition.** The PRD is unusually explicit about its current evidence gaps: `qualification-evidence.md` confirms that A5, A6, and A13 block M0/M1 and that A10 and A11 block M2. Those declared blocks are appropriate. The deeper problem is that the normative contract still contains four stop-ship ambiguities that could let different teams build or gate different systems: an AI-bound MCP client appears able to exercise a supposedly human-only approval operation, the canonical association state machine gives two incompatible ways out of `Deferred`, correction completion ignores several source-owned or irreversible consequences of a wrong association, and the release-status section treats A11 as M2-only even though the M1 gate depends on A11-defined measurements. Until these are resolved, even complete implementation evidence would not produce a defensible release decision.

## Critical

### C1 — An AI-bound MCP principal appears able to approve its own AI action

**PRD location:** §Measurable Outcomes → Cross-surface parity outcomes; §Service Client Permissions (`mcp-tool-client`); FR41, FR49, FR82–FR83; Glossary → Approval-required.

**Concrete note:** The singular M1 parity set includes `AI-action approval decision`. FR83 gives an authorized MCP client that same set, and the service-client table defines `mcp-tool-client` as `per-tenant + per-AI-actor` with the full parity set. By contrast, FR41 and FR49 require authorized **human** approval before boundary-crossing AI effects or outbound communication. The statement that MCP exposure does not extend the AI allowlist does not close this hole: the AI allowlist governs downstream executable commands, while `ApproveAIAction` is a ChatBot entry command in the parity set. A conforming implementation could therefore let an AI-bound MCP credential call the approval operation that unlocks its own proposal.

**Suggested fix:** Split MCP principals into closed classes. A human-delegated MCP session may submit approval commands only with current human authentication, recorded user presence, and `actorType=human`; an AI/tool MCP principal must be structurally denied `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, and any equivalent approval mutation. Add a non-self-approval invariant for every AI-originated proposal, reflect it in the RBAC and service-client tables, and add negative contract tests proving AI/service credentials cannot satisfy the human gate.

### C2 — The authoritative association state machine contradicts itself at `Deferred`

**PRD location:** §Shared Workflow Contract, especially the lifecycle summary and transition rows for `MarkEmailAssociationNeedsReview`, `ConfirmEmailProjectAssociation`, `RejectEmailProjectAssociation`, and `ResumeEmailAssociationReview`; FR6.

**Concrete note:** The lifecycle summary permits only `Deferred -> NeedsReview`. The `MarkEmailAssociationNeedsReview` row reinforces that “Only `ResumeEmailAssociationReview` may resume `Deferred`.” Yet the next two rows allow `ConfirmEmailProjectAssociation` and `RejectEmailProjectAssociation` directly from `NeedsReview or Deferred`. These paths produce different revision, evidence-refresh, and audit behavior. FR6 adds another broad user capability without resolving which path is legal. This is not editorial: UI buttons, CLI/MCP behavior, domain guards, and idempotency tests will differ depending on which statement a team follows.

**Suggested fix:** Choose one model and state it once. The safer model is `Deferred -> NeedsReview` only through `ResumeEmailAssociationReview` after the revisit condition and evidence refresh; confirmation/rejection then operate only on `NeedsReview`. Update the summary, transition table, FR6, surface behavior, error codes, and acceptance tests together.

### C3 — “Correction complete” does not account for source-owned data or irreversible effects

**PRD location:** UJ4; §Shared Workflow Contract (`CorrectEmailProjectAssociation` and acknowledgement rows); §Context Ownership; FR7, FR91a; NFR17a.

**Concrete note:** FR91a defines correction completion as acknowledgements from a list of ChatBot-derived stores. UJ4 additionally promises conversation relinking and attachment move/relink, while §Context Ownership assigns those records to Conversations and Folders. The contract does not define commands, acknowledgements, or failure/compensation rules for those owner contexts. It also does not disposition already approved/executed AI actions, appended messages, sent outbound mail, task-intent conversions, or file disclosures caused by the wrong association. A correction can therefore reach `Corrected` while authoritative content remains linked to the wrong Project or an irreversible consequence remains unacknowledged. That defeats the product’s central trust claim.

**Suggested fix:** Define a correction impact manifest at correction start, covering every source owner and every derived or effectful consequence. Require current authority on both source and destination Projects, owner-specific reassignment/compensation commands, immutable acknowledgement records, and an explicit outcome for irreversible effects (`contained`, `compensation-required`, or `cannot-repair`). `Corrected` must require completion of the manifest; otherwise remain in a typed blocked/incident state. Add Conversations and Folders producer acceptance to A13 where their contracts are required for M0.

### C4 — M1 can be declared passed while one of its mandatory metric gates is undefined

**PRD location:** §Current Release Status; §Measurable Outcomes (SM8, SM16, SM-C3, SM-C5); M1 gate row; A11; `qualification-evidence.md` §Current gate state.

**Concrete note:** Current Release Status names A11 only as an additional M2 blocker. The M1 gate nevertheless requires SM8, SM12, SM15, SM16, SM-C3, and SM-C5. SM16 is explicitly a starter target under A11, and SM-C5 cannot be evaluated until the A11 baseline fixes the supported-request mix. `qualification-evidence.md` says A11 calibration is incomplete. The document therefore permits two incompatible readings: M1 is blocked because its mandatory measures are not defined, or M1 may pass using provisional values despite the supported-request denominator being absent.

**Suggested fix:** Split A11 into increment-specific decisions. Create an A11-M1 gate that freezes every M1 metric definition, denominator, supported-request mix, target, evidence source, owner, and pass/fail rule before the M1 observation window begins. Reserve A11-M2 for the SLO catalog. Add A11-M1 to Current Release Status and the M1 gate, or explicitly remove the provisional measures from the M1 pass/fail set and replace them with defined qualification criteria.

## High

### H1 — “Current approval” for A5/A6/A13 has no executable validity contract

**PRD location:** §Current Release Status; increment gate table; A5, A6, A13; `qualification-evidence.md` §Required pre-pilot evidence.

**Concrete note:** The gate table depends on approvals being “current,” “expired,” or “invalidated,” but the normative PRD does not define a common gate record, expiry calculation, signer independence, revocation mechanism, or machine-evaluable closure state for A5, A6, or A13. The evidence file asks for expiry/reopen rules but does not itself define them. This makes closure partly ceremonial: the same attachment could be called current by one approver and stale by another.

**Suggested fix:** Add a normative gate-record schema with gate ID, decision, exact candidate and dependency revisions, evidence locators and hashes, owner and independent approver identities, approved environment/profile, approval and expiry timestamps, reopen predicates, superseded decision, and computed status. Require the release gate to consume those records rather than prose or the presence of files.

### H2 — Policy snapshots are simultaneously an M0 dependency and an M1-owned record

**PRD location:** M0 scope and gate; §Shared Workflow Contract → Tenant policy; §Data Governance Surface → Policy snapshot row; Tenant Policy Schema.

**Concrete note:** M0 requires creation of the “M0 policy snapshot,” applies M0 threshold/authenticity/retention knobs, and requires policy references in atomic mutation envelopes. The Data Governance Surface assigns the `Policy snapshot` record class to M1. This changes whether M0 must persist, isolate, retain, export, erase, and test the record, and it directly affects the M0 native-store isolation gate.

**Suggested fix:** Make policy snapshot an M0 record class and enumerate which M0 knobs it contains. Require its A6 retention/export/erasure treatment and native-store isolation proof in M0; let M1 add the editor and the M1-only rows without changing ownership.

### H3 — Indeterminate risk classification both creates an approval path and forbids the proposal write

**PRD location:** `addendum.md` §Risk Classifier → Misclassification fallback; §Retry Profiles → Task-intent and risk classification; NFR15a → AI action proposal.

**Concrete note:** The Risk Classifier says an indeterminate result is treated as `approval-required` and fails closed “to review.” The retry profile likewise names an `approval-required` safe state. NFR15a, however, lists indeterminate risk as a fail-closed condition for the proposal record, and its fail-closed definition writes no durable state. There is then no proposal for a reviewer to inspect or approve. Teams cannot tell whether indeterminate classification yields a durable review item, a typed terminal denial, or no record beyond an attempt audit.

**Suggested fix:** Define a distinct durable `ClassificationReviewRequired` workflow that cannot execute and is not an `AwaitingApproval` proposal, or define indeterminate as a typed denial with no proposal. Give it an owner, command, states, retry/remediation path, audit type, and surface behavior. Use the same rule in the classifier, retry table, and NFR15a.

### H4 — Approval freshness is referenced but never defined

**PRD location:** §Shared Workflow Contract → AI action execution; FR42, FR50; NFR16, NFR36, NFR48; Tenant Policy Schema.

**Concrete note:** Execution rejects an “expired approval,” and NFR16 requires approval state to be revalidated, but no approval TTL, expiry state/event, tenant-policy knob, or renewal rule exists. Evidence freshness is defined, but approval freshness is not. A frozen proposal could remain approved indefinitely despite model, policy, recipient, file-retention, sender-authority, or risk-contract changes, with implementations inventing different expiry behavior.

**Suggested fix:** Add a closed approval lifetime contract: `expires_at`, default/max TTL by effect class, `Approved -> Expired`, execution-time checks for proposal/content/resource digests and current policy/authority, and a new linked proposal/approval after expiry or material drift. Add the knob to the policy schema only if tenants may choose within safe bounds.

### H5 — Association quality has conflicting evaluation populations and a gameable headline metric

**PRD location:** SM1, SM7, SM-C1; A9a; `addendum.md` §Confidence Thresholds → Calibration protocol.

**Concrete note:** SM7 defines recall for “non-ambiguous messages,” while the normative addendum defines recall “across the ambiguous + auto-associated set.” Those populations can yield different gate outcomes. SM1 counts either a correct association **or safe no-association** as correct across all adjudicated messages, so a system can improve the headline result by routing most difficult cases to no-association. A9a sets only total corpus size and named partitions, not minimum per-partition counts, prevalence bounds, adjudication protocol, or confidence intervals.

**Suggested fix:** Define one versioned evaluation protocol with mutually exclusive populations, minimum class counts, sampling/prevalence rules, ground-truth adjudication and disagreement handling, exact formulas, and confidence bounds. Separate “correct association,” “safe abstention,” and “wrong association”; do not combine correctness and abstention in SM1. Make the addendum and SM7 use the same recall population.

### H6 — Data-subject and legal-hold workflows cannot be operated end to end from the published contract

**PRD location:** §Shared Workflow Contract → Data export, Data erasure, Legal hold, Retention disposition; §Command and Query Contracts; FR58; §UI Surface Inventory; Functional Acceptance Guidance FR55–FR63.

**Concrete note:** M0+ has mutation commands for export, erasure, legal hold, and retention, and A6 requires them before pilot data. Yet the query catalog has no get/list/status/result contracts for these workflows, the UI inventory has no M0/M1 surface for them, and the RBAC language uses the undefined actor label “authorized data-subject operator.” The acceptance table promises policy-snapshot retrieval and export/delete support without corresponding queries. This leaves stories unable to define who initiates, tracks, downloads, rejects, or appeals a request.

**Suggested fix:** Add the actor/authority mapping, query contracts, response/redaction semantics, expiring result access, status and appeal/rejection paths, and the minimum pre-pilot operator surface (which may be a restricted API/CLI if explicitly declared). Trace each workflow to A6 evidence and acceptance scenarios.

### H7 — Unknown outbound-send outcome is a dead-end outside the authoritative state model

**PRD location:** §Shared Workflow Contract → Outbound email; `addendum.md` §Retry Profiles → Outbound send; FR47–FR50; FR65.

**Concrete note:** The outbound workflow permits only `Sent` or `Failed`. The retry profile introduces `investigation-required` for an unknown-send outcome and says retry is blocked until provider reconciliation proves no send. There is no such state, reconciliation command/query, evidence contract, timeout, or authorized resolver in the operation catalog. An operator cannot safely decide whether to send again, and the system cannot express permanent uncertainty without overloading `Failed`.

**Suggested fix:** Add `SendOutcomeUnknown` and `Reconciling` states, a provider-reconciliation query/command, stable provider-message evidence, ownership and timeout/escalation rules, and terminal outcomes (`Sent`, `NotSent`, `Unresolved`). Allow a new draft only after an auditable `NotSent` result.

### H8 — Untrusted email/attachment content is not governed as an AI instruction source

**PRD location:** §Key Product Risks; System Journey; FR27, FR33, FR39–FR46; NFR8–NFR10, NFR21; A5.

**Concrete note:** The PRD controls authorization, malware, model-provider reuse, command allowlists, and human approval, but never requires externally supplied email, attachment text, quoted thread content, or retrieved project material to be treated as untrusted data rather than agent instructions. Prompt-injection and tool-instruction attacks can therefore manipulate summaries, risk explanations, context selection, or proposals before the human gate. The current A5 evidence list is provider-governance oriented and does not close this product-level input-trust gap.

**Suggested fix:** Add a content-origin/trust contract: external and retrieved content cannot define system/tool policy; instruction/data boundaries remain explicit through extraction; context and output carry origin labels; suspicious instruction patterns trigger safe review; model output never supplies authority; and adversarial prompt-injection fixtures cover email bodies, quoted threads, attachments, filenames, and tool results. Include these controls in A5 and the AI-action acceptance matrix.

### H9 — The “single command spine” applies AI-only stages to every mutation

**PRD location:** FR81a; `addendum.md` §Shared Command Pipeline.

**Concrete note:** Both normative statements say every state mutation runs action-risk classification and approval validation. Those stages are defined only for AI action requests, yet the same pipeline processes mailbox capture, attachment outcomes, policy changes, legal holds, retention, notifications, and projection operations. Taken literally, those operations require an AI risk classification and approval; treated as optional, adapters or handlers may decide which controls to skip, weakening the claimed structural parity.

**Suggested fix:** Define a typed admission-stage matrix by operation/effect class. Authentication, tenant binding, authorization, idempotency, concurrency, and atomic audit can be universal; risk classification and proposal approval apply only to declared AI-mediated effect classes, while admin separation-of-duty, retention authority, mailbox authenticity, and other guards apply to their own classes. Require the central pipeline—not adapters—to select the closed profile.

### H10 — Microsoft 365 sender authority conflates API scope with mailbox delegation

**PRD location:** §Microsoft 365 / Exchange Permission Constraints; `addendum.md` §Inbound Message Authenticity → Authority class mapping.

**Concrete note:** The narrative says mailbox read, delegated access, send-as, and send-on-behalf must be distinguished. The authority table then reduces `shared-mailbox send` to membership plus `Mail.Send`, and maps send-on-behalf to `Mail.Send.Shared or send-on-behalf`. That does not define the required intersection of token/application permission, mailbox resource/delegation grant, current membership, sender identity, and ChatBot authorization. Different adapters can accept materially different authority evidence while claiming conformance.

**Suggested fix:** Define a provider-neutral authority tuple and a versioned M365 adapter mapping for each class: token subject/client, OAuth permission mode, target mailbox, mailbox ACL/delegation grant, current membership, send-as versus send-on-behalf semantics, tenant, evidence timestamp, and revalidation result. Require every element plus ChatBot policy; any mismatch is a typed denial.

### H11 — Authenticity “block” has no canonical workflow outcome

**PRD location:** FR48a–FR48d; Tenant Policy Schema `mailbox.authenticity-strictness`; `addendum.md` §Inbound Message Authenticity; §Shared Workflow Contract association transitions.

**Concrete note:** `strict` routes anomalies to `NeedsReview`, while `paranoid` “blocks” them. The association state model has no authenticity-blocked destination, reason-to-state mapping, release/review path, or retention rule. `Failed`, `Rejected`, `Skipped`, participant `Quarantined`, and safety-control `quarantined` all have different meanings. An implementation can silently discard, preserve, or expose the message and still claim it was blocked.

**Suggested fix:** Add a canonical intake/authenticity state family (for example `AuthenticityReviewRequired`, `AuthenticityBlocked`, `Released`, `Rejected`) with commands, actors, evidence visibility, retention, terminality, and reprocessing rules. Bind `strict` and `paranoid` to exact transitions before association candidate generation.

## Medium

### M1 — Audit reconstructability can be measured from the artifact whose omissions it is meant to detect

**PRD location:** M2 scope; NFR50a; §Audit Requirements.

**Concrete note:** The 99.5% measure is “operations whose audit chain reconstructs” divided by operations, but the independent source of the denominator, eligibility window, treatment of in-flight operations, and reconciliation procedure are not defined. If both numerator and denominator come from the audit projection, omitted operations disappear from both and the metric can report perfect completeness. Atomic mutation-envelope presence is separate and does not by itself prove end-to-end projection reconstruction.

**Suggested fix:** Define the denominator from an independent canonical command/event index, join by stable operation ID, specify in-flight grace and exclusions, and publish missing/orphan/duplicate-chain counts separately. Require a reconciliation test that deliberately drops projection records and proves the measure degrades.

### M2 — “Rubber-stamp approval rate” is not a measurable counter-metric

**PRD location:** SM-C3; NFR46.

**Concrete note:** The PRD sets a 15% threshold but never defines what observation classifies an approval as rubber-stamped, the denominator, minimum sample size, treatment of fast legitimate approvals, or who adjudicates it. Teams could infer it from elapsed time, from later reversals, or from reviewer survey data and obtain incompatible results.

**Suggested fix:** Define the signal and formula, minimum sample, excluded low-complexity cases, false-positive review, and owner. If no reliable behavioral definition exists, replace the pass/fail percentage with observable proxies such as preview engagement, evidence opens, decision-time distribution, reversal/incident rate, and sampled reviewer audit.

### M3 — NFR41 contains an escape hatch that defeats its five-minute obligation

**PRD location:** NFR41; `qualification-evidence.md` Recovery-validation commitments.

**Concrete note:** NFR41 requires incident scope within five minutes only “when monitoring is available.” Monitoring is itself an operability requirement, so an unavailable monitor can exempt the system from the obligation it is supposed to verify. The qualification evidence correctly marks coverage unmeasurable, but the normative loophole remains.

**Suggested fix:** Make monitoring availability a prerequisite for the affected increment and define detection time from a named signal. If a dependency cannot be monitored, mark its scope-time SLO unsupported and block the corresponding claim rather than waive the target.

### M4 — Threshold-change wording conflicts with the schema’s hard range

**PRD location:** `addendum.md` §Confidence Thresholds → Guardrail on threshold changes; Tenant Policy Schema `association.t-high` and `association.t-low`.

**Concrete note:** The guardrail says M0 cannot lower thresholds below 0.80/0.50 “without a corresponding documented evaluation run,” implying an evaluation can authorize a lower value. The schema declares those values as hard lower bounds and says an invalid snapshot is rejected. This creates two legal value domains.

**Suggested fix:** State either that the lower bounds are absolute for the version or define a version-bump/exception mechanism with stricter approval. Do not imply that an evaluation run can bypass a schema range.

### M5 — FR6 grants a user transition that the command matrix reserves to a worker

**PRD location:** FR6; §Shared Workflow Contract rows for `MarkEmailAssociationNeedsReview` and `ProposeEmailProjectAssociation`.

**Concrete note:** FR6 says authorized users can “mark an item as needing review.” The authoritative transition table exposes `MarkEmailAssociationNeedsReview` only to a worker from `Received`; human actors confirm, reject, defer, or resume. There is no user command or legal source state for the FR6 action.

**Suggested fix:** Remove the user capability from FR6 or add a specifically authorized user escalation command with source states, guards, reason, audit event, and parity exposure.

### M6 — Rejected policy changes are called canonical mutation envelopes

**PRD location:** Tenant Policy Schema final paragraph; NFR15a; NFR50.

**Concrete note:** The addendum says all successful and rejected policy changes produce canonical audit envelopes. NFR15a says failed validation writes no durable policy/idempotency state, while NFR50 distinguishes mutation envelopes from non-mutating attempt records. Calling both “canonical” blurs the invariant and could inflate mutation completeness with rejected attempts.

**Suggested fix:** Reserve `canonical mutation envelope` for committed mutations and require a separately typed `PolicyMutationRejectedAttempt` record for rejected requests. Define separate completeness measures and never count the attempt as a mutation.

### M7 — Security-attempt auditing has no completeness or abuse-control contract

**PRD location:** Technical Success; FR55; NFR50 and NFR50a.

**Concrete note:** Denials, restricted reads, and service-client failures “must” be audited, but only field presence in a sampled dataset is tested; NFR50a’s 100% invariant covers durable mutations, not attempts. No delivery-loss measure, overload behavior, deduplication, rate protection, or retention rule is specified for an attacker generating large numbers of denials. The separate path can silently lose the incidents it exists to show or become an audit-volume denial of service.

**Suggested fix:** Define an attempt-event durability and overload contract, independent loss/lag metrics, bounded deduplication that preserves counts and exemplars, retention/redaction, tenant rate isolation, and alerting. State whether a security-sensitive request fails closed if the attempt path is unavailable.

### M8 — Tenant checkpoint evidence cannot prove the claimed tamper boundary without cadence and coverage rules

**PRD location:** M2 scope; NFR49a; `addendum.md` §Shared Command Pipeline → Audit ledger topology and concurrency.

**Concrete note:** Signed per-tenant checkpoints “periodically” anchor aggregate heads, but no maximum cadence, signer/key authority, aggregate inclusion inventory, missing-stream detection, verification window, or checkpoint recovery rule is specified. The five-minute alert begins only after a verification failure, leaving the mechanism that must detect omission undefined.

**Suggested fix:** Define checkpoint cadence and maximum lag, canonical inventory/coverage, signing key ownership and rotation, verification schedule, loss/rebuild rules, and exact alerts/SLO rows. Test deletion of a complete aggregate stream, not only fork/reordering within a known stream.

### M9 — “Where isolation is technically possible” makes noisy-neighbor protection optional

**PRD location:** NFR30.

**Concrete note:** The clause allows an implementation to declare isolation technically impossible after architecture is chosen. No shared-resource saturation budget, fairness measure, or accepted exception process exists, so the requirement cannot gate capacity readiness.

**Suggested fix:** Name the resources that must be isolated, define per-tenant/mailbox fairness and saturation measures, and require Architecture plus Operations to approve any bounded exception with blast radius, mitigation, expiry, and load-test evidence.

### M10 — NFR70 cannot literally apply to queries and other non-mutating external operations

**PRD location:** NFR70; §Command and Query Contracts.

**Concrete note:** NFR70 requires every externally visible operation to define a state transition and audit event. Most queries should have neither a domain state transition nor a mutation envelope; only security-sensitive reads are necessarily audited. Treating the requirement literally creates fake transitions or excessive sensitive read logs, while treating it loosely gives reviewers no closed coverage rule.

**Suggested fix:** Split the rule: mutations require transition, canonical mutation envelope, and idempotency; queries require response/redaction/freshness/error semantics, and only named sensitive query classes require an auditable-attempt/read-access record.

## Low

### L1 — `Correcting` is both a sub-state of a terminal state and a separate transition destination

**PRD location:** §Shared Workflow Contract state definitions and transition table; §Association Lifecycle and States.

**Concrete note:** `Corrected` is described as terminal, `Correcting` as its sub-state, and the table transitions `Associated -> Correcting -> Corrected`. The shorthand lifecycle states omit both `Correcting` and `Correction-delayed`. Implementers can model nesting, separate states, or flags differently.

**Suggested fix:** Publish one explicit state representation. Prefer separate non-terminal states in the canonical enum, with `Corrected` alone terminal, and include them in the lifecycle list.

### L2 — M0 is described as human-driven despite automatic association

**PRD location:** Increment M0 opening paragraph; M0 association scope; §Shared Workflow Contract.

**Concrete note:** M0 says “humans driving every decision point,” but `AssociateEmailToProject` automatically moves `Received` to `Associated` when threshold and deterministic evidence pass. The prose can mislead pilot consent, UX, and test expectations.

**Suggested fix:** Say humans drive every ambiguous association and every risky AI action decision, while qualified deterministic association is automatic and auditable.

### L3 — The project’s “medium” complexity label understates its own governance surface

**PRD location:** frontmatter classification; §Project Classification.

**Concrete note:** The release spans multi-tenant authorization, external email authenticity, AI mediation, cross-context atomic audit, GDPR data operations, machine surfaces, replay isolation, tamper evidence, and disaster recovery, with five declared release blockers. Labeling this `medium` can bias staffing, assurance, and schedule decisions even though the rest of the PRD treats it as chain-top work.

**Suggested fix:** Reclassify complexity as high/enterprise-critical or explicitly state that the label is only product-domain complexity and does not drive assurance or staffing.

### L4 — NFR42a says the addendum will be created during M2 although it already governs the PRD

**PRD location:** NFR42a; `addendum.md` §Operating Baselines.

**Concrete note:** NFR42a refers to the Operating Baselines appendix as “created during M2,” while the approved normative addendum already contains it as a backlog. The addendum correctly distinguishes backlog from publishable catalog, but the stale wording weakens artifact-state clarity.

**Suggested fix:** Replace “created during M2” with “promoted from qualification backlog to a candidate-bound published SLO catalog during M2 after A11 passes.”
