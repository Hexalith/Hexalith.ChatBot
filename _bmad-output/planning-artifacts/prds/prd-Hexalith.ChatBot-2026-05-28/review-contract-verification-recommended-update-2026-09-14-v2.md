---
title: Contract Verification — Recommended PRD Update — 2026-09-14 v2
status: complete
created: "2026-09-14"
reviewer: adversarial contract verifier
supersedes: review-contract-verification-recommended-update-2026-09-14.md
inputs:
  - reconcile-recommended-update-2026-09-14.md
  - reconcile-recommended-edit-map-2026-09-14.md
targets:
  - prd.md
  - addendum.md
---

# Contract Verification — Recommended PRD Update v2

## Gate verdict

**PASS for specification consistency: 15 resolved, 0 unresolved.** The autofixes close the prior C3, H6, and H11 residuals. The current PRD/addendum now provide one enforceable answer for every original 4 Critical + 11 High recommendation without renumbering FR/NFR IDs, transferring Conversations/Folders ownership to ChatBot, or weakening the evidence-gate model.

This verdict does not close A5, A6, A9a artifact qualification, A10, A11-M1, A11-M2, or A13. Those gates correctly remain evidence-bound and open in Current Release Status.

## Fifteen-item verdict matrix

| ID | Verdict | Current contract evidence |
| --- | --- | --- |
| C1 — Human-only MCP approval | **Resolved** | Human decisions require a current human-delegated MCP principal, recorded presence, `actorType=human`, and identity distinct from proposing/executing AI/tool/service principals. The AI/tool class has a closed query/proposal/eligible-low-risk set and cannot invoke approval decisions (`prd.md:543-547`, `prd.md:788-839`, FR41/FR42/FR83, NFR67). |
| C2 — Resume-only exit from `Deferred` | **Resolved** | The sole transition is `Deferred -> NeedsReview` through `ResumeEmailAssociationReview`; confirm/reject accept `NeedsReview` only and direct attempts from `Deferred` return `state-not-permitted` (`prd.md:492-527`, FR6). |
| C3 — Full correction impact completion | **Resolved** | Dual-Project authority, complete frozen manifest, source-owner commands/acknowledgements, irreversible-effect dispositions, non-terminal delay, AI blocking, and terminal-only completion are aligned across UJ4, the transition table, FR7/FR91a, NFR17a, A13, and the M0 gate. The manifest is now an explicit M0 ChatBot durable record with A6/isolation treatment and bounded ownership (`prd.md:426-430`, `prd.md:529-532`, `prd.md:605`, `prd.md:751`, `prd.md:1243`, `prd.md:1403`, `prd.md:1465`). |
| C4 — Split A11 M1/M2 gates | **Resolved** | Current Release Status, increment gates, A11, the gate-record contract, and Operating Baselines consistently make A11-M1 block M1 measurement/exit and A11-M2 block M2 SLO claims (`prd.md:100-108`, `prd.md:289-291`, `prd.md:1428`; `addendum.md:155-161`, `addendum.md:297+`). The override remains logged at `.memlog.md:52`. |
| H1 — Machine-evaluable gate approval record | **Resolved** | The immutable gate record binds candidate/dependency revisions, evidence hashes, environment, owner/independent approvers, expiry, revocation, reopen predicates, supersession, and the closed computed-status set; prose or file presence cannot close a gate (`prd.md:108`; `addendum.md:155-161`). |
| H2 — Policy snapshot owned in M0 | **Resolved** | Policy snapshot is an M0 record covering immutable bootstrap/M0 knobs, A6 treatment, and first-store isolation; M1 adds the editor-managed rows (`prd.md:603`, FR61). |
| H3 — Indeterminate classifier is a no-write denial | **Resolved** | `classifier-indeterminate` creates no proposal or durable idempotency state, cannot be approved, permits only the separate security-sensitive attempt record, and requires a new linked request after remediation (`prd.md:543`, FR39, NFR15a; `addendum.md:49-59`, retry profile). |
| H4 — Approval expiry and drift invalidation | **Resolved** | Exact default/maximum TTLs, `approved_at`/`expires_at`, digests and authority evidence, `Approved -> Expired`, material-drift invalidation, and new-proposal-only renewal are normative (`addendum.md:74-80`, FR42/FR50, NFR16/NFR36/NFR48). The product decision remains logged at `.memlog.md:53`. |
| H5 — Non-gameable association evaluation | **Resolved** | SM1, SM7, SM-C1, safe abstention, and wrong-association reporting consume the single protocol defining exclusive populations, minima/prevalence, independent adjudication, formulas, raw counts, and Wilson bounds (`prd.md:178-199`; `addendum.md:29-38`). Abstention cannot inflate correctness. |
| H6 — Operable pre-pilot data-rights workflows | **Resolved** | O1, the workflow table, RBAC, stable status/result queries, FR58, and retry rules cover actors, independent approval, partial completion, hold precedence, rejection/appeal, expiring redacted delivery, retry, and owner acknowledgements. FR75f now explicitly allows only the enumerated data-protection workflow family while denying collaboration/Project workflow operations (`prd.md:625`, `prd.md:574-580`, `prd.md:951-956`, FR58, `prd.md:1376`). |
| H7 — Unknown outbound-send reconciliation | **Resolved** | The lifecycle defines `SendOutcomeUnknown -> Reconciling -> Sent | NotSent | Unresolved`, provider evidence, four-hour escalation, no blind retry, and new-draft-only-after-`NotSent`. `ReconcileOutboundSendOutcome` and stable `GetOutboundSendStatus` expose command/query coverage (`prd.md:551`, `prd.md:887`, `prd.md:950`; `addendum.md:268-270`, retry profile). |
| H8 — Untrusted external/retrieved content | **Resolved** | Email, threads, attachments, filenames, retrieved Project context, model output, and tool results retain origin/trust labels and cannot define policy, tools, authority, recipients, command scope, or approval. Suspicious instructions fail safely, and A5/NFR9 require adversarial fixtures across every source class (`prd.md:94`, `prd.md:477`, FR27/FR33, NFR8/NFR9, A5; `addendum.md:82-86`). |
| H9 — Closed admission-stage profiles | **Resolved** | FR81a defines universal admission stages; the central pipeline selects one closed profile, unknown classes fail closed, and adapters/handlers cannot select, omit, reorder, or replicate stages. The addendum includes the closed profile matrix and operation registry (`prd.md:1389`; `addendum.md:163-196`). |
| H10 — Complete sender-authority tuple | **Resolved** | The provider-neutral tuple and all five M365 mappings require the same current tenant/mailbox/requester evidence intersection; missing, stale, or mismatched elements return typed denial and never broaden authority (`prd.md:1005`, FR48/FR50; `addendum.md:250-266`). |
| H11 — Separate inbound-authenticity workflow | **Resolved** | The pre-association state family, strict/paranoid behavior, terminality, successor-only reprocessing, mailbox-only redacted evidence, S2a, and retry semantics align. The autofix adds the M0 durable record, `GetInboundAuthenticityStatus`, and current `mailbox-admin` initiation plus independent `policy-admin` approval for review and reprocessing (`prd.md:488`, `prd.md:604`, `prd.md:621`, `prd.md:946`, `prd.md:958`, FR48d; `addendum.md:241-248`, retry profile). |

