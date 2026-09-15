---
title: Recommended PRD Update Reconciliation — 2026-09-14
status: extracted
created: "2026-09-14"
source_bundle:
  - validation-report.md
  - review-rubric.md
  - review-adversarial-current.md
  - review-source-integrity-current.md
target_artifacts:
  - prd.md
  - addendum.md
  - .memlog.md
---

# Recommended PRD Update Reconciliation — 2026-09-14

## Verdict

**STOP remains the correct gate.** The PRD is strategically strong and candid about its evidence gates, but four normative contradictions still permit materially different conforming systems. Apply the deduplicated **4 Critical + 11 High** remediation set before returning the artifact to final status or handing it downstream without qualification. The Medium/Low tail should remain a named follow-up ledger unless an adjacent critical/high edit can close an item without adding scope.

This extraction does not modify `prd.md`, `addendum.md`, or `.memlog.md`. It reconciles the current reviewers' recommendations against those artifacts and identifies the one recommendation that changes a recorded release-gate decision.

## Source and decision-history reconciliation

The latest reports agree on the following strengths and constraints:

- The product thesis, ownership boundaries, stable identifiers, fail-closed posture, atomic audit intent, and declared A5/A6/A10/A11/A13 evidence gaps remain valid.
- The direct product and Epic 12 architecture inputs retain matching hashes, and Epic 12 remains correctly `activation: pending`.
- The latest source-integrity pass found no newly consumed public-contract change in the inspected dependency advances; its two High findings are included below.
- The memlog already authorizes the latest critical/high recommendation set through its final assumption, but that assumption is not a substitute for logging any new decision or override made while applying this extraction.

### Prior-decision conflict scan

| Recommendation | Conflict status against `.memlog.md` | Required handling before edit |
| --- | --- | --- |
| Split human-delegated MCP from AI/tool MCP and deny approval mutations to AI/service identities | **No conflict.** This closes an accidental privilege path and strengthens the recorded decisions that risky AI work requires human approval and AI is deny-by-default. | Treat as a contract clarification; preserve human-delegated CLI/MCP parity. |
| Make `Deferred -> NeedsReview` through `ResumeEmailAssociationReview` the only exit from `Deferred` | **No conflict.** No durable decision grants direct confirm/reject from `Deferred`; the existing summary already states the resume-only path. | Reconcile all transition, FR6, error, surface, and test statements together. |
| Expand correction completion to source-owned records and irreversible-effect disposition | **No conflict, but material scope clarification.** It strengthens the recorded correction decision and respects the recorded bounded-context ownership rule. | Add owner commands/acknowledgements through A13; do not let ChatBot mutate Conversations/Folders records directly. |
| Split A11 into M1 and M2 decisions and make A11-M1 an M1 gate | **Direct conflict with the recorded/current gate model.** The memlog and Current Release Status state A5/A6/A13 for M0/M1 and add A10/A11 only for M2. | Record an explicit override if adopting A11-M1. The recommended resolution is to adopt it because the current M1 pass set depends on undefined A11 measures. The alternative is to remove/replace all provisional A11-dependent M1 pass criteria; do not leave the contradiction in place. |
| Move Policy snapshot owner increment from M1 to M0 | **No conflict.** It restores consistency with the recorded M0 governance-bootstrap decision. | Correct the ownership row and bind M0 isolation/A6 evidence to the record. |
| Treat indeterminate risk as typed no-write denial | **No conflict; preferred branch.** It matches the recorded rule that denied or unresolved requests are refused and the glossary/NFR15a no-write fail-closed contract. | Replace `approval-required/fail-closed to review` wording; emit only the security-sensitive attempt record, then require remediation and a new linked request. |
| Remaining High recommendations | **No durable-decision conflict found.** They operationalize current safety, governance, parity, and authority decisions. | Update every normative occurrence and acceptance contract; log any newly selected TTL, state name, actor, or bound as a decision. |

## Apply-now remediation set

### Critical

#### C1 — Human approval cannot be satisfied by an AI-bound MCP principal

**Collision:** The M1 parity set includes AI-action approval; FR83 grants the parity set to MCP clients; `mcp-tool-client` is scoped per AI actor. FR41/FR49 and the product's durable decisions require authorized human approval.

**Recommended decision:** Split MCP authorization into closed principal classes:

