---
title: Hexalith.ChatBot UX Reconciliation — Architecture and Approved Change
status: in-review
created: "2026-09-16"
updated: "2026-09-16"
scope: "Current architecture spine and approved 2026-09-15 planning-authority correction reconciled into the maintained UX contracts"
sources:
  - ../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md
  - ../../sprint-change-proposal-2026-09-15.md
  - .memlog.md
  - DESIGN.md
  - EXPERIENCE.md
---

# Architecture and approved-change reconciliation

## Verdict

The approved 2026-09-15 change establishes that the PRD is final and its addendum approved, and that downstream artifacts must conform without reopening product scope (`../../sprint-change-proposal-2026-09-15.md:112-116`, `:234-247`). The current architecture spine incorporates that correction, is `status: final`, and records the approved change as an input (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:8-10`, `:21-30`). The 2026-09-16 spine correction removed the obsolete product-draft and pre-correction architecture-mismatch narrative and preserved the fail-closed interaction contracts (`EXPERIENCE.md:28-34`, `:54-61`). Architecture alignment is current and resolved for this reconciliation; validation and qualification/implementation evidence remain separate.

The UX decision log remains authoritative for the accepted approval boundary: all six AI-mediated boundary-effect classes are permanently human-approval-required, tenant policy cannot downgrade them, and a direct authorized human command follows its own command and audit contract (`.memlog.md:11`). The architecture agrees that state modification, file disclosure, external send, task creation or assignment, external-tool invocation, and action on behalf of a participant have no downgrade path (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:193-206`).

This reconciliation changes no visual identity or inherited component system. It records the behavioral, status, provenance, and qualification deltas applied to the 2026-09-16 `DESIGN.md` and `EXPERIENCE.md`. The source-mandated classifier identity, correction ownership, long-running timing, gate-set, and categorical-risk clauses are now explicit in the maintained spines.

## Applied deltas

