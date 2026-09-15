# Accessibility Review — Hexalith.ChatBot

## Overall verdict

**PASS at the UX-contract level.** No reproducible accessibility finding remains in the current `DESIGN.md` and `EXPERIENCE.md`. All ten concerns from the preceding review remain resolved, and the landing, shortcut, live-route, deterministic sorting, structure, and prose edits introduce no S1, S1a, S8, or S10 regression.

Finding counts: **critical 0 · high 0 · medium 0 · low 0**.

This review validates the UX contract, not an implementation. Each delivered surface still needs the specified running-application, server-verified, automated, keyboard-only, screen-reader, responsive, localization, forced-colors, and reduced-motion evidence (`EXPERIENCE.md:322-340`).

## Findings by severity

### Critical (0)

None.

### High (0)

None.

### Medium (0)

None.

### Low (0)

None.

## Prior-finding regression check

| Prior concern | Current disposition | Evidence |
|---|---|---|
| Phased S2a/O1 coverage | Resolved | S2a and O1 remain in scope, IA, composition, per-surface acceptance, and journeys. Their blocked/authorized behavior is available nonvisually without Project-data leakage (`EXPERIENCE.md:39-80,84-100,326-340,376-388,425-437`). |
| Per-surface acceptance evidence | Resolved | Every delivered surface requires a real live route through the running application, server-verified primary success, direct functional assertions, automated checks, keyboard-only review, screen-reader review, and its full exposed state set. Fixtures and handler-only tests cannot substitute (`EXPERIENCE.md:322-340`). |
| English/French and language metadata | Resolved | Feature/state/action/reason/screen-reader parity, locale persistence, root/page and language-of-parts metadata, locale-aware formatting, untranslated stable codes, complete messages, and French expansion are explicit (`EXPERIENCE.md:318-320`). |
| Focus, live regions, Project switch, proposal, and composer shortcuts | Resolved | Current-attempt announcements are scoped/deduplicated, streamed chunks stay outside live output, Stop/Cancel has stable focus behavior, Project switch targets the new-context heading, proposals link to their origin, and modifier-free shortcuts are suppressed during entry without interfering with text or assistive-technology commands (`DESIGN.md:127`; `EXPERIENCE.md:123,235,308-315,328-329,354-364`). |
| Accessibility-tree redaction | Resolved | Authorization/redaction runs before DOM or accessibility-tree exposure and covers names, descriptions, status regions, hidden content, grid metadata, clipboard, transcript/download, export, and read-aloud output (`EXPERIENCE.md:280-284`). |
| Reflow, focus visibility, target size, and text spacing | Resolved | Tasks remain complete at 320 CSS pixels/400% zoom without page-level horizontal scrolling; focus remains visible above chrome/overlays; the AA target floor and touch-primary target apply; text-spacing overrides cannot hide or clip content/actions (`DESIGN.md:98-104,174-178`; `EXPERIENCE.md:308-315,342-346`). |
| Reduced motion | Resolved | Skeleton shimmer, streaming cursor/typing animation, row movement, and nonessential dialog/toast transitions are removed while state text and real determinate values remain (`EXPERIENCE.md:312-315`). |
| Controlled updates and sorting | Resolved | Decision updates wait behind an accessible apply action, one concise summary is announced, active identity/focus is preserved when safe, and removed items are explained. Queue filtering adds explicit server-side sort with deterministic tie-break while preserving omission-safe count/order, selection, focus, pagination, and narrow-screen reflow (`DESIGN.md:159,164`; `EXPERIENCE.md:155-160,252-254,337-339`). |
| Unavailable-action reasons | Resolved | High-consequence unavailable actions use `aria-disabled` plus a programmatically associated reason or adjacent focusable explanation; forbidden actions remain absent when presence would leak capability. Invalid activation lands on the error summary without losing valid input (`EXPERIENCE.md:138,256-263`). |
| Connectivity and recovery | Resolved | Before-submit disconnect, pending-unknown, response-loss reconciliation, and restored status remain distinct; duplicate submission is prevented and focus/selection is preserved (`EXPERIENCE.md:233-244`). |

## New-text regression check

- **S1 landing:** `/` now has an existence-neutral authorized Project picker/recents, distinct no-project-selected and empty-conversation states, gated composer availability with an accessible M1 reason, and non-forcing chronology/new-update behavior (`EXPERIENCE.md:62,88,328`).
- **S1a composer:** text entry suppresses single-character/modifier-free application shortcuts without suppressing text or assistive-technology commands; draft preservation, attempt announcements, Stop/Cancel return focus, and proposal-link focus remain deterministic (`DESIGN.md:127`; `EXPERIENCE.md:123,235,329`).
- **S8 dashboard:** qualification, freshness, informational approval-load/quality observations, audited reads, controlled updates, aggregation-only scope, and no-approval-bypass semantics remain explicit and non-color-dependent (`DESIGN.md:82,152`; `EXPERIENCE.md:144,250-254,304,337`).
- **S10 queue:** filters, deterministic server sort, pagination, partition/item authority, diagnostics, retry/terminal states, and active-context preservation remain keyboard/screen-reader compatible under refresh and reflow (`DESIGN.md:149,159,164`; `EXPERIENCE.md:147,155,160,254,339`).

## Strengths

- The pair uses semantic inherited Fluent UI/FrontComposer controls, exact per-surface composition, one safe Association Review carve-out, one radiogroup decision model, focusable error recovery, and modal focus return (`DESIGN.md:116-164`; `EXPERIENCE.md:82-100,119-160`).
- Meaning survives light, dark, and forced-colors modes through text plus icon/border; functional muted text, controls, and focus indicators retain WCAG 2.2 AA contrast (`DESIGN.md:78-84`).
- Source evidence, AI interpretation, canonical records, projections, partial output, qualification, authority, and restricted content remain explicitly labeled rather than communicated through layout or color alone (`DESIGN.md:90-96`; `EXPERIENCE.md:27-31,308-316`).
- Upstream-blocked transitions retain accessible unavailable states and acceptance tests without inventing missing authority or presenting a larger-screen handoff as required (`EXPERIENCE.md:47-54,322-346`).
