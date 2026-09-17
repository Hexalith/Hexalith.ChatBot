---
title: Sprint Change Proposal — Planning Authority and Backlog Rebaseline
project: Hexalith.ChatBot
created: "2026-09-15"
status: approved
decision: approved
approvedAt: "2026-09-15"
handoffStatus: routed
handoffRecipients:
  - Product Manager
  - Solution Architect
workflowMode: batch
changeClassification: major
authoritativeProductSource: prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
---

# Sprint Change Proposal — Planning Authority and Backlog Rebaseline

## 1. Issue Summary

Sprint planning cannot safely generate an assignable ledger because the finalized product contract, downstream architecture, current epic hierarchy, UX review state, planning index, and historical sprint ledger do not describe one coherent baseline.

The immediate trigger is Story 4.4. The finalized PRD requires an indeterminate classifier result to return `classifier-indeterminate`, create no proposal or durable idempotency state, expose no approval action, and perform no effect. The current story instead converts some indeterminate inputs into `approval-required` and names other cases `classifier-unavailable`.

The same review found four broader divergences:

1. Maintained architecture and stories use the non-canonical state `Correction-delayed`; the authoritative product state is `CorrectionDelayed`.
2. The PRD and approved addendum make A11-M1 mandatory for M1, while both architecture spines and Story 11.5 model A11 as an M2-only qualification.
3. The active epic hierarchy contains 13 epics and 146 stories, but the planning index claims 116 and the sprint ledger still models a replaced decomposition, including Story 12.16, which is not in the active hierarchy.
4. The maintained UX spines remain `in-review`; `EXPERIENCE.md` still describes the PRD as draft and carries an upstream mismatch narrative that predates finalization on 2026-09-14.

This is a planning-baseline divergence discovered during readiness/sprint-planning, categorized as a misunderstanding or stale propagation of finalized requirements. It is not a new stakeholder requirement, technical implementation failure, or strategic product pivot.

### Evidence anchors

| Evidence | Current finding |
|---|---|
| `prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | Status `final`; defines `classifier-indeterminate`, `CorrectionDelayed`, and A11-M1 as an M1 gate. |
| `prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | Status `approved`; states that A11-M1 blocks M1 and A10 plus A11-M2 additionally block M2. |
| `epics.md`, Story 4.4 | Uses `approval-required` for unknown effect/authority metadata and `classifier-unavailable` for failed or non-contract classification. |
| `architecture.md` and `architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md` | Use `Correction-delayed`; represent A11 as an M2-only SLO gate; retain the pre-final classifier contract. |
| `epics.md` | Contains 146 story headings; every story has acceptance criteria and requirement links. |
| `index.md` | Claims 116 assignable product stories and describes Stories 12.14–12.16 as the active deferral set. |
| `implementation-artifacts/sprint-status.yaml` | Represents the former decomposition and contains an active Story 12.16 entry. |
| `ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` | Its behavioral contract is largely aligned, but its status and upstream-blocker narrative are stale. |

## 2. Impact Analysis

### 2.1 Product and MVP impact

The PRD does not need a scope change. The M0 → M1 → M2 sequence, the 13-epic product structure, and the 146-story active hierarchy remain viable. The correction restores the finalized contract downstream; it neither removes product value nor adds a new epic.

The MVP remains achievable. Implementation assignment is temporarily unsafe until canonical contracts and tracking converge, because a developer could otherwise implement an approval path the PRD prohibits or claim M1 readiness without mandatory A11-M1 evidence.

### 2.2 Epic impact

