---
title: Hexalith.ChatBot UX Reconciliation — Current Product Contracts
status: complete
created: "2026-09-14"
refreshed: "2026-09-14"
scope: "Current draft product text, approval status, UX deltas, architecture drift, and the six-effect approval boundary"
sources:
  - ../../product-brief-Hexalith.ChatBot.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/validation-report.md
  - ../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md
---

# Current product-contract reconciliation

## Verdict

The reopened `prd.md` and `addendum.md` now contain candidate text that resolves the previously reported UX-critical ambiguities. They must no longer be described as still containing the MCP-principal, `Deferred`, correction-manifest, classifier, approval-expiry, pre-pilot data-rights, outbound-unknown, authenticity, or A11-split gaps.

That remediation is not yet fresh product approval evidence. Both files are explicitly `status: draft`; the PRD says the draft and draft appendices are not current approval evidence, and no current record claims the open release gates are closed (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:37-44`, `:100-108`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:1-12`). The latest available PRD validation still records `STOP`, four Critical findings, and eleven High findings, but it predates the remediation and therefore establishes the need for re-validation rather than proving that those contradictions remain in the present text (`../../prds/prd-Hexalith.ChatBot-2026-05-28/validation-report.md:1-16`).

The safe hierarchy for the current UX update is therefore:

1. The product brief remains the intent and user-voice anchor.
2. The reopened PRD/addendum are the current **candidate** product contract to distill and re-validate, not an approved implementation oracle.
3. `.memlog.md` remains the durable record of authorized decisions and must record any supersession.
4. Qualification evidence governs gate claims; prose or file presence cannot close a gate.
5. The finalized architecture is downstream and cannot override product intent; where it differs from the candidate contract, it must be reconciled before handoff.

## Candidate product text now defined

These behaviors are defined by the current PRD/addendum text and are no longer open content questions inside those drafts:

| Contract | Current candidate definition | UX consequence |
| --- | --- | --- |
| Inbound authenticity and S2a | Authenticity is a distinct pre-association workflow with `AuthenticityAccepted`, `AuthenticityReviewRequired`, `AuthenticityBlocked`, and `AuthenticityRejected`; only accepted intake starts association. The UI inventory now names `S2a — Inbound authenticity review` (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:486-488`, `:612-623`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:209-218`). | The spines include S2a in IA, Journey 3, states, redacted evidence, two-person accept/reject review, and successor-only reprocessing. They do not alias authenticity review to association `NeedsReview`. |
| Pre-pilot data rights and O1 | `O1 — Data-rights operations API/provisioning interface` is required before first persisted pilot data, with request/status/result, owner-by-owner completion, holds, appeal/retry, expiring redacted delivery, and audit. FR58 names the actors and authority boundary (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:623-623`, `:1340-1340`, and workflow rows `:572-575`). | The spines include O1 in IA, flow, state, and acceptance without presenting it as the later general M2 compliance UI or as part of CLI/MCP parity. |
| `Deferred` association exits | The lifecycle is `Deferred -> NeedsReview` through `ResumeEmailAssociationReview`; direct confirm/reject from `Deferred` is not permitted (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:490-494`, `:523-527`). | The spines expose Resume as the only action from `Deferred`; confirm/reject become available only after refreshed `NeedsReview`. |
| Correction completion | The frozen impact manifest covers ChatBot stores, Conversations/Folders records, approved/executed actions, appended messages, task-intent conversions, sent mail, external/tool effects, and file disclosures; completion requires every acknowledgment or irreversible-effect disposition (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:496-514`, `:530-531`). | The spines preserve `Correcting`/`CorrectionDelayed`, block affected AI context, and show `Corrected` only when the complete manifest closes. |
| Classifier failure | Missing, invalid, unqualified, or indeterminate classification returns `classifier-indeterminate`, creates no proposal or durable idempotency state, cannot be approved, and records only the separate redacted non-mutating attempt (`../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:49-59`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:543-543`, `:1296-1307`). | The spines expose a fail-closed indeterminate outcome with remediation and never render approval controls for classifier absence or unknown metadata. |
| Approval expiry | Two bounded lifetime classes, immutable maxima, `approved_at`/`expires_at`, drift invalidation, and new-proposal-only recovery are specified (`../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:61-68`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:544-550`). | The spines show expiry/freshness and drift invalidation; expired approval cannot execute or renew in place. |
| Outbound reconciliation | `SendOutcomeUnknown -> Reconciling -> Sent | NotSent | Unresolved`; blind retry is forbidden, the default deadline is four hours, and only audited `NotSent` permits a new draft/approval (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:551-551`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:238-240`, `:256-256`). | The spines keep send outcome separate from projection/effect state, show reconciliation evidence and P2 escalation, and never offer resend from unknown/reconciling/unresolved. |
| A11 milestone split | A11-M1 blocks M1 metric qualification; A11-M2 separately blocks M2 SLO/production qualification (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:102-108`, `:288-291`, `:1425-1425`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:149-149`). | The spines surface M1 and M2 qualification independently and do not publish an SLO catalog before A11-M2. |

The current draft also structurally separates human-delegated MCP sessions from AI/tool MCP principals: human decisions require current user presence and `actorType=human`; AI/tool/service identities are denied those mutations (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:215-215`, `:800-816`, `:833-837`, `:1388-1388`). UX should retain machine-approval denial, but it should not call this a still-unresolved product-text contradiction.

