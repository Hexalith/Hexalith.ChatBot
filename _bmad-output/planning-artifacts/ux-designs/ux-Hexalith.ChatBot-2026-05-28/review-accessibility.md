# Accessibility Review — Hexalith.ChatBot

## Overall verdict — adequate

The updated spine pair is now a detailed, mostly implementation-ready WCAG 2.2 AA behavioral contract. It closes all nine findings from the prior accessibility review and gives downstream teams unusually concrete rules for keyboard and focus behavior, screen-reader semantics, authoritative evidence, asynchronous state, approval safety, localization, redaction, and continuity during AI outage.

One direct tension prevents a strong verdict: the documents promise WCAG 2.2 AA for every shipped surface while allowing dense administration and investigation to require a larger screen, which can remove information or functionality at the narrow viewport equivalent used to assess Reflow. Automatically refreshed queue and dashboard data also lacks a user-controlled update policy. Severity counts: **critical 0 · high 1 · medium 1 · low 0**.

## Strengths

- Keyboard and focus behavior is implementable: the pair specifies one-tab-stop radiogroups, stable Stop/Cancel placement, contained and returned dialog focus, safe Escape behavior, focusable error summaries, preserved focus/selection, and Focus Not Obscured through persistent chrome at every breakpoint and at 200%/400% zoom (`EXPERIENCE.md:95-96, 100, 113-116, 182-194, 233-240, 247`).
- Screen-reader users receive the same decision evidence: source evidence is expanded and authoritative; AI summaries are collapsed, explicitly labelled, preceded by provenance, keyboard-operable, and structurally distinct; classifications, freshness, approval disposition, and redaction are programmatically associated rather than encoded by color (`EXPERIENCE.md:88-92, 97-100, 245-253`; `DESIGN.md:133-147, 175-190`).
- Evidence freshness and expiry are fully operationalized with per-reference timestamps, `fresh`/`stale`/`expired` labels, a single transition announcement, preserved focus, an explained unavailable decision, and Error summary recovery (`EXPERIENCE.md:89, 92, 141, 188`).
- Incremental AI output has a bounded announcement policy: content remains ordinary document content with `aria-live=off`, while a separate deduplicated status region announces only generation start, completion, stop, or failure for the current request (`EXPERIENCE.md:189, 233-240`).
- Correction and other long-running operations expose stable state, meaningful determinate progress only when a real value exists, textual indeterminate status otherwise, owner, estimate, next action, retry outcome, and focus-preserving completion (`EXPERIENCE.md:101, 104, 150-152, 186-194`).
- Bounded administration and two-person approval preserve accessible decision context: scopes are named without leaking project detail; changed values, authority, justification, version, expiry/conflict, and separation of duty are visible; self-approval and unavailable approval have reachable reasons (`EXPERIENCE.md:99-105, 130-132, 144, 175, 177`).
- Status and error recovery consistently keep durable state inline, deduplicate transient announcements, preserve valid input and selection, identify owner and safe next action, and use assertive announcements only for a failure caused by the current user's action (`EXPERIENCE.md:111-116, 182-194`).
- The visual contract sets explicit text/non-text contrast targets and requires text/icon/border meaning that survives light, dark, and forced-colors modes. Reduced motion removes shimmer, row movement, streaming animation, and non-essential transitions while retaining textual state (`DESIGN.md:129-135, 179-206`; `EXPERIENCE.md:248-250`).
- English/French parity covers visible and screen-reader strings, dates/numbers/plurals, French expansion, non-concatenated accessible text, page language, and language of parts for messages, summaries, and quotations (`EXPERIENCE.md:251-252`).
- Copy, read-aloud, transcript download, retention/export, and support bundles inherit the same redaction boundary as the visual surface; accessible names cannot hide source text, and external exposure requires explicit authorized human confirmation (`EXPERIENCE.md:109-110, 147, 253`).
- AI outage is scoped rather than presented as a total outage: manual association/correction, existing-proposal decisions, deterministic classification, mailbox retry, status, and audit remain available when their own dependencies are healthy (`EXPERIENCE.md:73, 148, 173, 283, 382`).

## Findings by severity

### Critical (0)

No critical findings.

### High (1)