| Scope | Impact | Required response |
|---|---|---|
| Epic 2 — Association review/correction | Direct semantic conflict in lifecycle, commands available to users, and delayed-correction state. | Correct state literals and association action boundaries. |
| Epic 3 — Investigation experience | Displays the non-canonical delayed-correction state. | Correct the displayed durable state and preserve the full acknowledgement manifest. |
| Epic 4 — Governed AI assistance | Trigger epic. Classifier failure and indeterminate inputs can enter a forbidden approval path. | Rewrite Stories 4.4, 4.5, and affected status/error presentation. |
| Epic 11 — Operations and observability | Story 11.5 conflates A11-M1 and A11-M2 and qualifies A11 only for M2. | Split the two qualification responsibilities without adding an epic or story. |
| Epic 12 — Compliance/recovery | Release-gate references use unsplit A11; active hierarchy ends at Story 12.15. | Correct gate dependencies and index/tracking ownership. |
| Epic 13 — Interactive workspace | Correction UI and release-gate references inherit stale state/gate names. | Correct state and split gate references. |
| All 13 epics | Active inventory differs substantially from historical implementation tracking. | Rebaseline story keys and statuses through an evidence-aware crosswalk. |

No epic becomes obsolete. No new epic is required. Existing epic order remains mandatory. Epic priority does not change, but A11-M1 qualification must move into the M1 critical path rather than waiting for M2.

### 2.3 Current hierarchy inventory

| Epic | Stories | Epic | Stories |
|---:|---:|---:|---:|
| 1 | 11 | 8 | 10 |
| 2 | 13 | 9 | 11 |
| 3 | 13 | 10 | 10 |
| 4 | 13 | 11 | 10 |
| 5 | 10 | 12 | 15 |
| 6 | 10 | 13 | 8 |
| 7 | 12 | **Total** | **146** |

Structural coverage is already strong: all 146 current stories contain acceptance criteria and requirement links, and the extracted requirement inventory has no missing linked coverage. This correction must add semantic validation because structural linkage alone did not detect the conflicting meanings.

### 2.4 Artifact impact

| Artifact | Impact |
|---|---|
| Final PRD and approved addendum | Normative source; no content change proposed. |
| Root architecture and architecture spine | Contract corrections required for classifier results, association state, correction acknowledgements, and A11-M1/A11-M2 release gating. |
| Epics and stories | Requirements inventory and affected acceptance criteria require semantic correction. Story count remains 146. |
| UX maintained spines | Behavioral content is mostly authoritative; status, stale blocker text, release-gate token, and post-correction validation need updates. |
| Planning index | Replace the 116-story/Story 12.16 narrative with the active 146-story inventory. |
| Implementation story/spec files | Preserve as historical evidence. Build a crosswalk; do not treat filenames or old IDs as the active backlog. |
| Sprint status | Regenerate only after approval and artifact convergence. Do not blindly transfer `done`. |
| Code, deployment, IaC, and dependencies | No rollback or implementation mutation is proposed by this planning change. Later implementation impact must be assessed from the approved, reconciled backlog. |

## 3. Options Considered

| Option | Viability | Effort | Risk | Assessment |
|---|---|---:|---:|---|
| 1. Direct Adjustment | **Viable and recommended** | Medium–High | Medium after controls | Correct maintained planning artifacts, semantically revalidate the current hierarchy, crosswalk historical evidence, then regenerate tracking. Preserves product scope and momentum. |
| 2. Rollback | Not viable | High | High | There is no identified implementation change whose rollback would reconcile competing planning baselines. Reinstating the old 116-story decomposition would discard the active 146-story design and reintroduce stale requirements. |
| 3. PRD MVP Review | Not viable/necessary | High | High | The final PRD is internally explicit on the disputed contracts. Reducing or redefining MVP would solve a downstream propagation problem by changing the source of truth. |

### Recommended approach

Use **Option 1: Direct Adjustment**, executed as a controlled planning and backlog rebaseline. The change classification is **Major**, despite no MVP scope change, because it affects architecture authority, multiple epics, maintained UX status, 146 active story identities, and the interpretation of historical completion evidence.

Expected planning effort is approximately four to seven working days: one to two days for contract reconciliation, two to four days for semantic validation and evidence crosswalk, and about one day for UX/readiness revalidation and ledger generation. This estimate excludes implementation work discovered by the crosswalk.

## 4. Detailed Change Proposals

### 4.1 Final PRD and approved addendum — reaffirm, do not edit

**OLD interpretation:** Downstream artifacts behave as if the product contract were still draft or as if architecture could select different classifier/state/release-gate semantics.