| Area | Current source decision | Applied result in the maintained UX spines |
| --- | --- | --- |
| Product and addendum authority | The approved change treats the finalized PRD and approved addendum dated 2026-09-14 as normative and resolves the product-source approval blocker (`../../sprint-change-proposal-2026-09-15.md:112-116`, `:238-245`). The architecture uses the same authority hierarchy (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:65-80`). | Applied. Foundation now names the finalized PRD and approved addendum as product authority, and the blocker table states that product approval is resolved (`EXPERIENCE.md:28-34`, `:54-61`). Both spines remain `in-review` for validation, not because product authority is unresolved (`DESIGN.md:1-6`; `EXPERIENCE.md:1-5`). |
| Architecture alignment | The approved change permits an architecture-alignment blocker only until the maintained architecture surfaces implement the correction and pass revalidation (`../../sprint-change-proposal-2026-09-15.md:244-251`). The declared implementation reference contains the corrected classifier, correction, and release-gate contracts (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:189-225`, `:240-265`, `:306-348`). | Applied and resolved. The stale mismatch claim is gone; architecture is described as the current downstream implementation reference subordinate to product intent (`EXPERIENCE.md:34`). The remaining downstream row concerns producer-owner, audit-boundary, identity-evolution, operational, and implementation evidence—not a product-versus-architecture contract conflict (`EXPERIENCE.md:54-61`). |
| A9a and increment sequencing | Exact A9a detector/classifier records gate first use at M0, are revalidated at M1/M2, A11-M1 independently blocks M1, and A10 plus A11-M2 additionally block M2 after lower-gate revalidation (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:306-341`; `../../sprint-change-proposal-2026-09-15.md:147-160`, `:218-232`, `:242-247`). | Applied. The increment table now uses A9a, makes A11-M1 a blocking M1 condition, keeps A10/A11-M2 at M2, and preserves lower-gate revalidation (`EXPERIENCE.md:44-50`). |
| Immutable gate-set authority | Release Governance owns an immutable per-candidate/increment `gate_set_id`; incomplete, expired, invalidated, superseded, or mixed-candidate sets fail closed, and CI/release plus runtime enablement consume the same set (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:342-348`). | Applied. Qualification terminology, increment availability, component behavior, state vocabulary, and operational acceptance bind to the published `gate_set_id` and immutable record provenance (`EXPERIENCE.md:40-50`, `:123-155`, `:230-237`, `:361-375`; `DESIGN.md:127-159`, `:173-184`). |
| Categorical action risk and applicable confidence | A successful `ActionRiskClassifier` result is categorical: `low-risk` or `approval-required`; indeterminate artifact, output, tag, effect-surface, or authority cases return `classifier-indeterminate` (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:189-212`; `../../sprint-change-proposal-2026-09-15.md:122-137`). Association and task-intent evidence retain their own scoring/qualification contracts (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:193-220`). | Applied. Action classification is categorical, association/task-intent evidence owns confidence, and both queue-row contracts state that numeric confidence never represents action risk (`DESIGN.md:134-146`, `:159`; `EXPERIENCE.md:130-142`, `:155`, `:178-189`, `:239-241`). |
| Indeterminate classifier operation identity | `classifier-indeterminate` creates no proposal, domain state, durable idempotency state, approval action, or effect. The separate redacted attempt records the original `operation_id` and classifier evidence; remediation uses a fresh `operation_id` with an immutable predecessor link. The attempt ledger may deny reuse but cannot authorize continuation (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:198-210`; `../../sprint-change-proposal-2026-09-15.md:191-205`). | Applied. The outcome row records the original attempt identity, prohibits the attempt ledger from authorizing continuation or serving as domain idempotency, and requires remediation to use a fresh `operation_id` with an immutable predecessor link and determinate reclassification (`EXPERIENCE.md:178-189`). |
| Correction manifest and ownership | The ChatBot correction aggregate solely owns immutable manifest membership and lifecycle. Every stable item belongs to the complete cross-owner/effect manifest and may be acknowledged or dispositioned only by its A13-mapped authenticated owner adapter/actor; coordinator, projection, and UI cannot self-acknowledge or change membership (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:240-265`; `../../sprint-change-proposal-2026-09-15.md:139-145`, `:206-217`). | Applied. Correction components and state behavior name stable item identity, ChatBot aggregate membership/lifecycle ownership, the A13-mapped owner actor restriction, complete-manifest completion, AI-context blocking, and `CorrectionDelayed` recovery/escalation (`DESIGN.md:151`; `EXPERIENCE.md:147`, `:254-262`). |
| Long-running operation timing | Operation identity and current status return within five seconds p95. After 30 seconds, clients receive a retrievable status containing retry count, partial-output marker, terminal reason, next safe action, and correlation; advisory progress never proves a commit (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:392-410`). | Applied. Shared operation status, governed-chat behavior, and S1a acceptance carry the five-second p95 and after-30-seconds detailed-status contract while preserving authoritative re-query and uncommitted-stream semantics (`DESIGN.md:157`; `EXPERIENCE.md:153`, `:243-275`, `:365`). |

## Preserved alignment

- The FrontComposer shell and Fluent UI v5 remain the single inherited visual/component system; WCAG 2.2 AA, non-color status, keyboard-only review, screen-reader review, and source-evidence-versus-AI-summary distinction remain binding (`DESIGN.md:73-81`, `:103-110`; `EXPERIENCE.md:340-375`; `../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:411-416`).
- The classifier outcome table keeps `classifier-indeterminate` distinct from determinate classes, denial, and unsupported work; it offers no approval action or effect and treats `classifier-unavailable` only as a non-canonical reason (`EXPERIENCE.md:178-188`; `../../sprint-change-proposal-2026-09-15.md:236-247`).
- The UX uses exact `CorrectionDelayed`, places M0 correction in S2 with S4 as its M1 extension, and withholds `Corrected` until the frozen manifest is complete (`DESIGN.md:145-151`, `:183`; `EXPERIENCE.md:44-50`, `:141-147`, `:254-262`, `:426-435`; `../../sprint-change-proposal-2026-09-15.md:139-145`).
- Coordinator/admission, owner effect, delivery/provider, and projection/investigation remain separate status lanes. Partial streamed text, advisory progress, a queue row, or a projection never proves a committed effect (`EXPERIENCE.md:243-275`; `../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:376-410`).
- Current owner authority, omission parity, auditable restricted reads, and fail-closed runtime controls remain unchanged (`EXPERIENCE.md:288-338`; `../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:108-133`, `:267-283`, `:441-454`).
- The permanent six-effect approval rule from `.memlog.md` is preserved without converting direct authorized human commands into AI proposals (`.memlog.md:11`; `DESIGN.md:179-183`; `EXPERIENCE.md:52`, `:169-188`).

## Dropped or superseded ideas

No qualitative visual idea, user-supplied design direction, mockup, or brand choice was dropped: neither source introduces one.

The following stale interpretations were removed or superseded in the maintained spines:

- the current PRD/addendum described as draft or awaiting product-source approval;
- the corrected architecture described as still using the pre-final classifier, correction-state, or A11 contract;
- standalone `A9` where the finalized gate is `A9a`;
- an unsplit A11 or an M2-only A11 interpretation;
- numeric “confidence” presented as an ActionRiskClassifier output; classifier-specific and queue content now keep risk categorical and restrict confidence to association/task-intent evidence;
- classifier remediation that resumes, approves, or reuses the indeterminate attempt identity;
- correction completion inferred from a projection, partial acknowledgement set, or coordinator assertion; and
- local/copied gate booleans treated as release or runtime authority.

## Remaining blockers

### UX validation and status

The spine edits are applied, but `DESIGN.md` and `EXPERIENCE.md` remain `status: in-review` until a new dated UX validation/reconciliation pass succeeds. Only that passing result may promote them to `final`/binding. Historical reviews remain snapshots and must not be rewritten as current evidence (`DESIGN.md:1-6`; `EXPERIENCE.md:1-5`; `../../sprint-change-proposal-2026-09-15.md:249-253`, `:302-315`, `:317-325`).

### Qualification and release authority

Architecture finality does not close qualification. A5, A6, A9a, A13, A11-M1, A10, and A11-M2 remain open in the current architecture ledger: live AI/onboarding, detector/classifier use, the complete M0 loop, the M1 governed-pilot claim, recovery qualification, and the M2 production/release-candidate claim remain blocked according to their own gate effects (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:566-580`). UX may specify the accessible blocked and recheck experience, but it must not claim that a visible surface, adopted architecture, document finality, or partial evidence closes any gate (`../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md:306-348`).

### Surface closure

The earlier correction-surface placement gap is resolved in the spines: M0 correction is S2-owned, S1 only deep-links, and M1 S4 extends the same contract (`DESIGN.md:144-151`, `:183`; `EXPERIENCE.md:44-50`, `:140-147`, `:254-262`, `:361-368`, `:425-434`). Validation still must confirm that every source-mandated status, qualification, ownership, timing, and recovery fact has a surface and journey.

## Handoff disposition

The recorded spine deltas are applied and this file is listed as a dated reconciliation source (`DESIGN.md:17-23`; `EXPERIENCE.md:16-22`). Preserve the historical reconciliation artifacts and run the selected UX validation lenses. Until validation passes, the safe contract remains fail-closed and `in-review`; open release gates remain qualification, implementation, runtime, and claim blockers even after the UX documents themselves become final.
