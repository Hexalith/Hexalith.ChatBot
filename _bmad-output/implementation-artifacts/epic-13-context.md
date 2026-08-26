# Epic 13 Context: Governed Interactive Workspace & UI Conformance

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver one governed, user-visible workspace for project conversation and operational work. All routes share a stable FrontComposer/Fluent frame and preserve tenant scope, authorization, policy, audit, redaction, and command semantics. Completion requires successful primary live routes plus cross-surface regression, with no separate or ungoverned chat path.

## Stories

- Story 13.1: Establish one working Fluent/FrontComposer application frame
- Story 13.2: Work, converse, and interrupt AI safely in project context
- Story 13.3: Resolve ambiguous association from a safe live review surface
- Story 13.4: Review risky AI actions without losing evidence or authority
- Story 13.5: Administer tenant policy and review operations within bounded authority
- Story 13.6: Understand live operational health and queues
- Story 13.7: Investigate permitted audit evidence from a usable live route
- Story 13.8: Confirm live cross-surface release conformance

## Requirements & Constraints

- Keep project context, conversation, files, actors, workflow state, AI proposals, approvals, and outcomes connected and attributed. Distinguish source evidence from AI summaries.
- Messages and AI requests use the governed command path. Risky requests become proposals; approved work uses allowlisted commands. No UI path bypasses authorization, approval, idempotency, or audit, or claims success before server admission.
- Secured surfaces fail closed. Unauthorized resources are suppressed without confirming existence; failure and recovery states use redacted language and expose the next permitted action.
- Each surface exposes only role-permitted data/actions and owns its live workflow plus applicable loading, empty, validation, unauthorized, degraded, retryable, terminal, and recovery states. Compliance stays read-only, administration gains no superuser bypass, and final regression cannot replace local acceptance.
- Workflows meet WCAG 2.2 AA through automated, keyboard-only, and screen-reader validation, including stable focus, non-color status, reachable disabled reasons, forced-colors, and reduced motion.
- English/French parity and French expansion are required. Desktop/tablet workflows remain complete; phone layouts provide accessible fallbacks for dense work.
- Runtime, layout, asset, hosting, and real-time claims require a successful primary path against the real application; static or fallback evidence is supporting only.

## Technical Decisions

- Use one `FrontComposerShell` and exactly one Fluent provider tree. Every routable page composes through `FcPageLayout` and `FcPageHeader`; module-owned page chrome that competes with the shell is prohibited.
- Use FrontComposer or Fluent UI v5 for controls and primary data. Raw interactive controls, recreated theme primitives, and definition-list data dumps are prohibited. Use semantic Fluent components; sibling titled sections use one accordion unless documented as one primary workflow.
- Serve `Hexalith.ChatBot.UI.styles.css` and prove a load-bearing computed style (`.fluent-layout` is `display:grid`) in a live browser.
- Keep Fluent-control and FrontComposer-layout conformance as separate, non-vacuous gates with shrink-only, empty offender lists.
- Surface adapters create typed commands and cannot bypass the shared pipeline. Origin stays immutable; equivalent UI, CLI, and MCP inputs produce equivalent normalized commands and governed outcomes.
- AI progress uses tenant-scoped, metadata-only SignalR nudges; clients re-query typed server state. Stop/Cancel is a governed optimistic-concurrency mutation validating tenant, project, conversation, generation identity, active state, authority, and expected version. Invalid targets fail closed, duplicates are benign, and terminal state is server verified.
- Release regression uses live loopback hosting and a real browser across locales, themes, widths, accessibility modes, and negative states while retaining tenant isolation, redaction, command admission, approval, reconnect/isolation, audit, and CLI/MCP parity.

## UX & Interaction Patterns

- Present the product as a quiet enterprise command workspace, not a playful assistant or consumer chat feed. Project context, evidence, risk, actor, state, timestamp, and next action stay close to the work.
- Apply inherited semantic roles consistently: neutral/default, brand/primary, information/evidence, warning/ambiguity or degradation, danger/blocked or terminal, and success/completed. Meaning never depends on color alone.
- Focus lands on the heading or first action. Validation focuses its summary, success focuses stable status, overlays return focus, and background updates neither steal focus nor force-scroll. Unavailable actions expose a keyboard-reachable reason.
- Stop/Cancel remains in a stable keyboard-reachable position during generation, announces the stopped result once when applicable, and returns focus to the composer or proposal. Reduced-motion mode suppresses streaming and non-essential movement while retaining textual progress.
- Dense grids reflow without dropping actor, risk, state, confidence, next action, or safe reason. Queues use stable pagination/virtualization; copy/export preserves visual-surface redaction.

## Cross-Story Dependencies

- Story 13.1 establishes the shared frame, assets, and governance gates used by every later surface story.
- Stories 13.2–13.7 each supply first acceptance for their own live route and states; Story 13.8 depends on all of them and confirms regression rather than replacing missing local evidence.
- The epic consumes the existing governed command spine, conversation and file projections, association and approval workflows, bounded administration, operational telemetry, audit/recovery capabilities, and CLI/MCP parity. It adds no alternate governance or backend mutation path.
