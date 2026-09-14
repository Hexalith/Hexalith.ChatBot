# Accessibility Review — Hexalith.ChatBot

## Overall assessment

**Adequate, with one high-impact conformance contradiction.** The two spines are substantially implementation-ready for WCAG 2.2 AA: they make keyboard/focus behavior, non-color status, generated-content provenance, streaming announcements, target size, forced colors, reduced motion, English/French parity, redaction, stale evidence, and explained permission denial explicit. No critical issue was found. The larger-screen handoff can, however, remove required M1/M2 information or actions at the narrow viewport used to assess Reflow. Two additional operational gaps leave auto-refresh and disconnected submissions underspecified for assistive-technology users.

## Findings

### High — Larger-screen handoff contradicts the WCAG 2.2 AA Reflow promise

- **Location:** `DESIGN.md` §Layout & Spacing, lines 149–155; `EXPERIENCE.md` §Foundation, lines 21–23; §Information Architecture, lines 42–59; §Responsive & Platform, lines 255–263; source `prd.md` §Accessibility and Usability Quality, lines 1522–1526.
- **Note:** The contracts require WCAG 2.2 AA for every shipped UI surface, including M1 administration and M2 dashboards, compliance investigation, and queue operations, while allowing dense administration and investigation to require a larger-screen handoff. A handoff is useful continuity, but it is not a substitute for SC 1.4.10 Reflow when a user at 320 CSS pixels wide, including a desktop user at 400% zoom, loses information or functionality. The two-dimensional-content exception can cover a bounded grid, not an entire workflow.
- **Fix:** Make handoff optional. Require every in-scope task to remain readable and operable at 320 CSS pixels without horizontal page scrolling or loss of content/actions. Linearize grids into labelled rows/details/steps where possible; allow two-dimensional scrolling only inside content whose two-dimensional layout is essential. Add 320-CSS-pixel and 400%-zoom acceptance to every M1/M2 surface.

### Medium — Operational auto-refresh lacks pause or explicit apply-update behavior

- **Location:** `EXPERIENCE.md` §Component Patterns, lines 104–116; §Per-surface coverage, lines 169–180; §Feedback and focus, lines 182–194; §Interaction Primitives, lines 202–205 and 240; source `prd.md` FR67, lines 1321–1322, and NFR42, line 1486.
- **Note:** Conversation/audit history is protected by a keyboard-reachable “new updates” affordance, and component refresh promises stable focus/selection. The queue and dashboard contracts do not say whether automatic insertions, removals, re-sorts, or metric updates pause while someone reads or operates a row. Because the source requires bounded-freshness refresh, an implementation could satisfy the spine while repeatedly changing content beside the user's current context, contrary to the control expected for non-essential automatically updating information under SC 2.2.2.
- **Fix:** Define a shared queue/dashboard update policy: accumulate changes behind a keyboard-reachable “new updates” action while a row, filter, or detail is active, or provide pause/manual refresh. Applying updates must preserve the active item and focus when safe, announce one concise change/result-count summary, and never silently remove or reorder the active row. Document any narrowly essential live-monitoring exception per surface.

### Medium — Browser disconnection and response-loss recovery are absent from the UI state contract

- **Location:** `EXPERIENCE.md` §Component Patterns, lines 83–116; §Canonical state families, lines 138–148; §Per-surface coverage, lines 169–180; §Feedback and focus, lines 182–194; source `prd.md` §Dependency Failure Handling, lines 1001–1007, FR81a, line 1355, and NFR70, line 1540.
- **Note:** The spines cover AI outage, server-side degraded states, stale evidence, revoked permissions, retries, and prior outcomes, but never name browser offline/disconnected or “submission accepted, response lost.” For a governed mutation or approval, downstream teams therefore lack a binding accessible status, focus behavior, draft/selection preservation rule, and reconciliation path. A generic retry can mislead users about whether the first request committed, even when backend idempotency prevents a duplicate effect.
- **Fix:** Add a client-connectivity state family and per-surface cases for disconnected-before-submit, disconnected-while-pending, and response-lost-after-admission. Preserve drafts, selections, filters, and focus; announce connection loss/recovery once in the scoped status region; issue and retain the stable operation identity before submission; reconcile through status lookup on reconnect before offering retry; state plainly whether an action was not sent, is pending/unknown, or returned a prior outcome.

## Strong commitments

- `DESIGN.md` lines 129–135 and 208–218 bind text/non-text contrast, visible focus, non-color meaning, dark mode, and forced-colors survival without redefining the inherited Fluent theme.
- `EXPERIENCE.md` lines 89–100, 141, and 188 make evidence freshness per-reference, text-labelled, announced once on expiry, and decision-blocking with an accessible reason.
- `EXPERIENCE.md` lines 95–96 and 202 define a proper single-tab-stop radiogroup with arrow navigation, announced position/count, programmatic evidence description, no commit on selection, and safe refresh invalidation.
- `EXPERIENCE.md` lines 113–115 and 233–240 provide strong busy-region, streaming/live-region, Stop/Cancel, dialog/sheet, scroll, and focus-return behavior.
- `EXPERIENCE.md` lines 244–250 set a real behavioral floor for landmarks, roles/names/states, reachable unavailable reasons, focus order, 24 CSS pixel minimum targets or spacing, 44 CSS pixel primary touch actions, and reduced motion.
- `EXPERIENCE.md` lines 251–253 require page and language-of-parts metadata, English/French visible and screen-reader parity, locale-aware formatting, expansion tolerance, and identical redaction across visual, copied, downloaded, and spoken output.
- `EXPERIENCE.md` lines 75, 100–112, 127–132, and 224–231 keep permission and authorization failures existence-neutral, visibly explained, non-overridable, and tied to a safe next action without leaking restricted resources.

## Finding counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 1 |
| Medium | 2 |
| Low | 0 |
| **Total** | **3** |