- **[high] The larger-screen handoff conflicts with the pair's WCAG 2.2 AA Reflow commitment.** Foundation, visual layout, and the phone contract allow dense Tenant Administration and Compliance Investigation functionality to leave the narrow viewport and require a larger-screen handoff (`EXPERIENCE.md:21-23, 255-263`; `DESIGN.md:149-155`). Yet the same spine requires WCAG 2.2 AA on every shipped surface and explicitly includes these M1/M2 surfaces (`EXPERIENCE.md:59, 169-180, 242-253`; source `prd.md:1574-1578`). A state-preserving link is useful continuity, but it is not an AA substitute when a user at 320 CSS pixels wide—or at 400% zoom on a desktop viewport—loses information or functionality. *Fix:* make the handoff optional. Require every in-scope administration and investigation task to remain readable and operable at 320 CSS pixels without horizontal page scrolling or loss of content/actions. Permit two-dimensional scrolling only inside content whose two-dimensional layout is genuinely essential, such as a bounded data grid; otherwise linearize it into labelled rows, details, or steps. Add explicit 320-CSS-pixel/400%-zoom acceptance for every M1/M2 surface.

### Medium (1)

- **[medium] Automatically refreshed queue and dashboard data has no pause/apply-update contract.** The pair preserves focus and selection on refresh and safely defers new conversation/audit items behind a “new updates” control, but it still permits a generic observed change to update inline and does not govern automatic row insertion, removal, or reordering in operational queues and dashboards (`EXPERIENCE.md:105-106, 112, 116, 179, 182-194, 204-205, 240, 250`; source `prd.md:1383-1384, 1538`). Focus stability and reduced motion do not prevent an assistive-technology user's reading context from changing underneath them, and WCAG 2.2's Pause, Stop, Hide criterion applies when automatically updating information starts automatically alongside other content unless the update is essential. *Fix:* define one consistent policy: queue/dashboard updates accumulate behind a keyboard-reachable “new updates” action while a row, filter, or detail is active, or users can pause automatic refresh and refresh manually. Applying updates must preserve focus/selection when safe, announce one concise result-count/change summary, and never silently remove or reorder the active item; document any narrowly essential live-monitoring exception per surface.

### Low (0)

No low-severity findings.

## Coverage notes

| Review area | Coverage | Note |
|---|---|---|
| Keyboard and focus, including Focus Not Obscured | Strong | Stable controls, predictable focus movement/return, error landing, dialog containment, and obscuration rules are explicit. |
| Screen-reader semantics | Strong | Roles, names, states, landmark names, unavailable reasons, generated-content provenance, and safe descriptions are contracted. |
| Source evidence versus AI summary | Strong | Authority, default disclosure state, DOM/reading order, provenance, and non-color distinction are explicit. |
| Freshness and expiry | Strong | Per-reference state/timestamp, single announcement, approval block, reason, and recovery are present. |
| Association radiogroup | Strong | Group name, one Tab stop, arrows, position/count, evidence description, no commit on selection, refresh invalidation, and error focus are present. |
| Incremental output and live regions | Strong | Stream content is not live; state announcements are separate, scoped, deduplicated, and bounded. |
| Correction progress | Strong | Progress semantics, acknowledgements, delayed-state owner/action, AI block, and focus preservation are explicit. |
| Bounded admin and two-person approval | Strong | Scope separation, distinct principals, changed values, version/conflict, unavailable reason, and audit link are visible. |
| Status, errors, and recovery | Strong | Inline durable status, safe codes, owner/action, selection preservation, retry/prior-outcome distinction, and announcement urgency are specified. |
| Reduced motion, dark mode, and forced colors | Strong | Functional meaning and focus survive theme modes; motion never carries status alone. |
| English/French and language of parts | Strong | UI and assistive-text parity, locale formatting, expansion, page language, and mixed-language content are addressed. |
| Responsive touch and reflow | Thin | Touch targets and labelled grid reflow are strong, but mandatory larger-screen handoff conflicts with AA Reflow. |
| Redaction, export, and support bundles | Strong | Visual, copied, spoken, downloaded, and shared output use one redaction policy with explicit approval for exposure. |
| AI-outage continuity | Strong | Surviving non-AI workflows and the blocked generation scope/recovery are named. |
| Automatic operational updates | Adequate | Conversation/audit history is safe; queue/dashboard refresh needs a pause or explicit apply-update policy. |