- `human-delegated-mcp-client`: current human authentication, recorded user presence, `actorType=human`, source user and delegation expiry; may submit approval/rejection/revision only when the human holds current approval authority.
- `ai-tool-mcp-client`: AI/tool identity; structurally denied `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, and equivalent approval mutations.
- No proposal may be approved by its originating AI actor, its bound tool principal, or the execution/service client acting for it.

**Required edits:** M1 parity wording, RBAC, owner-authority mapping, service-client table, FR41/FR49/FR82/FR83, command exposure policy, glossary, and negative contract tests. Preserve the parity claim for authorized human-delegated sessions; do not claim approval parity for AI/tool principals.

#### C2 — `Deferred` has one legal exit

**Collision:** The lifecycle summary and `MarkEmailAssociationNeedsReview` state that only `ResumeEmailAssociationReview` may leave `Deferred`, while confirm/reject rows allow direct transitions from `Deferred`. FR6 also implies a user can mark an item `NeedsReview`, although the command is worker-only.

**Recommended decision:** Adopt the safer existing summary as authoritative:

`NeedsReview -> Deferred -> NeedsReview -> Associated | Rejected`

- `ResumeEmailAssociationReview` is the only command that leaves `Deferred`.
- Resume requires the revisit condition, refreshed evidence, current actor/Project authority, expected revision, and a new immutable resume audit event.
- `ConfirmEmailProjectAssociation` and `RejectEmailProjectAssociation` accept `NeedsReview` only.
- FR6 enumerates the actual human commands and their required fields; it does not grant the worker-only `MarkEmailAssociationNeedsReview` command.

**Required edits:** Lifecycle summary, canonical definitions, transition table, command catalog descriptions, FR6, UI/CLI/MCP affordances, invalid-transition reason codes, idempotency expectations, and acceptance tests.

#### C3 — Correction completion covers every owner and effect

**Collision:** `Corrected` currently depends on ChatBot-derived-store acknowledgements, while UJ4 promises Conversations and Folders remediation. Already appended messages, stored attachments, converted intents, executed commands, sent mail, and file disclosures have no required correction disposition.

**Recommended decision:** At correction start, atomically freeze a versioned **correction impact manifest** containing the source/destination Projects, current authority evidence for both, every affected ChatBot store, every source-owned record, and every effectful consequence.

- Conversations and Folders perform changes only through owner-supported reassignment or compensation commands accepted under A13.
- Each manifest item receives an immutable owner acknowledgement or an explicit irreversible-effect disposition: `contained`, `compensation-required`, or `cannot-repair`.
- Already sent communication, completed external/tool action, file disclosure, and other irreversible effects are never described as reversed; they remain visible and incident/compensation tracked.
- `Correcting` is a non-terminal state; `CorrectionDelayed` is an incident state; `Corrected` is terminal only when every manifest item has a recorded disposition. A blocked or unrepairable item cannot silently complete the correction.
- AI use remains blocked for every affected source/destination context until manifest completion.

**Required edits:** UJ4, correction states/transitions, Context Ownership, A13 owner acceptance, operation/query catalogs, FR7/FR91a, NFR15a/NFR17a, UI correction surface, retry/incident behavior, and acceptance tests. Add Conversations and Folders producer acceptance to A13 wherever M0 correction relies on their commands.

#### C4 — M1 cannot pass on undefined A11 measures

**Collision:** M1 requires SM8/SM12/SM15/SM16/SM-C3/SM-C5, but SM16 and SM-C5 depend on provisional A11 definitions; Current Release Status labels A11 only as an M2 blocker.

**Recommended decision:** Split A11 into:

- **A11-M1 — pilot-measure qualification:** before the M1 observation window, freeze each mandatory M1 metric's definition, denominator, supported-request mix, target, minimum sample/window, evidence source, owner, and pass/fail rule.
- **A11-M2 — operating SLO qualification:** retain the complete candidate-bound SLO catalog, live signal, alert route, error budget, and burn-test requirements.

Add A11-M1 to Current Release Status, the M1 gate row, qualification evidence, and invalidation/reopen rules. Record this as an explicit override to the prior A11-is-M2-only gate decision. Do not allow provisional values to pass M1.

### High

#### H1 — Make gate approval freshness executable

Define one machine-evaluable gate record for A5/A6/A13 and reusable future gates: gate ID/version, decision, exact candidate and dependency revisions, environment/profile, evidence locators and hashes, owner and independent approver identities, independence validation, approval/expiry timestamps, revocation and reopen predicates, superseded decision, and computed `open|approved-current|expired|invalidated|superseded` status. Release gates consume this record, not prose or file presence.

#### H2 — Policy snapshot is an M0 record

Change the Data Governance Surface owner increment to M0. Enumerate the M0 policy rows, first immutable bootstrap snapshot, retention/export/erasure treatment, atomic audit reference, native-store isolation proof, and A6 obligations. M1 adds the full editor and M1-only knobs; it does not become the record's first owner increment.

#### H3 — Indeterminate risk is not an approvable proposal

Resolve the classifier conflict with the no-write branch favored by existing durable decisions:

- An indeterminate classifier result returns typed `classifier-indeterminate` and writes no proposal/idempotency state.
- It creates only the separately typed, security-sensitive non-mutating attempt record allowed by NFR15a.
- No reviewer can approve an indeterminate classification.
- After classifier/input/authority remediation, the requester creates a new linked request that must classify determinately.

Align Risk Classifier, Retry Profiles, Shared Workflow Contract, NFR15a, message catalog, audit semantics, and surface behavior.

#### H4 — Define approval expiry and drift invalidation

Add a closed approval lifetime contract: immutable `approved_at`/`expires_at`, safe default and maximum TTL by effect class, `Approved -> Expired`, an expiry event, execution-time proposal/content/resource digest checks, current policy/authority/sender evidence checks, revocation/material-drift invalidation, and a new linked proposal/approval after expiry or drift. Any tenant knob may only tighten bounds.

#### H5 — Publish one association evaluation protocol

Separate correct association, safe abstention, and wrong association. Use mutually exclusive populations, minimum class counts, prevalence/sampling rules, ground-truth adjudication and disagreements, exact precision/recall/abstention formulas, and confidence bounds. Make SM1, SM7, SM-C1, A9a, and Confidence Thresholds use the same recall population; abstention must not inflate association correctness.

#### H6 — Make pre-pilot data-rights workflows operable

Define initiating/reviewing roles and owner-side authority for export, erasure, legal hold, and retention; remove the undefined `authorized data-subject operator` label. Add status/result/redaction/expiring-download queries, partial-completion semantics, hold precedence, rejection/appeal, retry, and result delivery. Declare an explicit pre-pilot governed API/CLI/provisioning or M0 admin surface and add a journey or acceptance contract. Trace every workflow to A6 evidence.

#### H7 — Model unknown outbound-send outcomes

Add authoritative `SendOutcomeUnknown` and `Reconciling` states, provider evidence, an authorized reconciliation operation/query, timeout and escalation rules, and terminal `Sent`, `NotSent`, or `Unresolved` results. Blind retry stays forbidden; a new draft is permitted only after an audited `NotSent` result.

#### H8 — Treat external/retrieved content as untrusted data

Add a content-origin and instruction-authority contract: email bodies, quoted threads, attachments, filenames, retrieved Project context, and tool results are data, never system/tool policy or authorization. Preserve origin labels through extraction/context/output, route suspicious instruction patterns to safe review, and state that model output never supplies authority. Add adversarial prompt-injection fixtures to A5 and the AI-action acceptance matrix.

#### H9 — Use a closed admission-stage matrix

Preserve one central command spine, but select stages from a closed operation/effect-class profile. Authentication, tenant binding, authorization, stable identity/concurrency, and atomic audit remain universal. AI risk/approval stages apply only to declared AI-mediated effect classes; admin separation of duty, retention authority, mailbox authenticity, and other domain guards apply to their declared classes. The central pipeline, never an adapter or handler, selects the profile. Update FR81a and the normative Shared Command Pipeline together.

#### H10 — Require the full sender-authority evidence tuple

Define a provider-neutral tuple for each sender authority class and a versioned M365 mapping: token subject/client, OAuth permission mode, target mailbox, mailbox ACL/delegation grant, current membership, sender identity, send-as/send-on-behalf semantics, tenant, ChatBot policy/authorization, evidence timestamp, and revalidation result. Every required element must be present and current; missing or mismatched evidence yields a typed denial.

#### H11 — Give authenticity blocking its own intake workflow

Define an intake/authenticity state family before association, with exact commands, actors, evidence visibility, retention, terminality, and reprocessing. `strict` anomalies enter an authorized authenticity-review state; `paranoid` anomalies enter a blocked state. Only released/accepted intake proceeds to association `Received`; blocked/rejected work is retained and reprocessed only by an audited successor according to policy. Do not overload association `NeedsReview`, `Failed`, `Skipped`, or participant `Quarantined`.

## Directly prerequisite Medium findings

Only two Medium findings are prerequisites of the Critical reconciliation and should be closed in the same edit:

1. **FR6 actor/required-field mismatch:** absorbed into C2. Remove the user-facing worker-only `MarkEmailAssociationNeedsReview` capability and make confirm/reject/defer fields match the authoritative transition rows.
2. **Correction state representation:** absorbed into C3. Publish one canonical representation in every lifecycle list and diagram: `Associated -> Correcting -> CorrectionDelayed | Corrected`, with `Corrected` alone terminal and the recovery edge from `CorrectionDelayed` to `Corrected` after manifest completion.

## Recommended application order

1. Record the C4 gate override and all newly selected contract decisions in `.memlog.md` through `memlog.py` before changing normative text.
2. Reconcile C1–C4 across every authority-map destination, not only the cited line.
3. Apply H1–H11, keeping `prd.md` capability-oriented and placing executable mechanism/schema detail in `addendum.md`.
4. Refresh the supporting qualification evidence affected by the new gates and owner contracts.
5. Run source reconciliation, rubric, adversarial, and source-integrity reviews again; resolve all Critical/High findings before polish and finalization.

## Acceptance condition for this update

The update is ready for finalization only when the normative documents admit one answer for each of the following: who may approve an AI proposal; the only exit from `Deferred`; what must be repaired or dispositioned before correction completes; which A11 record blocks M1 and M2; which increment owns policy snapshots; the durable outcome of indeterminate classification; how approvals expire; how data-rights actors receive results; how unknown sends reconcile; how untrusted content is isolated from instructions; how command stages are selected; what constitutes sender authority; and what `paranoid` authenticity blocking means.
