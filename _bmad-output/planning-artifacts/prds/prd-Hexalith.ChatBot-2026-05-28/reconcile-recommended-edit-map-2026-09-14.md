---
title: Recommended Critical and High Edit Map — Hexalith.ChatBot PRD
status: proposed
created: "2026-09-14"
source: "validation-report.md and its current rubric, adversarial, and source-integrity reviews"
scope: "Unresolved Critical and High recommendations only; no edits applied to prd.md, addendum.md, or .memlog.md"
---

# Recommended Critical and High Edit Map

## Verdict and editing constraints

All four Critical and eleven High consolidated recommendations in `validation-report.md` remain unresolved in the current normative PRD/addendum. Several contracts already contain useful fragments, but none of the 15 findings is fully satisfied. Apply the edits below as one coordinated contract reconciliation before returning the artifact to `final`.

Preserve every existing FR/NFR/SM identifier. Extend existing requirements instead of renumbering them. New stable **operation IDs** are proposed only where a genuinely missing command/query makes a workflow inoperable. Where a change affects both the capability statement and its executable detail, keep the capability/invariant in `prd.md` and the mechanism or closed schema in `addendum.md`.

Mechanical checks on the current artifact:

- FR1–FR96 and NFR1–NFR70 are contiguous and unique; all letter-suffixed definitions are unique. There is no current requirement-ID drift to repair.
- No exact duplicate FR/NFR bodies were found. Semantic overlap is intentional authority layering, but the edits below must update all listed copies together so they do not create new contradictions.
- The remaining explicit assumption tags resolve to A9a or A11. There are no live `[NOTE FOR PM]` or `Open Questions` sections. A11 must be split by increment as described in C4; no other assumption tag blocks these edits.
- Do not add implementation transport choices to the PRD. Closed classifier, pipeline, sender-evidence, approval-lifetime, and authenticity mechanisms belong in `addendum.md`.

## Critical edits

### C1 — Prevent AI/service self-approval across MCP

**Status:** unresolved. FR41/FR49 say “human,” and the AI allowlist excludes admin/outbound mutations, but those controls do not restrict the ChatBot entry command `ApproveAIAction`. The current `mcp-tool-client` is AI-bound and receives the full parity set.

**Exact anchors:** `prd.md` §Measurable Outcomes → Cross-surface parity outcomes; §RBAC Matrix; §Service Client Permissions; §Shared Workflow Contract → AI action approval/rejection/revision; §Command and Query Contracts; FR41, FR42, FR49, FR82, FR83, FR86; NFR16, NFR67. `addendum.md` §Shared Command Pipeline.

**Minimal coordinated change:**

