# Validation Report — Hexalith.ChatBot

- **DESIGN.md:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- **EXPERIENCE.md:** `/home/administrator/projects/hexalith/chatbot/_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- **Run at:** 2026-09-14T10:06:15+02:00

## Overall verdict

The updated spine pair is not ready to finalize as a downstream contract. It now has strong structural coverage—nine source journeys, explicit S1–S10 closure, 34 paired product components, a schema-valid DESIGN.md, and an explicit spine-only visual posture—but threshold wording, canonical state vocabulary, and classifier fallback remain ambiguous.

Accessibility is otherwise implementation-ready, and governance has a strong fail-closed foundation. Residual high-severity issues affect responsive reflow, batch approval, admin authority, suppressed-candidate confidentiality, and correction commit timing. The approved six-class mandatory-human-approval rule remains intact.

## Category verdicts

- Flow coverage — adequate
- Token completeness — adequate
- Component coverage — strong
- State coverage — thin
- Visual reference coverage — strong
- Bloat & overspecification — adequate
- Inheritance discipline — broken
- Shape fit — strong

## Findings by severity

### Critical (0)

No critical findings.

### High (8)

**[Rubric / Flow coverage] — Association threshold disposition is incomplete (§ `EXPERIENCE.md:34,174,295`; `addendum.md:22-23`)**

The UX sends only scores below `T_low` to `NeedsReview`, leaving `[T_low, T_high)` ambiguous. The normative rule sends every score below `T_high` to review; `T_low` controls reviewer presentation only.

Fix: use `score < T_high` for disposition everywhere and describe `T_low` only as presentation filtering or ordering.

**[Rubric / State coverage] — Canonical domain state names are not preserved (§ `EXPERIENCE.md:136-148`; `prd.md:483-545`)**

The state-family table introduces presentation labels and omits several authoritative families, allowing UI copy to be mistaken for domain state.

Fix: reproduce or directly reference every authoritative family and exact enum; keep user-facing labels in a separate mapping column.

**[Rubric / Inheritance discipline] — Indeterminate classifier write behavior conflicts (§ `EXPERIENCE.md:33,126,175,378`; `addendum.md:46`; `prd.md:1356`)**

The addendum normalizes indeterminate classification to approval-required, while NFR15a says no proposal record is written. The UX silently chooses a reviewable proposal when other preconditions are safe.

Fix: record an explicit approved reconciliation and align the durable-write behavior across sources and spines.

**[Accessibility] — Larger-screen handoff conflicts with WCAG Reflow (§ `DESIGN.md:149-155`; `EXPERIENCE.md:21-23,255-263`)**

Dense administration and investigation may require a larger screen, which can remove functionality at 320 CSS pixels or 400% zoom.

Fix: make handoff optional; keep every in-scope task readable and operable at 320 CSS pixels without page-level horizontal scrolling, except bounded essential two-dimensional content.

**[Governance & Safety] — Batch approval is broader than the current source permits (§ `EXPERIENCE.md:203`; `prd.md:1535-1540`)**

The comparison set omits frozen recipients, files, content digest, sender authority, tool target, approval revision, and expected resource revision, and it does not exclude prohibited effect classes.

Fix: require the complete frozen-field comparison and prohibit one-click batching for irreversible, external-send, file-exposing, external-tool, and on-behalf actions; audit each item atomically.

**[Governance & Safety] — Admin queue operations exceed the latest Project-authority boundary (§ `EXPERIENCE.md:53,179,205,325-330`; `prd.md:1391-1397`)**

The UX permits per-item queue actions at aggregate scope. Current requirements allow only opaque partition controls without Project authority; per-item mutation requires current Project authority and full revalidation.

Fix: separate aggregate partition controls from per-item controls and gate each per-item action on Project authority, requester authority, revision, policy, and audit readiness.

**[Governance & Safety] — Candidate parity can reveal suppressed resources (§ `EXPERIENCE.md:95,111,214`)**

The parity matrix promises the same “suppressed unsafe set,” contradicting the rule that no surface may confirm a forbidden candidate exists.

Fix: require equivalent omission/redaction semantics; never expose suppressed identity, evidence, ordering, or cardinality outside separately authorized audit evidence.

**[Governance & Safety] — Correction commit timing is ambiguous (§ `EXPERIENCE.md:176,313-319`; `prd.md:1478-1494`)**

The flow can imply a committed correction when the invalidation queue or canonical audit path is unavailable.

Fix: define a pre-commit failure that leaves the predecessor authoritative. Reserve `Correcting` and `Correction-delayed` for committed corrections awaiting downstream acknowledgement, with affected AI context blocked.

### Medium (10)

**[Rubric / Flow coverage] — UJ1 omits current suffix and surface identifiers (§ `EXPERIENCE.md:273`; `prd.md:313,568,1084,1117,1166`)**

Fix: map UJ1 through `FR28f` and name `S1a` explicitly in both the crosswalk and journey mapping.

**[Rubric / Token completeness] — Component frontmatter extensions are ignored by generic DESIGN.md consumers (§ `DESIGN.md:16-118`)**

The document is schema-valid, but `base`, `emphasis`, `distinction`, and `default` produce 68 linter warnings.

Fix: version these as a project extension or move the inheritance/anatomy metadata into Components prose and omit frontmatter component entries without local visual tokens.

**[Rubric / Inheritance discipline] — Display vocabulary and detector ownership drift (§ `EXPERIENCE.md:93,97,124,377`; `addendum.md:29-46`)**

Fix: map canonical internal values to user-facing labels explicitly and use the exact independently owned detector/artifact version.

**[Accessibility] — Operational auto-refresh lacks pause/apply behavior (§ `EXPERIENCE.md:105-106,179,182-205,240,250`)**

Fix: accumulate changes behind a keyboard-reachable update action while context is active, or provide pause/manual refresh; preserve the active item and announce one concise change summary.

**[Governance & Safety] — Mandatory approval is described as temporary and actor scope is broad (§ `DESIGN.md:215`; `EXPERIENCE.md:32,86,130`)**

Fix: make the six-class invariant permanent for AI-mediated actions and clarify that direct authorized human writes remain governed but do not automatically enter the AI-proposal flow.

**[Governance & Safety] — Task-intent detector failure and version contract drift (§ `EXPERIENCE.md:93,142,173`; `addendum.md:18-20,28-43`)**

Fix: use `detector_version`, add `detector-unavailable` to authorized review, and do not invoke the action-risk classifier before authorized task intent exists.

**[Governance & Safety] — Universal admin audit obligation is missing (§ `EXPERIENCE.md:102-103,177,205,321-332`)**

Fix: audit every allowed or rejected admin action, including qualifying reads; prohibit service clients and AI actors from assigning admins or changing policy.

**[Governance & Safety] — Identity evolution and human annotation evidence are underspecified (§ `EXPERIENCE.md:107,180,313-317,350-356`)**

Fix: show original identity plus unresolved-current-identity status and authorized review; label appended notes as non-authoritative and distinct from canonical evidence.

**[Governance & Safety] — Operational diagnostics are not runbook-complete (§ `EXPERIENCE.md:104-106,179`; `prd.md:1533`)**

Fix: add an authorized disclosure containing tenant, mailbox, workflow item, current state, last transition with actor/from-state, retry count, reason, correlation, and next action.

**[Governance & Safety] — Parity does not separate exposure from AI allowlisting (§ `EXPERIENCE.md:207-224`)**

Fix: show product operation catalog, per-surface exposure, and deny-by-default AI allowlist as separate memberships; parity applies only after actor/surface authorization succeeds.

### Low (1)

**[Governance & Safety] — Pre-A11 SLO view is named as a published dashboard (§ `DESIGN.md:196`; `EXPERIENCE.md:51,106,179,328`)**

Fix: call it the “SLO qualification backlog” until every target, error budget, live signal, route, and burn test is present; only then expose the published catalog.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-governance-safety.md`