**NEW interpretation:** Treat the finalized PRD and approved addendum dated 2026-09-14 as normative for product behavior and release authority. Do not reopen them for this correction. If a downstream artifact conflicts, update the downstream artifact.

### 4.2 Root architecture and architecture spine

Apply the same contract change to `architecture.md` and `architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md` so the detailed and concise architecture surfaces cannot disagree.

#### A. Classifier outcome and containment

**OLD**

- A valid classifier with missing tags, unknown effect surface, or undeclared authority can yield `approval-required`.
- A missing, invalid, unqualified, failed, or non-contract artifact/output yields `classifier-unavailable`.

**NEW**

- A successful, determinate classification has only two categorical results: `low-risk` or `approval-required`.
- Missing/invalid/unqualified/failed/non-contract classifier artifacts or outputs, missing tags, unknown effect surfaces, and undeclared authority classes all return the typed product result `classifier-indeterminate`.
- `classifier-indeterminate` creates no proposal, durable domain or idempotency success, approval action, or effect. It records a separate redacted, non-mutating auditable attempt.
- `classifier-unavailable` may remain only as a non-canonical availability/safe-reason detail; it is not the durable product outcome.
- Remediation creates a new linked operation rather than resuming or approving the indeterminate attempt.

Update AD-7/the risk-classifier section, API/result contracts, decision summaries, and any diagrams or examples that encode the old path.

#### B. Association lifecycle and correction manifest

**OLD:** `Correction-delayed` is used as a durable state, and some architecture summaries narrow the derived-store acknowledgement set.

**NEW:** Use the exact durable state `CorrectionDelayed`. Require the complete finalized manifest for affected Conversations/Folders records and indexes, actions, appended messages, conversions, sent mail, external/tool effects, file disclosures, and explicit irreversible dispositions. Keep affected AI context blocked until every required store acknowledges the successor or the workflow reaches its permitted terminal outcome.

Also normalize the association lifecycle to the finalized state machine; do not reintroduce `Proposed` as an association state.

#### C. Release authority and A11 split

**OLD:** “A5/A6/A13 block M0 and M1; A10/A11 additionally block M2,” with A11 represented only as an M2 SLO qualification.

**NEW:**

- M0 requires its finalized current gate set.
- M1 revalidates lower-increment gates and requires A11-M1 as specified by the PRD/addendum.
- A11-M1 freezes the mandatory M1 metric definitions, denominators, supported mix, provisional targets, minimum samples/windows, evidence sources, owners, and pass/fail rules for SM8, SM16, SM-C3, and SM-C5, while recording SM12 and SM15 in the same evidence bundle.
- M2 revalidates changed lower-increment gates and additionally requires A10 and A11-M2.
- A11-M2 qualifies every declared SLO against the exact release candidate with target, unit, window, error budget, source signal, alert route, calibration, and burn evidence.
- Missing, stale, partial, mismatched, or historical A11-M1 evidence blocks M1; equivalent A11-M2 defects block M2. The two decisions are independently machine-readable.

Replace unsplit A11 references in frontmatter, release tables, D12/AD-12, observability sections, sequencing, completion checks, and open-gate summaries with the applicable A11-M1 or A11-M2 reference. Include A9a wherever the finalized current release-gate inventory requires it.

### 4.3 Epics and stories

Temporarily mark epic design/extraction review as in progress while the following edits are applied. Restore `approved`/`confirmed` only after story inventory, structural coverage, and semantic conformance checks pass.

#### A. Requirements inventory normalization