## Six-effect approval boundary

The behavior must remain unchanged: every AI-mediated Project-state mutation, file exposure, external communication, task creation or assignment, external-tool invocation, and act on behalf of a participant is permanently `approval-required`, and tenant policy cannot downgrade it (`../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md:49-58`; `../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:1307-1313`). A direct authorized human command follows its own authorization, command, and audit contract rather than becoming an AI proposal solely because it mutates state.

The user's 2026-09-14 UX update decision supersedes the earlier temporal qualifier and permanently retains the six-effect guardrail for this spine pair. The decision log records that supersession independently of the draft product package. No update may relax the rule. The current spines state the permanent behavior and remain `in-review` until the upstream evidence chain is refreshed.

## Remaining reconciliation blockers

### Product approval and validation freshness

The latest validation report is a valid record of the earlier source snapshot, not a validation result for the remediated draft. The present text may not be called approved, final, or safe for unqualified handoff until the selected PRD reviews are rerun and the product package is formally re-finalized. Release gates A5, A6, A13, A11-M1, A10, and A11-M2 remain evidence-controlled and open as stated in the draft (`../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md:100-108`).

### Finalized architecture drift

The declared finalized architecture still reflects pre-remediation contracts. Examples include routing authenticity anomalies into association `NeedsReview`, using `Correction-delayed`, and treating A11 as M2-only (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:212-223`, `:225-242`, `:283-310`). The current candidate PRD/addendum and UX instead use distinct authenticity states, `CorrectionDelayed`, and A11-M1/A11-M2. This is a downstream architecture reconciliation requirement, not permission for UX to choose the older behavior.

### Current UX extraction status

The updated spines now incorporate S2a/O1 flow and IA coverage, canonical authenticity states, surface-local view-state and accessibility acceptance, permanent six-effect approval, outbound reconciliation, identity fallback, and the refreshed supplemental contracts. Accessibility and governance reviewers report zero findings. The only remaining downstream extraction blocker is the upstream authority mismatch described above: the product revision is not freshly approved, and finalized architecture does not yet implement the same contract.

## Recommended disposition

1. Keep `DESIGN.md` and `EXPERIENCE.md` at `status: in-review`.
2. Re-run PRD validation against the current draft; if resolved, formally re-finalize the PRD/addendum without implying that release gates have passed.
3. Preserve the authorized UX memlog supersession; after product approval, link it to the exact approved product revision without weakening the rule.
4. Reconcile the finalized architecture with S2a/authenticity, O1/data rights, `Deferred`, correction, classifier, expiry, outbound, MCP-principal, and A11 contracts.
5. Rerun UX validation after product and architecture converge, then finalize the spine pair only if the refreshed gate passes.

Until those steps complete, the safe UX contract is conservative and non-executable where evidence is missing: no machine approval, no classifier-bypass approval, no direct action from `Deferred`, no false `Corrected`, no blind outbound resend, no gate/readiness claim, and no relaxation of the six mandatory effect classes.