1. In the singular M1 parity set and FR82/FR83, call the capability a **human-delegated AI-action approval decision**. State that parity does not mean every principal class can exercise every operation.
2. Replace the one `mcp-tool-client` row with two closed principal classes:
   - `mcp-human-delegated-client`: current human authentication and presence; audit records the user, delegation, `actorType=human`, expiry, and MCP origin; may invoke approval decisions when the human has Project/action authority.
   - `mcp-ai-tool-client`: AI/tool-bound; may inspect only its granted non-approval tools and is structurally denied `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, `CancelAIAction`, and equivalent approval mutations.
3. In the RBAC MCP row, FR41/FR42/FR49, and the approval transition rows, require a current authorized human whose identity is distinct from the proposing AI/service principal. Add the invariant: no AI-originated proposal can be approved by its originating AI actor, its execution client, or another credential controlled by that actor.
4. In the addendum pipeline, make principal class a centrally selected admission-profile input; adapters cannot relabel an AI/tool principal as human-delegated.
5. Extend FR86/NFR67 acceptance coverage with negative UI/API/CLI/MCP contract tests for AI/service self-approval, actor-type spoofing, stale human presence, and delegation expiry.

No new FR/NFR or public operation ID is needed.

### C2 — Make `Deferred` transitions single-path

**Status:** unresolved. The lifecycle summary and resume row already select the safer path, but confirm/reject still accept `Deferred`, and FR6 grants a broader user action.

**Exact anchors:** `prd.md` §Shared Workflow Contract association summary and rows for `MarkEmailAssociationNeedsReview`, `ConfirmEmailProjectAssociation`, `RejectEmailProjectAssociation`, `DeferEmailProjectAssociation`, `ResumeEmailAssociationReview`; §UI Surface Inventory S2; FR5, FR6, FR76; §Functional Acceptance Guidance FR1–FR12; NFR15/NFR15a, NFR70.

**Minimal coordinated change:**

1. Keep the existing canonical rule `Deferred -> NeedsReview` through `ResumeEmailAssociationReview` after revisit-condition satisfaction and evidence refresh.
2. Change the source of `ConfirmEmailProjectAssociation` and `RejectEmailProjectAssociation` from “`NeedsReview` or `Deferred`” to `NeedsReview` only.
3. Rewrite FR6 to enumerate the legal human actions and mandatory fields: confirm from fresh `NeedsReview`; reject-all with reason; defer with owner and revisit condition; resume `Deferred` after evidence refresh. Remove “mark an item as needing review,” which remains a worker transition from `Received`.
4. In S2/FR76, while state is `Deferred`, expose Resume only; confirm/reject show `disabled-with-reason: state-not-permitted` until resume succeeds.
5. Add acceptance cases for direct confirm/reject from `Deferred` returning the typed invalid-transition response without mutation, plus resume/revision/idempotency behavior.

No ID additions or state additions are needed.

### C3 — Make correction completion cover source-owned and irreversible effects

**Status:** unresolved. Current correction completion covers only named ChatBot-derived stores. Conversations/Folders and irreversible consequences are omitted.

**Exact anchors:** `prd.md` UJ4; §Shared Workflow Contract association correction rows; §Data Governance Surface; §Context Ownership; §Command and Query Contracts; A13; FR7, FR8, FR23, FR24, FR28, FR30, FR44, FR50, FR60, FR91, FR91a; NFR13, NFR15a Correction row, NFR17/NFR17a, NFR47, NFR50/NFR51, NFR65. `addendum.md` §Retry Profiles and owner-executable contract mapping.

**Minimal coordinated change:**

1. At `CorrectEmailProjectAssociation`, require current authority on both source and destination Projects and atomically create a **correction impact manifest**. Each manifest item records owner context, affected record/effect, reversibility class, required repair/compensation, owner command/version, status, evidence, and acknowledgement.
2. Enumerate the minimum impact set: Conversations assignment/messages; Folders attachment location/access; ChatBot-derived rankings/evidence/proposals/indexes/queues; task-intent conversions; approved/executed actions; sent outbound mail; and prior file disclosures. Require immutable outcomes `repaired`, `contained`, `compensation-required`, or `cannot-repair` for every item.
3. Preserve the stable `AcknowledgeAssociationCorrectionStore` operation ID, but explicitly treat its name as a legacy ID whose payload acknowledges one manifest item, including owner-context repairs and irreversible-effect dispositions—not only a derived store. The final acknowledgement may emit `AssociationCorrected` only when every required item has a terminal disposition and evidence.
4. Replace “sub-state of Corrected” wording: `Correcting` and `Correction-delayed` are separate non-terminal states; only `Corrected` is terminal. Update the summary and §Association Lifecycle list accordingly.
5. Extend FR7/FR91a and the NFR15a Correction row with dual-Project authority, manifest durability, owner acknowledgement, and blocked/incident behavior. NFR17a measures completion of the full manifest, not only reindexing.
6. Add `Correction impact manifest` as an M0 ChatBot durable record in §Data Governance Surface. Update UJ4/S4 so users see outstanding owner repairs and irreversible effects, not only reindex progress.
7. Extend A13 and its M0 gate/approver list to require accepted, versioned Conversations reassignment/message and Folders relocation/revoke-quarantine-copy contracts, plus their idempotency, authorization, audit, retry, and acknowledgement semantics. Add Folders to the A13 owner approvals.

Do not rename existing operation IDs. Owner-context command names remain in the addendum/source manifest once producers accept them.

### C4 — Split A11 into executable M1 and M2 gates

**Status:** unresolved. M1 currently gates on SM8/SM16/SM-C3/SM-C5 while their definitions depend on A11, but Current Release Status calls A11 M2-only.

**Exact anchors:** `prd.md` §Current Release Status; §Measurable Outcomes SM8, SM16, SM-C3, SM-C5; §Minimum Release Slice M1/M2 rows; A11; NFR23, NFR46, NFR65. `addendum.md` §Operating Baselines. `qualification-evidence.md` should later carry evidence, not normative definitions.

**Minimal coordinated change:**

1. Preserve ID A11 as the umbrella decision and define two named sub-gates inside it:
   - **A11-M1:** before the M1 observation window, freeze every M1 metric formula, population/denominator, supported-request mix, target, minimum sample/window, exclusions, evidence source, owner, and pass/fail rule.
   - **A11-M2:** complete the candidate-bound SLO catalog already specified in the Operating Baselines appendix.
2. Change Current Release Status so M0/M1 are blocked by A5/A6/A13 and **A11-M1 where M1 metric observation/exit is concerned**; M2 additionally requires A10 and A11-M2.
3. Add A11-M1 explicitly to the M1 gate evidence and disable condition; retain A11-M2 in M2. State that provisional/starter values cannot pass M1.
4. For SM-C3, include the definition work in A11-M1 rather than allowing an undefined “rubber stamp” percentage; for SM-C5, freeze the supported-request mix before measurement. Keep SM8 and SM16 IDs unchanged.

No new assumption ID is required; A11 remains stable.

## High edits

### H1 — Define a machine-evaluable gate approval record

**Status:** unresolved. Evidence artifacts request pieces of this data, but no normative schema computes `current`, `expired`, or `invalidated` consistently.

**Exact anchors:** `prd.md` §Current Release Status; §Minimum Release Slice gate table; §Open Assumptions A5, A6, A10, A11, A13; NFR65/NFR65a. `addendum.md` new §Qualification Gate Record.

**Minimal coordinated change:**

1. Add a normative addendum schema with `gate_id`, decision, exact candidate revision, consumed dependency revisions, evidence locators and hashes, environment/profile, owner, independent approver(s), signer identities/roles, approval time, expiry time/rule, reopen predicates, revocation/supersession link, and computed status `open|current|expired|invalidated|superseded`.
2. Require signer independence where a gate names separate owner/approver roles; the same principal cannot satisfy both.
3. Make Current Release Status and NFR65 consume computed gate records, never prose or mere file presence. Dependency revision, evidence hash, environment, policy, or signer-authority drift triggers the declared reopen predicate.

Keep revision-specific instances in `qualification-evidence.md`; do not copy them into the normative PRD.

### H2 — Assign the policy snapshot record to M0

**Status:** unresolved and localized. M0 already creates/uses the record; only the ownership table says M1.

**Exact anchors:** `prd.md` §Data Governance Surface → Policy snapshot row; §Increment M0; §Shared Workflow Contract Tenant policy; FR61; NFR9, NFR15a, NFR53.

**Minimal coordinated change:** change the Policy snapshot owner increment from `M1` to `M0`, and clarify that M0 owns immutable bootstrap/decision snapshots plus their A6 retention/export/erasure and native-store isolation evidence; M1 adds the full editor and M1-only schema rows. No new record class or requirement ID is needed.

### H3 — Give indeterminate risk one canonical no-write outcome

**Status:** unresolved. The least-expansive safe resolution is a typed no-proposal refusal, consistent with NFR15a.

**Exact anchors:** `prd.md` §Shared Workflow Contract AI action proposal; §Task Intent and AI Action Mediation; FR37, FR39, FR46, FR77; NFR15a AI action proposal row, NFR17. `addendum.md` §Risk Classifier and §Retry Profiles → Task-intent and risk classification.

**Minimal coordinated change:**

1. In the Risk Classifier, replace “treated as `approval-required` (fail-closed to review)” with: an indeterminate result returns typed `classifier-indeterminate`, creates no AI-action proposal, cannot be approved/executed, and emits only the security-sensitive attempt audit permitted by NFR15a.
2. In the retry profile, replace the contradictory `NeedsReview`/approval-required durable exhaustion state with the same typed no-proposal result. After classifier/policy remediation, a user may submit a new linked `ProposeAIAction` operation; the indeterminate attempt itself is immutable and never promoted.
3. Add this outcome to FR39/FR46/FR77 acceptance and the workflow proposal guard. Keep NFR15a unchanged except to explicitly say “no proposal state.”

This avoids introducing a new workflow state, command, or FR ID.

### H4 — Define approval lifetime and material-drift invalidation

**Status:** unresolved. Evidence freshness exists, but approval freshness does not.

**Exact anchors:** `prd.md` §Shared Workflow Contract AI approval/execution; §Service Client Permissions; FR42, FR44, FR50; NFR6, NFR16, NFR36, NFR48, NFR65. `addendum.md` new §Approval Lifetime.

**Minimal coordinated change:**

1. Add an addendum contract requiring `approved_at`, `expires_at`, effect class, content/resource/recipient/sender/tool digests, policy/allowlist/classifier versions, authority evidence references, and target revision.
2. Define `Approved -> Expired` with `AIActionApprovalExpired`; execution rechecks all fields. Any TTL expiry, evidence expiry, authority revocation, policy/allowlist/classifier change, target revision change, or digest drift invalidates execution.
3. Require a new linked proposal and human approval after expiry/material drift; approvals never renew or mutate in place.
4. Add one unresolved product decision to A11-M1 only if exact default/max TTL values are not selected during editing. Until values are fixed, the M1 approval execution gate is unsupported and blocked. Do not silently borrow evidence-cache TTLs.

No new FR/NFR ID is needed. Exact TTLs are the only product-choice blocker in this edit.

### H5 — Publish one non-gameable association evaluation protocol

**Status:** unresolved. SM1 combines success with abstention, and SM7/addendum use incompatible recall populations.

**Exact anchors:** `prd.md` §Measurable Outcomes SM1, SM7, SM-C1; A9a; §Functional Acceptance Guidance FR1–FR12; FR3–FR6, FR9, FR11. `addendum.md` §Confidence Thresholds.

**Minimal coordinated change:**

1. Keep SM1 and SM7 IDs but define separate reported/gated measures: auto-association precision, deterministic-match coverage/recall, safe abstention for ambiguous/no-match/unauthorized classes, and wrong-Project rate. A safe no-association must not count as a correct association.
2. Use the same mutually exclusive A9a populations in SM7 and the addendum. Define exact numerators/denominators, minimum count per class, sampling/prevalence constraints, ground-truth adjudication/disagreement procedure, exclusions, and confidence bounds.
3. Preserve SM-C1 as the zero-tolerance unauthorized-project subset; report it separately from all wrong-Project outcomes.
4. Make threshold calibration and M0/M1 gates consume the versioned protocol/run, preventing threshold changes from redefining the measured population.

Avoid creating a second competing metric appendix; §Confidence Thresholds remains the executable protocol authority.

### H6 — Make pre-pilot data-rights workflows operable end to end

**Status:** unresolved. Mutation rows exist, but the actor, queries, status/result delivery, and M0 surface do not.

**Exact anchors:** `prd.md` §Shared Workflow Contract data export/erasure/legal hold/retention; §Permission Model, §RBAC Matrix, §Owner-authority mapping; §Command and Query Contracts; §UI Surface Inventory; §Compliance Requirements; FR58, FR65, FR75f, FR80; NFR2, NFR15a, NFR36, NFR49a, NFR53–NFR55, NFR65, NFR70. `addendum.md` §Retry Profiles.

**Minimal coordinated change:**

1. Replace undefined “authorized data-subject operator” with `compliance-admin` acting under current tenant/Project/data-subject scope; require an independent authorized compliance/privacy approver for erasure, hold, release, and any authority-expanding result access. Internal workers execute but cannot initiate/approve.
2. Add the owner-authority row and clarify FR75f: compliance-admins may operate only the named data-protection workflow family; “cannot operate on workflow items” continues to apply to collaboration/Project work items.
3. Add stable queries `GetDataExportStatus`, `GetDataExportResult`, `GetDataErasureStatus`, `GetLegalHoldStatus`, and `GetRetentionDispositionStatus`. Responses include state, scoped/partial completion, hold precedence, rejected reason, expiry, surviving metadata, redaction, owner, and next safe action.
4. Expand FR58 acceptance to cover request, authorization, review, partial completion, result delivery/download expiry, hold precedence, rejection, new linked resubmission/appeal, retry, redaction, and audit.
5. Declare a restricted, human-authenticated **M0 governance operations API/provisioning surface** before first pilot data; it is not CLI/MCP parity and not a public UI. The M2 compliance UI may later consume the same contracts.

New stable query IDs are necessary; no new FR/NFR ID is needed.

### H7 — Model unknown outbound-send outcomes

**Status:** unresolved. The retry appendix already forbids blind retry, but the workflow has no state or resolver.

**Exact anchors:** `prd.md` §Shared Workflow Contract Outbound email; §Command and Query Contracts; §UI Surface Inventory S6; FR47–FR50, FR65, FR66, FR77, FR80; NFR13/NFR13a, NFR17, NFR31, NFR36, NFR70. `addendum.md` §Idempotency Keys and §Retry Profiles → Outbound send.

**Minimal coordinated change:**

1. Extend the outbound family to `Drafted -> Sending -> Sent | NotSent | SendOutcomeUnknown`; `SendOutcomeUnknown -> Reconciling -> Sent | NotSent | Unresolved`.
2. Add stable operation `ReconcileOutboundSendOutcome` and query `GetOutboundSendOutcome`. Only `mailbox-admin`/authorized operator may reconcile using stable provider message/submission evidence, mailbox, recipients, timestamps, and provider trace.
3. Define reconciliation timeout/escalation; `Unresolved` is terminal and blocks another send. Only an auditable `NotSent` allows a new draft and new human approval. `Sent` never resends.
4. Align FR47–FR50, S6, FR65/FR77, the idempotency row, and retry profile to these exact states and typed messages.

The two new operation IDs are necessary; do not overload `Failed`.

### H8 — Treat email, attachments, and retrieved content as untrusted AI data

**Status:** unresolved. Authorization, malware, and provider reuse are covered; instruction/data trust is not.

**Exact anchors:** `prd.md` §Key Product Risks; System Journey; §Task Intent and AI Action Mediation; §Security and Isolation Acceptance Test Matrix; FR27, FR33, FR35, FR39–FR46; A5; NFR8–NFR10, NFR21, NFR65, NFR67/NFR68. `addendum.md` new §AI Content Trust Boundary.

**Minimal coordinated change:**

1. Add the product invariant to FR33/FR39 and NFR8/NFR9: email bodies, quoted threads, attachments, filenames, retrieved Project content, tool results, and model output are untrusted data and cannot define system/tool policy, authority, recipients, command scope, or approval.
2. Add an addendum mechanism: immutable origin/trust labels survive extraction and retrieval; system/developer/tool policy is separated from content; suspicious instruction-like content routes to safe review; model output never supplies authority; every proposal cites source origins.
3. Extend A5 evidence and NFR67/NFR68 fixtures with prompt/tool-injection cases for each source class and attempts to alter policy, recipients, commands, files, or approval behavior.
4. Add user-visible provenance/suspicion behavior to FR27/FR42 without exposing restricted content.

No new FR/NFR ID is needed; mechanism detail belongs in the addendum.

### H9 — Replace the universal AI-stage command spine with closed admission profiles

**Status:** unresolved. FR81a and the addendum literally run AI classification/approval validation for every mutation.

**Exact anchors:** `prd.md` §Shared Workflow Contract; FR55, FR75a–FR75g, FR81a, FR86; NFR15a, NFR50/NFR50a, NFR70. `addendum.md` §Shared Command Pipeline.

**Minimal coordinated change:**

1. Rewrite FR81a so universal stages are authentication, tenant binding, authorization, stable operation identity, concurrency/revision validation, and atomic canonical audit. The central spine selects one closed, versioned admission profile by operation/effect class.
2. Add a compact addendum matrix:
   - AI-mediated effects: risk classification + proposal/approval validation.
   - Mailbox intake: provider authority + authenticity admission.
   - Policy/admin/service-client/safety controls: schema + separation-of-duty approval.
   - Data protection/retention: A6 scope + legal-hold precedence.
   - Owner-context mutations: current owner authority + accepted A13 concurrency contract.
   - Projection/notification operations: source watermark or destination/redaction guards.
3. Unknown operation classes fail closed; adapters/handlers cannot select, omit, or reorder profiles. FR86 contract tests verify the selected profile and universal envelope, not merely normalized command equality.

No new FR/NFR ID is needed. This edit must precede any test wording changes to avoid duplicating enforcement in adapters.

### H10 — Require the full sender-authority evidence tuple

**Status:** unresolved. The current table conflates OAuth/API capability, mailbox delegation, and ChatBot authority.

**Exact anchors:** `prd.md` §Microsoft 365 / Exchange Permission Constraints; §Shared Workflow Contract Outbound email; FR47–FR50; NFR5, NFR6, NFR16, NFR31. `addendum.md` §Inbound Message Authenticity → Authority class mapping and Tenant Policy Schema `outbound.authority-enabled`.

**Minimal coordinated change:**

1. Add one provider-neutral required tuple: token subject/client, delegated vs application permission mode, exact OAuth/API scopes, tenant, target mailbox, current mailbox ACL/delegation, current membership, asserted sender, send-as vs send-on-behalf relation, ChatBot project/outbound grant, evidence timestamp/freshness, and execution-time revalidation result.
2. Expand every FR48 class row to map the **intersection** of those elements to its M365 posture; `Mail.Send` alone is never sufficient.
3. State in FR48/NFR5 and the outbound guard that every required element must be present, current, internally consistent, and scoped to the same tenant/mailbox/requester. Missing/stale/mismatched evidence returns a typed denial from the FR77 catalog and cannot fall back to a broader authority class.

Keep provider mechanics in the addendum; no new requirement ID is needed.

### H11 — Add a canonical inbound-authenticity workflow

**Status:** unresolved. `strict` and `paranoid` name outcomes, but “blocked” has no lifecycle, owner, surface, retention, or reprocessing rule.

**Exact anchors:** `prd.md` §Shared Workflow Contract before association transitions; §Data Governance Surface; §UI Surface Inventory M0; §Command and Query Contracts; FR1, FR4–FR6, FR10, FR48a–FR48d, FR65–FR77; NFR15a M365 intake, NFR17, NFR21, NFR31, NFR53/NFR54, NFR65/NFR70. `addendum.md` §Inbound Message Authenticity, Tenant Policy Schema `mailbox.authenticity-strictness`, and §Retry Profiles Mailbox intake.

**Minimal coordinated change:**

1. Define an authenticity state family orthogonal to association: capture yields `AuthenticityAccepted`, `AuthenticityReviewRequired` (`strict` anomaly), or `AuthenticityBlocked` (`paranoid` anomaly). Review may move `AuthenticityReviewRequired -> AuthenticityAccepted | AuthenticityRejected`. Blocked/rejected are terminal; reprocessing creates a new audit-linked authenticity workflow after evidence/policy remediation.
2. Guard all association candidate generation/association transitions on `AuthenticityAccepted`; blocked/review-pending items expose no candidate evidence.
3. Add stable commands `ResolveInboundAuthenticityReview` and `ReprocessBlockedInboundAuthenticity`, plus query `GetInboundAuthenticityStatus`. Name `mailbox-admin` + independent `policy-admin` as the review/reprocess authority; neither role gains Project data access.
4. Add the authenticity record to §Data Governance Surface as M0 with A6 retention/redaction. Add an M0 restricted authenticity-review surface (or fold it into a renamed S2 intake/association review surface) showing provider evidence, typed reason, allowed action, owner, and retention outcome.
5. Align FR48d, NFR15a, the Tenant Policy row, and mailbox retry profile to the exact states, terminality, visibility, and new-workflow reprocessing rule.

The two command IDs and one query ID are necessary. Keep these states separate from association `NeedsReview`, participant `Quarantined`, and safety-control quarantine.

## Consolidated edit order

Apply in this order to avoid rework:

1. Resolve C1–C4 product invariants and choose the exact H4 approval TTL values.
2. Update authoritative state/operation contracts together: C2, C3, H3, H6, H7, H11.
3. Update authority/admission contracts together: C1, H8, H9, H10.
4. Apply localized ownership/gate/metric fixes: H1, H2, H4, H5, C4.
5. Propagate the final contracts to journeys, surfaces, functional acceptance guidance, NFR acceptance, traceability ranges, and qualification-evidence placeholders.
6. Re-run ID/reference checks and all current reviewers. Do not set `status: final` until Critical/High count is zero.

## Conflicts and blockers

- **Product decision required:** H4 needs exact default and maximum approval TTLs by effect class. The existing five-minute service-client credential lifetime is relevant evidence but is not itself an approval-lifetime decision.
- **External owner acceptance required:** C3 expands A13 to Conversations and Folders correction/compensation contracts; the PRD can specify the product outcome now, but M0 remains blocked until producers accept executable versions.
- **Evidence remains open:** these contract edits do not close A5, A6, A10, A11, or A13. They make the closure criteria executable.
- **Source manifest drift is Medium, not in this restricted edit map.** Refresh it during final input reconciliation, but it is not one of the requested Critical/High contract repairs.