## Critical findings

None.

## High findings

None.

## Non-gating tail

- **Medium — evaluation taxonomy naming:** A9a still uses `deterministic-match` / `inbound-authenticity-anomaly`, while the authoritative protocol uses `one-authorized-project-unambiguous` / `authenticity-anomaly`. The sole protocol authority and formulas prevent a competing gate interpretation, but an explicit alias/migration note would reduce implementation ambiguity.
- **Medium — injection-fixture traceability:** A5/NFR9 already make source-class prompt/tool-injection fixtures mandatory. Repeating them in the FR39-FR46 readiness row and NFR67/NFR68 would improve reviewer navigation without changing the requirement.
- **Low — MCP class label:** `mcp-tool-client` is clearly defined as the AI/tool-bound principal. If kept for compatibility, identifying it as the stable legacy label would align terminology with the edit map.

## Stable-ID, ownership, and gate audit

- Base FR1–FR96 are contiguous and unique; all existing letter-suffixed FRs are unique.
- Base NFR1–NFR70 are contiguous and unique; all existing letter-suffixed NFRs are unique.
- No FR/NFR definition ID was removed relative to `HEAD`.
- `AcknowledgeAssociationCorrectionStore` remains stable as the legacy operation ID while its payload now acknowledges any frozen manifest item.
- ChatBot owns the correction manifest/orchestration, not Conversations/Folders records or repair effects; A13 still blocks use until owner contracts are accepted.
- `GetOutboundSendStatus` is the stable semantic equivalent selected for the proposed outcome query; it does not weaken reconciliation semantics.
- Gate state remains computed from immutable records. Specification closure does not imply evidence closure.

## Re-review conclusion

The original 15-item remediation bundle is ready to proceed to the remaining Finalize steps. The non-gating tail may be cleaned during polish, but it does not admit a materially weaker conforming implementation of any Critical/High recommendation.