| Entry | OLD | NEW |
|---|---|---|
| FR6 | Lets a user mark an association `NeedsReview`. | Make `MarkEmailAssociationNeedsReview` worker-only. User actions from `NeedsReview` are Confirm, Reject, or Defer; `Deferred` must Resume to `NeedsReview` before another decision. |
| FR34 | Uses an incomplete or stale attachment-state list. | Use exactly `PendingScan`, `Stored`, `Unsafe`, and `Failed`. |
| FR58 | Summarizes the O1 data-rights interface without its approval/ownership constraints. | Preserve specific actor, independent approval, owner-by-owner execution/evidence, exception, and aggregate-result requirements from the final PRD. |
| FR83 | Collapses MCP authority into a simplified principal model. | Preserve structural separation between the human-delegated principal and the tool principal. |
| FR91a / NFR17a / UX-DR34 | Uses `correction-delayed` or `Correction-delayed`. | Use the exact state `CorrectionDelayed` and the full affected-store manifest. |
| ARCH-39 | Treats A11 as an M2-only gate. | State A11-M1 blocks M1; A10 and A11-M2 additionally block M2, with lower-gate revalidation. |
| UX-DR50 | Converts otherwise-safe indeterminate classification into reviewable `approval-required`. | Return `classifier-indeterminate` with no proposal, approval action, durable idempotency success, or effect. |

Run a full semantic comparison of all extracted FR/NFR/ARCH/UX entries against the finalized sources after these known corrections. Do not accept presence of a requirement link as proof that its extracted wording preserves the requirement.

#### B. Story 4.4 — replace the conflicting classifier acceptance criteria

**OLD**

> its only classifier outputs are `low-risk` or `approval-required`

> valid classifier machinery but missing tags, unknown effect surface, or undeclared authority class ... is `approval-required`

> a missing, invalid, unqualified, failed, or non-contract classifier artifact/output ... returns `classifier-unavailable`

**NEW**

1. State that `low-risk` and `approval-required` are the only **successful determinate classes**, not the only possible product outcomes.
2. Preserve permanent `approval-required` classification for all six effect classes: state modification, file disclosure, external send, task creation/assignment, external tool invocation, and action on behalf of a participant.
3. Return `classifier-indeterminate` for missing/invalid/unqualified/failed/non-contract classifier artifacts or outputs, missing tags, unknown effect surface, and undeclared authority class.
4. On `classifier-indeterminate`, create no proposal, durable domain/idempotency success, approval action, or effect; record only the required redacted, non-mutating auditable attempt and safe remediation/escalation.
5. Require a remediated request to use a new linked operation. Never reinterpret the former indeterminate attempt as approvable.
6. Retain pre-classification `denied` and `unsupported` dispositions as separate outcomes.
7. Replace the stale UX-DR50 dependency with the corrected requirement wording before declaring Story 4.4 ready.

#### C. Story 4.5 and related AI-state presentation

**OLD:** Story 4.5 describes only `denied`, `unsupported`, and successful `low-risk|approval-required` conversion; Story 4.13 presents `classifier-unavailable` as if it were the canonical workflow state.

**NEW:** Add an explicit `classifier-indeterminate` branch before proposal creation. It must terminate the attempt without a proposal, approval action, durable idempotency success, or effect. UI/status stories may display a safe availability reason such as `classifier-unavailable`, but must bind it to the canonical `classifier-indeterminate` outcome and must not offer approval.

#### D. Association stories

**OLD:** Story 2.7 admits `Received or Proposed -> Associated`; Story 2.8 permits reviewer submission of worker-only association commands; Stories 2.11, 2.12, 3.4, 4.12, and 13.5 use the non-canonical delayed-correction state.

**NEW:**

- Remove `Proposed` from the association lifecycle.
- Keep `MarkEmailAssociationNeedsReview` and `SkipEmailAssociation` worker-only.
- Limit human decisions from `NeedsReview` to Confirm, Reject, and Defer. Require Resume from `Deferred` to `NeedsReview`; escalation is guidance/routing, not an invented lifecycle transition.
- Replace every canonical state occurrence of `Correction-delayed`/`correction-delayed` with `CorrectionDelayed`. A command name may remain unchanged if it is already the authoritative command contract; its resulting state must be exact.
- Preserve the complete propagation/acknowledgement manifest and AI-context block in each affected acceptance criterion.

#### E. Story 11.5 and dependent release-gate stories

**OLD title:** `Qualify Candidate-Bound A11 Observability Evidence`

**NEW title:** `Qualify A11-M1 Metrics and A11-M2 Candidate-Bound Observability Evidence`

Replace the single M2-only qualification with two independent result sets:

