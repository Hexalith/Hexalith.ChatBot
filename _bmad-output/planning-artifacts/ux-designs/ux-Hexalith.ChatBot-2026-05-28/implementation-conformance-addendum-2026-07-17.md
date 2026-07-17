---
name: Hexalith.ChatBot UX Implementation Conformance Addendum
status: binding
created: 2026-07-17
updated: 2026-07-17
sourceProposal: ../../sprint-change-proposal-2026-07-17.md
sources:
  - DESIGN.md
  - EXPERIENCE.md
  - m1-m2-surface-elaboration.md
  - epic10-chat-surface-elaboration.md
  - ../../architecture.md
---

# UX Implementation Conformance Addendum — 2026-07-17

This addendum backports the binding implementation rules learned through the former Epic 10/12/13 delivery chain into the UX source of truth. It adds no product capability and does not replace `DESIGN.md` or `EXPERIENCE.md`.

## Visual and component inheritance

The only supported inheritance chain is:

```text
Microsoft Fluent UI v5 → Hexalith.FrontComposer → DESIGN.md → EXPERIENCE.md
```

- ChatBot must not redefine or clone the FrontComposer/Fluent theme, palette, typography ramp, control primitives, or semantic foreground/background roles.
- Use a Fluent UI v5 or FrontComposer component whenever an equivalent exists. Raw interactive `<button>`, `<input>`, `<select>`, and `<textarea>` elements are prohibited in ChatBot-owned Razor surfaces.
- Custom CSS is limited to layout or product-specific behavior not owned by Fluent/FrontComposer. It must not recreate buttons, headings, fields, cards, status primitives, or theme tokens.
- The scoped `Hexalith.ChatBot.UI.styles.css` bundle must be linked and served. Live verification must assert a load-bearing computed style, including `.fluent-layout { display: grid; }`, so a missing bundle cannot pass on source inspection alone.

## Page composition

- Every routable ChatBot page composes through `FcPageLayout` and `FcPageHeader` inside the single FrontComposer shell.
- Do not render module-owned page chrome that competes with the shell. The legacy `.chatbot-page-header`, `.chatbot-page`, and `.chatbot-command-bar` patterns remain prohibited.
- Use `FluentStack`, `FluentCard`, `FluentGrid`, and `FluentDataGrid` according to content semantics. Primary business data must not render as a monospace `<dl>` dump.
- A page-like surface with two or more sibling titled sections groups those sections in one `FluentAccordion`, with the primary section expanded by default, unless the content is a single primary grid, form, or workflow.
- The Fluent-control and FrontComposer-layout guards are separate, non-vacuous, shrink-only governance checks with empty offender lists. Documented carve-outs: none.

## Surface-local acceptance

Each surface story owns its own live route and may not defer first acceptance to a final cross-surface story. Applicable acceptance includes:

- primary user workflow and server-verified success;
- loading/empty and validation states;
- unauthorized/redacted state without resource-existence leakage;
- degraded, retryable, terminal, and recovery behavior;
- keyboard operation, focus landing/return, reachable disabled reasons, and correct live-region behavior;
- desktop, tablet, and phone/small-screen fallback behavior;
- light, dark, forced-colors, and reduced-motion behavior;
- English/French parity and French text expansion;
- preserved authorization, tenant scope, policy, audit, redaction, and governed-command semantics.

The canonical surface ownership is:

| Surface story | Live routes/outcome |
| --- | --- |
| Shell and foundation | All routes share one working FrontComposer frame, provider tree, scoped assets, and component/layout guards. |
| Project Workspace, Conversation, Composer & Streaming | `/` and project conversation routes; governed message/AI request, progressive state, and safe Stop/Cancel. |
| Association Review | Candidate evidence, confirm/reject/defer/escalate, unauthorized suppression, and recovery. |
| AI Action Review | Inspect/approve/reject/revise/cancel with policy, evidence freshness, disabled reasons, and execution state. |
| Tenant Administration & Review Operations | Policy, mailbox, notification, escalation, and queue operations within bounded roles. |
| Operational Dashboards | Health, queue, freshness, degraded-state, and next-action presentation. |
| Compliance Investigation | Search, reconstruct, redact, and escalate permitted audit evidence. |

## Evidence hierarchy

1. A successful primary-path run against the real application is required for runtime, layout, browser, asset, hosting, and SignalR claims.
2. The test must directly assert the load-bearing invariant and the captured render must be inspected. Browser availability alone or a coarse geometry assertion is insufficient.
3. Static fixtures, source scans, snapshots, fallbacks, and handler-level tests remain valuable supporting evidence but cannot substitute for the live route.
4. The final live cross-surface suite is regression confirmation. It does not become the first place a surface's negative, responsive, accessible, localized, or governed behavior is accepted.
