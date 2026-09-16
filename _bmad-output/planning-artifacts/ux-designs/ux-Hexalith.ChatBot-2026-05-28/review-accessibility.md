# Accessibility Review — Hexalith.ChatBot UX

## Overall assessment

**Verdict: PASS — no current accessibility-contract findings.** The four gaps from the prior review remain closed in the maintained `DESIGN.md` and `EXPERIENCE.md`. The Fluent-boundary clarifications also preserve an accessible compact-grid status exception and safely qualification-block French destructive-dialog flows until FrontComposer can localize all framework and generated copy. A regression scan against [WCAG 2.2 AA](https://www.w3.org/TR/WCAG22/) found no new gap across keyboard, screen reader, reflow, forced colors, reduced motion, English/French parity, focus, announcements, or redaction.

This verdict evaluates the UX spine contract, not rendered implementation conformance. Release evidence still has to exercise the real routes and actual accessibility tree as required by `EXPERIENCE.md`.

**Finding count:** 0 critical, 0 high, 0 medium, 0 low.

## Findings

No current findings.

## Prior-finding closure

| Prior finding | Current contract evidence | Result |
|---|---|---|
| A11Y-01 — transcript item/navigation semantics | `DESIGN.md:138` requires a uniquely named transcript region, semantic item/navigation structure, stable identity/sequence/source/state, origin relations, and a first-unseen target. `EXPERIENCE.md:133` supplies the matching behavior; `EXPERIENCE.md:365` and `:387` require accessibility-tree, keyboard, and screen-reader assertions plus bounded older/newer navigation and no forced scroll. The permitted `list|feed|log|equivalent` implementation choice is bounded by one common testable contract, including the separate no-stream-chunk-announcement rule. | Closed |
| A11Y-02 — nonvisual busy/skeleton contract | `DESIGN.md:175` and `EXPERIENCE.md:170,251` put `aria-busy` on the named owning region, exclude decorative skeletons, require one localized deduplicated start/terminal status, preserve focus/layout, and define deterministic clearing for success, empty, error, blocked, cancellation, and context replacement. `EXPERIENCE.md:366,383` require live-route accessibility-tree and announcement evidence. | Closed |
| A11Y-03 — field-level error semantics | `DESIGN.md:176` and `EXPERIENCE.md:171,309-310` require each invalid control to expose invalid state and stable localized error/help descriptions, with summary-to-control focus, valid-input preservation, and stale-association cleanup. `EXPERIENCE.md:367,383` makes those assertions part of every applicable live validation state. | Closed |
| A11Y-04 — session expiry and reauthentication ownership/recovery | `EXPERIENCE.md:313-317` assigns session lifetime and warning/extension policy to the configured identity provider, challenge/return bridging to FrontComposer, and eligible draft/selection preservation plus authorization, tenant, Project, evidence, revision, route, and focus recovery to ChatBot. `EXPERIENCE.md:388` requires warning and immediate-challenge variants, revalidation, ineligible-state discard, and safe focus recovery. | Closed |

## Fluent-boundary clarification checks

| Boundary | Current contract evidence | Result |
|---|---|---|
| Generated compact-grid status | `DESIGN.md:90,122,132` limits the exception to generated `[ProjectionBadge]`/`FcStatusIcon` cells and requires a shape-distinct glyph, contextual accessible name, keyboard/hover tooltip, and status text in responsive labeled records. `EXPERIENCE.md:127` carries the same behavior, while `EXPERIENCE.md:364` requires a localized accessible name plus accessibility-tree and keyboard evidence. Meaning therefore does not depend on color, hover, or an untranslated glyph name. | Pass |
| French destructive-dialog qualification | `DESIGN.md:177` and `EXPERIENCE.md:172` keep `IDialogService`/`FcDestructiveConfirmationDialog` ownership, prohibit a ChatBot fork, and block the French path until framework and generated copy is localizable. `EXPERIENCE.md:379,383,405` enumerates the missing Cancel/default Confirm/default body and host/domain title/body/label seams, repeats the qualification block in live acceptance, and preserves localized second-opener status plus deterministic focus. The contract fails closed rather than shipping a mixed-language destructive decision. | Pass |

## Regression scan

- **WCAG 2.2 AA and keyboard:** `EXPERIENCE.md:363-374` covers programmatic names, semantic widgets, visible-order focus, skip links, focus-not-obscured behavior, non-conflicting composer shortcuts, 24×24 CSS-pixel target sizing or valid exceptions, and 44×44 touch-primary targets. No defined interaction requires dragging as its sole input.
- **Screen reader and status:** `EXPERIENCE.md:364-371` separates compact status accessible naming and transcript navigation from one scoped status announcer, forbids streamed chunks, polling ticks, count ticks, background rows, and historical content from live announcement, and reserves assertive output for a block caused by the current action.
- **Focus and recovery:** `EXPERIENCE.md:309-310,368-371` defines error, outcome, Project-switch, proposal, dialog, refresh, correction-return, and safety-interruption landing/return behavior. `EXPERIENCE.md:299` prevents controlled background updates from moving focus or silently reordering the active item.
- **Reflow and text spacing:** `DESIGN.md:110,193` and `EXPERIENCE.md:373,401-405` keep every task complete at 320 CSS pixels and 400% zoom, preserve control order, replace nonessential wide grids with labeled records, bound essential two-dimensional scrolling, and require text-spacing resilience and French expansion.
- **Forced colors and reduced motion:** `DESIGN.md:88-92,196` requires visible text/icon/border semantics outside generated grids and a distinct non-color glyph inside compact generated cells. `EXPERIENCE.md:372-374,383` removes skeleton shimmer, streaming-cursor/typing animation, animated row movement, and nonessential dialog/toast transitions while preserving textual state and real determinate values.
- **English/French and language metadata:** `EXPERIENCE.md:377-379` requires feature, state, action, unavailable-reason, and screen-reader parity; locale persistence; language-of-parts metadata; locale-aware dates/numbers/durations/plurals; complete accessible strings; and fail-closed French qualification where inherited destructive-dialog copy is not localizable.
- **Redaction and privacy:** `EXPERIENCE.md:335-339` applies authorization/redaction before content enters the DOM or accessibility tree and explicitly covers names, descriptions, live regions, collapsed content, grid metadata, clipboard, transcript/download, export, and read-aloud output without leaking removed values, ordering, or cardinality.
- **Per-surface evidence:** `EXPERIENCE.md:381-399` requires real-route automated, keyboard-only, and screen-reader evidence for every exposed loading, empty, validation, unauthorized/redacted, degraded, offline/pending-unknown, retryable, and terminal state, including reflow, text spacing, target size, forced colors, reduced motion, and English/French behavior.

## Strengths

- The transcript contract now distinguishes navigable history from live status, handles bounded windows and unseen updates, and keeps streaming chunks outside live regions.
- The compact-grid status exception is narrowly scoped and preserves non-color, keyboard, screen-reader, localization, and responsive-text evidence without duplicating a badge pill.
- Busy, validation, and outcome patterns pair programmatic state with deterministic focus and announcement cleanup instead of relying on visual Fluent components alone.
- Session recovery explicitly revalidates authority and context before restoring state, preventing an accessible return path from becoming an authorization leak.
- Focus behavior is unusually complete across Project switching, proposal surfacing, dialogs, validation, correction return, list refresh, connectivity, and long-running attempts.
- The redaction boundary explicitly includes assistive-technology metadata and read-aloud/export channels, not only visible content.
- French destructive-dialog qualification is explicitly fail-closed at the shared platform boundary rather than worked around with a nonconformant product-local control.
- Accessibility acceptance is surface-local and state-complete rather than deferred to one final regression story.

## Mechanical notes

- Every `sources`, `supplementalSources`, `implementationReferences`, `reconciliationSources`, and `decisionLog` path declared by both spines resolves. The dated September 16 reconciliations supersede stale September 14 authority narratives without weakening their retained accessibility requirements.
- No `imports/`, `mockups/`, or `wireframes/` directory exists, so there was no visual artifact to inspect for reading order, contrast, target size, responsive transformation, or focus visibility.
- No `[ASSUMPTION]`, `[NOTE FOR UX]`, open-question, TODO, TBD, or FIXME marker remains in either spine.
- FrontComposer, Fluent UI v5, and the configured identity provider remain implementation dependencies. Their actual focusable nodes, localized accessible names/tooltips, shape distinctions in forced colors, busy/invalid propagation, challenge pages, modal arbitration, dialog localization, and live regions must be verified through the real-route evidence required by `EXPERIENCE.md:381-399`; this is an implementation/qualification-evidence note, not a UX-spine finding.