1. A11-M1 qualifies the mandatory M1 measurement contract and blocks M1 when incomplete, stale, mismatched, unsupported, or unmeasurable.
2. A11-M2 qualifies every declared SLO and candidate-bound burn/route/source requirement for M2, without using historical or partial evidence.
3. Each result records exact candidate/baseline binding, source provenance, freshness, owner, machine-readable failure, and the claims it can and cannot authorize.
4. Passing either A11 result never activates workflows or substitutes for another open gate.
5. Candidate, metric definition, topology, telemetry schema, policy, route, or evidence-source changes invalidate only the affected current qualification while preserving immutable history.

Update Stories 11.4 and 11.6–11.10, Story 12.15, Story 13.8, and any other requirement/acceptance text containing unsplit A11 so each reference names A11-M1, A11-M2, or both according to the claim. Do not add a story: the active hierarchy stays at 146.

### 4.4 Maintained UX artifacts

The interaction contracts in `EXPERIENCE.md` already preserve the principal classifier, correction-state, and A11 split semantics. Correct their authority and review metadata without weakening those contracts.

#### A. Upstream-blocker section

**OLD:** The current PRD/addendum are described as draft, the latest validation STOP remains the operative product status, and the architecture mismatch is written as an unresolved comparison against an earlier product source.

**NEW:**

- Record that the PRD is final and the addendum approved as of 2026-09-14; the product-source approval blocker is resolved.
- Keep architecture alignment open only until both maintained architecture surfaces implement this approved proposal and pass revalidation.
- Change the M1 scope-gate token from A9 to the finalized A9a where applicable.
- Continue to state that `classifier-unavailable` is only a non-canonical availability reason, that the product outcome is `classifier-indeterminate`, and that `CorrectionDelayed` is the exact durable state.

#### B. Review state and validation artifacts

Keep `DESIGN.md` and `EXPERIENCE.md` `in-review` during reconciliation. After architecture and epics converge, run a new UX validation/reconciliation pass against the final PRD, approved addendum, corrected architecture, and corrected epics. Only a passing result may promote the maintained spines to `final`/binding status.

Preserve dated historical reconciliation/review documents as snapshots. Add a new dated result or update the maintained validation index; do not rewrite old findings to make them appear contemporaneous.

### 4.5 Planning index

**OLD:** `epics.md` has 116 assignable product stories, based on 111 plus five additions; Stories 12.14–12.16 own the active Epic 12 deferrals.

**NEW:** State that `epics.md` contains 13 canonical epics and 146 assignable product stories, with the per-epic counts in Section 2.3. Remove Story 12.16 from the active inventory narrative. Describe the actual current Story 12.14 and 12.15 responsibilities, while keeping superseded implementation files discoverable as historical evidence rather than canonical backlog entries.

### 4.6 Historical implementation artifacts and sprint tracking

No historical story/spec file is deleted by this proposal. The old decomposition is evidence, not the active backlog.

After approval and after Sections 4.2–4.5 are complete:

1. Generate an old→current crosswalk using story intent, requirements, acceptance scope, repository evidence, and TE-2 completion integrity—not numeric IDs or similar titles alone.
2. Carry a historical `done` status only when evidence proves the complete current story scope and still satisfies the current TE-2 completion gate. Otherwise set the current story to the safest supported status, defaulting to `backlog`, and link the historical evidence for later review.
3. Generate `sprint-status.yaml` with all 13 active epic entries and all 146 active story keys in exact epic order.
4. Remove Story 12.16 and any other superseded story key from active `development_status`; retain provenance in the crosswalk/history rather than as an assignable item.
5. Replace action items that point to nonexistent current stories.
6. Validate exact equality between epic story headings and active sprint keys, uniqueness of every key, allowed status values, YAML syntax, and current epic/story counts.

This proposal deliberately does **not** regenerate or modify sprint tracking before approval.

## 5. Implementation Sequence and Handoff

### 5.1 Ordered plan

