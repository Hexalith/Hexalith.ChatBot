---
name: Hexalith.ChatBot UX Package Index
status: in-review
updated: 2026-09-16
decisionLog: .memlog.md
---

# Hexalith.ChatBot UX Package

This package is the current UX contract, but it is not yet a final or unqualified implementation handoff. The September 16 source reconciliations are applied, including the aligned architecture reference, and the selected rubric, accessibility, and Fluent/FrontComposer validation gate passes with no current findings. The maintained spines remain `in-review` pending source-defined release, ownership, platform, and live-evidence gates. The spine-only posture remains intentional; optional mock coverage and editorial polish are not release claims. Reading only one file is sufficient neither for implementation nor for surface acceptance.

## Maintained UX spines

1. [`DESIGN.md`](DESIGN.md) — visual identity, semantic tokens, component posture, and responsive visual rules.
2. [`EXPERIENCE.md`](EXPERIENCE.md) — information architecture, behaviors, states, interactions, accessibility, localization, responsive fallbacks, and journeys.

These two files are the maintained UX contract. They take precedence over older UX shorthand, mockups, wireframes, prototypes, and imports until intentionally revised.

## Binding supplements

1. [`m1-m2-surface-elaboration.md`](m1-m2-surface-elaboration.md) — M1/M2 surface assignment gate and product-contract mapping.
2. [`epic10-chat-surface-elaboration.md`](epic10-chat-surface-elaboration.md) — Project Workspace, governed composer, progressive response, and Stop/Cancel behavior.
3. [`implementation-conformance-addendum-2026-07-17.md`](implementation-conformance-addendum-2026-07-17.md) — Fluent UI v5 and FrontComposer composition, surface-local acceptance, and live-route evidence rules.

Supplements elaborate the maintained spines. A dated reconciliation may identify superseded supplement wording; a supplement does not override a newer product contract, an authorized decision in the canonical memlog, or the maintained spine pair.

## Reconciliation records

1. [`reconcile-product-contracts-2026-09-14.md`](reconcile-product-contracts-2026-09-14.md) — candidate product text, approval status, and the permanent six-effect approval boundary.
2. [`reconcile-architecture-2026-09-14.md`](reconcile-architecture-2026-09-14.md) — user-observable architecture consequences and disclosed product-versus-architecture divergences.
3. [`reconcile-existing-ux-inputs-2026-09-14.md`](reconcile-existing-ux-inputs-2026-09-14.md) — historical September 14 reconciliation of the three supplements.
4. [`reconcile-product-contracts-2026-09-16.md`](reconcile-product-contracts-2026-09-16.md) — current final-product and approved-addendum reconciliation, including source authority and superseded draft-era findings.
5. [`reconcile-architecture-and-change-2026-09-16.md`](reconcile-architecture-and-change-2026-09-16.md) — current finalized-architecture and approved-change reconciliation, including authority, gate, classifier, correction, and long-running-status deltas.
6. [`reconcile-existing-ux-inputs-2026-09-16.md`](reconcile-existing-ux-inputs-2026-09-16.md) — current supplemental and review-artifact reconciliation, including queue depth, replay isolation, outcome focus, intentional supersessions, and excluded-review facts.

The [canonical memlog](.memlog.md) records authorized UX decisions and update events. Reconciliation records explain how sources were applied; they do not create product approval or release qualification.

## Current validation evidence

- [`validation-report.md`](validation-report.md) and [`validation-report.html`](validation-report.html) — September 16 consolidated result for the selected rubric, accessibility, and Fluent UI V5/FrontComposer lenses: zero current findings at the UX-contract layer.
- [`review-rubric.md`](review-rubric.md) — spine coverage, completeness, inheritance, and canonical-shape review; zero current findings.
- [`review-accessibility.md`](review-accessibility.md) — WCAG 2.2 AA UX-contract review; zero current findings, with rendered conformance still subject to live-route evidence.
- [`review-fluent-conformance.md`](review-fluent-conformance.md) — Fluent UI V5 and FrontComposer contract review; zero current findings, with known platform gaps retained as qualification blockers.
- [`review-governance-safety.md`](review-governance-safety.md) — retained supplemental evidence for approval, authorization, omission, recovery, and safety from the earlier review snapshot; it was not rerun as part of the selected September 16 gate.

Validation evidence assesses the dated contract snapshot only. It does not make the UX package, capability, increment, implementation, or release final or qualified. Current source and architecture alignment is recorded by the September 16 reconciliations, while runtime evidence remains owned by the applicable qualification gates.

## Authority and conflict order

1. The formally final PRD and approved addendum, together with authorized decisions recorded in `.memlog.md`, govern product intent and accepted UX decisions.
2. `DESIGN.md` and `EXPERIENCE.md` are the maintained UX contract for how that intent looks and behaves.
3. The binding supplements provide dated surface and conformance detail unless a current source or reconciliation records a supersession.
4. The architecture spine is a downstream implementation reference. It may clarify implementation constraints but cannot override product intent, authorized UX decisions, or the maintained spines.
5. Reviews and validation reports are evidence about a snapshot, not normative authority and not proof of finality, implementation conformance, qualification, or release readiness.

The dated reconciliations are applied to the maintained spines, and the selected UX validation is complete with zero current findings. This package remains `in-review` until its source-defined release, ownership, shared-platform, and live-evidence gates close. Open qualification gates retain their runtime and claim effects independently of document status.
