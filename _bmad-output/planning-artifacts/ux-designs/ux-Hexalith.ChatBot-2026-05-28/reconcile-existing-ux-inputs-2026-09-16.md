---
title: Existing UX Input and Review Reconciliation — Hexalith.ChatBot
status: complete
created: "2026-09-16"
refreshed: "2026-09-16"
result: pass-with-revalidation-required
refreshes: reconcile-existing-ux-inputs-2026-09-14.md
sources:
  - DESIGN.md
  - EXPERIENCE.md
  - m1-m2-surface-elaboration.md
  - epic10-chat-surface-elaboration.md
  - implementation-conformance-addendum-2026-07-17.md
  - reconcile-existing-ux-inputs-2026-09-14.md
  - reconcile-product-contracts-2026-09-16.md
  - reconcile-architecture-and-change-2026-09-16.md
  - review-rubric.md
  - review-accessibility.md
  - review-governance-safety.md
  - review-fluent-conformance.md
  - validation-report.md
---

# Existing UX Input and Review Reconciliation — 2026-09-16

## Verdict

The maintained spine pair incorporates the three supplemental requirements that were not explicit in the September 14 reconciliation: operational queue depth, authorized `replay_run_id` isolation, and deterministic focus after user-triggered outcomes. The remaining Epic 10 and implementation-conformance requirements continue to be represented by the maintained spines and binding supplements.

This reconciliation refreshes, but does not alter or delete, [`reconcile-existing-ux-inputs-2026-09-14.md`](reconcile-existing-ux-inputs-2026-09-14.md). It also distinguishes the selected September 14 validation gate from the excluded historical Fluent review. The September 16 source reconciliations, including the aligned architecture reference, are applied. The spine pair remains `in-review` pending the new UX validation and any optional mock coverage or final polish selected during finalization.

## Supplemental closures

| Source requirement | Maintained-spine disposition | Result |
|---|---|---|
| S8 operational dashboards expose queue depth together with age, status, freshness, degraded dependency state, and role-owned triage (`m1-m2-surface-elaboration.md`, Surface Map). | `DESIGN.md` includes the queue/health name, current depth or exact health status, oldest-item age, owner, authorized detail, and freshness in Queue row anatomy. `EXPERIENCE.md` applies that exactness to operational dashboard behavior and acceptance without using depth as effect, qualification, or release evidence. | Closed |
| S9 explicitly revalidates replay isolation and `replay_run_id` treatment (`m1-m2-surface-elaboration.md`, Addendum Validation Notes). | `DESIGN.md` makes an authorized `replay_run_id` visible on replay entries. `EXPERIENCE.md` keeps replay-derived evidence isolated from canonical records and owner effects, applies authorization and omission before display, and verifies the identifier/isolation behavior in S9 acceptance. The identifier is opaque; its encoding remains an architecture concern. | Closed |
| Success, delayed, blocked, retryable, and terminal outcomes move focus to the appropriate status or error summary; S4 includes focus after correction (`m1-m2-surface-elaboration.md`, Surface Map and Story Acceptance Context). | `EXPERIENCE.md` moves focus once after the current actor's submitted action to the owning persistent status or error summary, preserves valid input and context, and prevents polling/background updates from stealing focus. S4 acceptance applies the rule to correction block, progress, delay, and completion. | Closed |

## Retained supplemental coverage

- Epic 10 retains the `/` Project Workspace landing, authorized Project picker and recents, distinct no-project-selected and empty-conversation states, governed human-message and AI-request entry, admission before streaming, immutable attempts, typed-state re-query, stable Stop/Cancel, polite `Response stopped`, deterministic focus return, reduced motion, and English/French parity.
- M1/M2 surfaces retain source-owned canonical states, authority and redaction boundaries, disabled-action reasons, responsive continuity, localization, deterministic S10 sorting, SLO and approval-quality observations, and surface-local live-route acceptance.
- Implementation conformance retains the Fluent UI v5 → FrontComposer → `DESIGN.md` → `EXPERIENCE.md` inheritance chain, one FrontComposer shell, exact available component bases, one owning accordion per qualifying surface, the sole Association Review carve-out, load-bearing stylesheet verification, and real-running-application evidence hierarchy.

## Intentional supersessions preserved

- The older Association Review `escalate` wording in the implementation-conformance surface summary permits safe escalation guidance; it does not create a durable association transition. `Deferred` still offers Resume to `NeedsReview` before confirm or reject.
- Older `idempotency windows` wording does not replace stable operation identity, recorded replay outcomes, immutable attempts, or source-defined successor semantics.
- Supplements do not restore superseded state spellings, permit machine approval, downgrade any of the six permanent human-approval effect classes, turn projection or advisory progress into owner-effect evidence, or broaden aggregate administration into Project-item authority.
- Current `DESIGN.md` and `EXPERIENCE.md` remain the maintained spines and win over conflicting older UX shorthand or future visual references until intentionally revised.

## Validation and historical-review scope

The September 14 consolidated validation selected the rubric, accessibility, and governance/safety lenses. It reported one High inheritance finding against that dated snapshot: the product revision was then treated as draft and the finalized architecture as reflecting an earlier contract. The approved September 15 change and current architecture reconciliation supersede that authority narrative, but only a new validation run can replace the dated result. Accessibility and governance/safety reported zero UX-contract findings for their selected snapshot.

`review-fluent-conformance.md` was not part of that selected gate and is historical evidence. Its unavailable component-name, missing surface-composition, and stale contingency-reference findings no longer reproduce in the maintained spines. Two facts remain useful but are not UX-spine blockers:

1. The July 17 implementation-conformance addendum names raw interactive elements and theme/control recreation but does not itself say that its automated Fluent-control guard rejects equivalent JavaScript or third-party widgets. `DESIGN.md` already prohibits those substitutions unless a reviewed no-equivalent exception exists. Aligning the dated addendum and its guard remains document/test hygiene outside this spine update.
2. The Product Brief's historical input registry omits the repository `references/` prefix and names dated Parties/Tenants brief files that are absent from the checkout. The maintained UX source paths resolve, and this historical provenance defect does not invalidate an already-reconciled UX interaction contract. Repair the upstream registry or record an immutable archive/unavailable-evidence disposition when that source is refreshed.

Therefore, the consolidated report's finding count must be read as the selected-lens result, not as a claim that every historical review observation was rerun or erased.

## Current disposition

- Keep `DESIGN.md`, `EXPERIENCE.md`, and the package index `in-review`.
- Preserve the conservative, fail-closed behavior and permanent six-effect human-approval boundary recorded in `.memlog.md`.
- Do not treat the open addendum-guard or Product Brief registry facts as UX-spine blockers.
- The September 16 source reconciliations are applied and introduce no remaining architecture-alignment blocker. Finalize only after the new UX validation passes and any optional mock coverage or final polish selected during finalization is resolved. Open qualification gates remain runtime and claim blockers according to their own source-defined effects.