1. **Approve or revise this proposal.** No downstream mutation occurs before explicit approval.
2. **Correct both architecture surfaces.** Establish one technical contract for classifier containment, `CorrectionDelayed`, and A11-M1/A11-M2.
3. **Correct and semantically validate `epics.md`.** Apply the requirements/story edits, confirm 13 epics and 146 unique stories, and re-run structural plus semantic coverage checks.
4. **Refresh UX maintained spines.** Resolve stale authority text and run a new binding validation against the converged product, architecture, and epic sources.
5. **Update `index.md`.** Publish the canonical inventory and historical-artifact policy.
6. **Build the historical evidence crosswalk.** Classify each old implementation entry as exact carry-forward, partial evidence, superseded evidence, or unmapped.
7. **Regenerate sprint tracking.** Create the active 146-story ledger and apply only evidence-supported statuses.
8. **Run implementation-readiness validation.** Require a clean result before making the regenerated ledger assignable.

### 5.2 Roles

| Role | Responsibility |
|---|---|
| Product Manager / Product Owner | Confirm the final PRD remains normative; approve story wording, active inventory, and any status carry-forward policy. |
| Architect | Correct and validate both architecture surfaces and their release-gate tables/contracts. |
| UX Designer | Refresh maintained spine authority/status and produce the new post-reconciliation UX validation result. |
| Test Architect / Quality owner | Run semantic traceability checks and independently challenge evidence used to carry completion status. |
| Developer / Sprint planner | Build the old→current evidence crosswalk, regenerate the sprint ledger, and run exact inventory/readiness checks. |
| Release governor | Confirm A11-M1 and A11-M2 evidence is evaluated at the correct increment; no gate is inferred from another. |

Handoff classification: **Major**. Route through Product Manager/Product Owner and Architect before Developer/Sprint Planner execution. Test Architecture and UX validation are mandatory before the new ledger becomes authoritative.

## 6. Success Criteria

The correction is complete only when all of the following are true:

- The final PRD and approved addendum remain unchanged and are explicitly treated as normative.
- Maintained architecture, epics, and UX use `classifier-indeterminate` for every indeterminate classifier case and expose no proposal or approval path from it.
- Maintained canonical contracts use `CorrectionDelayed`; any lowercase/hyphenated occurrence is either removed or explicitly labelled historical/non-canonical explanatory text.
- A11-M1 blocks M1, and A10 plus A11-M2 additionally block M2, everywhere release authority is summarized or tested.
- The active hierarchy remains 13 epics and 146 unique stories; every story has acceptance criteria and requirement links, and a semantic trace finds no altered or weakened extracted requirement.
- `index.md` reports 146 stories and no longer names Story 12.16 as active.
- The maintained UX spines no longer call the current PRD/addendum draft; their final/binding status is supported by a new passing validation result.
- The regenerated sprint ledger has exact one-to-one coverage of the 146 current story keys, contains no active Story 12.16, and carries no status without sufficient current-scope evidence.
- Historical story/spec evidence remains discoverable and is not mistaken for the current backlog.
- Implementation-readiness validation passes without critical contract, inventory, or authority conflicts.

## 7. Risks and Controls

| Risk | Control |
|---|---|
| A prior `done` status is mapped to a broader or different current story. | Default to `backlog`; require exact current-scope evidence and TE-2 integrity for carry-forward. |
| One architecture surface is corrected while the other remains stale. | Treat root architecture and architecture spine as one atomic review unit. |
| Search/replace changes prose but misses semantic state-machine or authorization differences. | Run targeted semantic tests for classifier containment, association transitions, correction manifests, and increment gates. |
| UX is promoted to final before upstream convergence. | Keep spines in review until a new dated validation passes. |
| Historical files are deleted or rewritten, losing provenance. | Preserve them and record their relationship in the crosswalk. |
| Rebaseline pauses delivery longer than expected. | Keep scope fixed, sequence the corrections, and permit implementation assignment only after the exact story concerned is reconciled and the ledger is authoritative. |

## 8. Checklist Status

| Checklist item | Status | Result |
|---|---|---|
| 1.1 Triggering story | [x] Done | Story 4.4 during readiness/sprint-planning. |
| 1.2 Core problem | [x] Done | Stale/misunderstood finalized requirements propagated across maintained artifacts and tracking. |
| 1.3 Evidence | [x] Done | Conflicts and inventory evidence recorded above. |
| 2.1 Current epic | [x] Done | Epic 4 remains viable after Story 4.4/4.5 correction. |
| 2.2 Epic-level changes | [x] Done | Modify existing stories; no new or removed epic. |
| 2.3 Remaining epics | [x] Done | Direct impacts identified in Epics 2, 3, 11, 12, and 13; all epics affected by ledger rebaseline. |
| 2.4 Obsolete/new epics | [x] Done | None. Historical story decomposition is superseded, not the active epics. |
| 2.5 Order/priority | [x] Done | Preserve M0 → M1 → M2; move A11-M1 into the M1 critical path. |
| 3.1 PRD | [x] Done | No PRD change; MVP remains achievable. |
| 3.2 Architecture | [x] Done | Exact classifier/state/A11 changes defined. |
| 3.3 UI/UX | [x] Done | Maintained-spine authority/status corrections and revalidation defined. |
| 3.4 Other artifacts | [x] Done | Index, historical evidence, testing, readiness, and sprint tracking impacts defined; no code/IaC change proposed. |
| 4.1 Direct Adjustment | [x] Viable | Recommended; Medium–High effort, controlled Medium risk. |
| 4.2 Rollback | [x] Not viable | No suitable implementation rollback; restoring old planning increases risk. |
| 4.3 PRD MVP Review | [x] Not viable/necessary | Source contract is final and explicit; no scope reduction justified. |
| 4.4 Recommended path | [x] Done | Direct Adjustment through controlled rebaseline. |
| 5.1–5.5 Proposal components | [x] Done | Issue, impacts, rationale, MVP, action plan, and handoff included. |
| 6.1 Checklist review | [x] Done | All applicable analysis items addressed; pending actions are explicit. |
| 6.2 Proposal accuracy | [x] Done | Proposal cross-checked against cited maintained artifacts. |
| 6.3 Explicit approval | [x] Done | User approved the complete proposal on 2026-09-15. |
| 6.4 Sprint status update | [x] Done | Completed after approval and upstream convergence on 2026-09-17; the active ledger now contains the exact 13-epic/146-story hierarchy with no unsupported status carry-forward. |
| 6.5 Confirm handoff | [x] Done | Major-change handoff and ordered implementation sequence approved. |

## 9. Decision and Workflow Execution Log

**Approved by the user on 2026-09-15.**

- Issue addressed: finalized product contracts and the current 146-story hierarchy diverge from maintained architecture, epic semantics, UX review metadata, the planning index, and historical sprint tracking.
- Change scope: **Major**.
- Artifacts modified by the Correct Course workflow: this Sprint Change Proposal, `index.md`, `historical-story-evidence-crosswalk-2026-09-17.md`, and `implementation-artifacts/sprint-status.yaml`.
- Routed to: **Product Manager and Solution Architect**, followed by Product Owner, UX Designer, Test Architect, Developer, Sprint Planner, and Release Governor according to Section 5.
- Escalation: the planning-synchronization pause is cleared. Product qualification, implementation, runtime, and release gates remain independently governed by their owning artifacts.
- Tracking disposition: `sprint-status.yaml` was regenerated on 2026-09-17 from the canonical hierarchy after architecture, epics, UX, and index convergence. All 146 current stories default to `backlog`; historical evidence is retained through the crosswalk rather than unsupported active statuses.

### Post-approval execution evidence — 2026-09-17

- The deterministic sprint parser generated 13 epics, 146 stories, and 13 retrospectives in exact epic order.
- The historical evidence audit found no exact current-key/file match and no current product-story TE-2 evidence contract; status carry-forward count is zero.
- `index.md` now reports 146 stories and removes Story 12.16 from the active inventory.
- The evidence crosswalk classifies predecessor material as partial, superseded, or unmapped without deleting historical files.
- The obsolete Epic 12 action depending on predecessor Stories 12.14–12.16 was removed; the six still-valid open actions were preserved unchanged.
- Sprint validation reports valid YAML, legal statuses, no unrecognized keys, no new entries, no dropped orphans, and exact inventory synchronization.
