---
name: Hexalith.ChatBot
description: Enterprise project conversation workspace for governed email, files, AI assistance, approvals, and multi-actor collaboration. Inherits Hexalith.FrontComposer and Microsoft Blazor Fluent UI v5.
status: in-review
created: 2026-05-28
updated: 2026-09-14
sources:
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
  - ../../product-brief-Hexalith.ChatBot.md
supplementalSources:
  - m1-m2-surface-elaboration.md
  - epic10-chat-surface-elaboration.md
  - implementation-conformance-addendum-2026-07-17.md
implementationReferences:
  - ../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md
reconciliationSources:
  - reconcile-product-contracts-2026-09-14.md
  - reconcile-architecture-2026-09-14.md
  - reconcile-existing-ux-inputs-2026-09-14.md
decisionLog: .memlog.md
components:
  project-context-header: { size: 'FrontComposer default' }
  conversation-shell: { size: 'FrontComposer default' }
  conversation-stream: { size: 'Fluent UI v5 default' }
  composer-action-entry: { size: 'Fluent UI v5 default' }
  actor-badge: { size: 'Fluent UI v5 compact' }
  message-classification: { size: 'Fluent UI v5 compact' }
  surface-section-group: { size: 'Fluent UI v5 default' }
  source-evidence: { size: 'Fluent UI v5 default' }
  ai-summary: { size: 'Fluent UI v5 default' }
  why-this-project: { size: 'Fluent UI v5 default' }
  evidence-freshness: { size: 'Fluent UI v5 compact' }
  task-intent-review: { size: 'Fluent UI v5 default' }
  attachment-row: { size: 'Fluent UI v5 data-grid' }
  association-candidate-group: { size: 'Fluent UI v5 default' }
  association-decision-bar: { size: 'Fluent UI v5 default' }
  action-classification: { size: 'Fluent UI v5 compact' }
  ai-proposal-panel: { size: 'Fluent UI v5 default' }
  approval-authority-and-effects: { size: 'Fluent UI v5 default' }
  approval-controls: { size: 'Fluent UI v5 default' }
  correction-progress: { size: 'Fluent UI v5 default' }
  qualification-status: { size: 'Fluent UI v5 default' }
  bounded-admin-scope: { size: 'Fluent UI v5 compact' }
  service-client-grant: { size: 'Fluent UI v5 default' }
  two-person-approval: { size: 'Fluent UI v5 default' }
  runtime-control-status: { size: 'Fluent UI v5 data-grid' }
  shared-operation-status: { size: 'Fluent UI v5 default' }
  operation-diagnostic: { size: 'Fluent UI v5 default' }
  queue-row: { size: 'Fluent UI v5 data-grid' }
  operational-slo-dashboard: { size: 'Fluent UI v5 data-grid' }
  audit-timeline: { size: 'Fluent UI v5 data-grid' }
  inbound-authenticity-and-sender-authority: { size: 'Fluent UI v5 default' }
  retention-and-export-request: { size: 'Fluent UI v5 default' }
  redacted-support-bundle: { size: 'Fluent UI v5 default' }
  blocked-state: { size: 'Fluent UI v5 default' }
  connectivity-status: { size: 'Fluent UI v5 default' }
  controlled-update-bar: { size: 'FrontComposer default' }
  status-toast-banner: { size: 'Fluent UI v5 default' }
  busy-region: { size: 'Fluent UI v5 default' }
  error-summary: { size: 'Fluent UI v5 default' }
  review-dialog: { size: 'Fluent UI v5 default' }
  queue-filter-bar: { size: 'FrontComposer default' }
---

## Brand & Style

Hexalith.ChatBot is a quiet enterprise command workspace for project-centered collaboration. It keeps authorized project context, source evidence, human authority, intended effects, qualification, and audit outcomes visually close to the work. It must not resemble a playful assistant, social chat feed, or marketing surface.

The supported visual inheritance chain is Microsoft Blazor Fluent UI v5 → Hexalith.FrontComposer → this DESIGN.md → `EXPERIENCE.md`. FrontComposer and Fluent own the palette, typography ramp, spacing, radii, elevation, focus rings, density, and base controls. This spine adds product semantics and component anatomy only. The central package catalog and current FrontComposer source are version authority; stale contingency notes are not an implementation baseline.

The architecture spine is a downstream implementation reference. It may clarify how an approved product state remains visible, but it does not override product intent or introduce a visual-system delta.

If a future mockup, wireframe, prototype, or imported reference conflicts with this spine pair, `DESIGN.md` and `EXPERIENCE.md` take precedence until the spine is intentionally revised.

No local color, typography, radius, or spacing tokens are declared because the product has no approved visual delta from the inherited system. Downstream implementations must use Fluent UI v5 or FrontComposer whenever an equivalent exists. Equivalent raw HTML, custom CSS controls, JavaScript widgets, or third-party components require a reviewed no-equivalent exception; layout-only CSS must not recreate inherited styling.

## Colors

All colors inherit from the active FrontComposer/Fluent UI v5 theme. Use inherited neutral roles for work surfaces, brand roles for the single primary action, information roles for evidence and non-terminal status, warning roles for ambiguity or review, danger/error roles for blocked or failed outcomes, and success roles only for qualified completed outcomes.

Color never carries meaning alone. Classification, freshness, approval disposition, correction, coordinator/effect/projection state, runtime controls, connectivity, qualification, replay/conflict, service-level objective (SLO), and denial states always include visible text plus an icon or border that survives dark mode and forced colors. The same status meaning applies across user-interface examples, command-line interface (CLI) documentation, and Model Context Protocol (MCP) descriptions.

Functional combinations must meet WCAG 2.2 AA: 4.5:1 for normal text and 3:1 for non-text UI and focus indicators. Focus appearance must remain visible in light, dark, and forced-colors modes. Muted text that communicates status, authority, provenance, qualification, or recovery remains functional text and meets the normal-text contrast target.

## Typography

Typography inherits the Fluent UI v5 ramp through FrontComposer. `FcPageHeader` owns page titles; Fluent heading, body, label, caption, and code conventions own the remaining hierarchy.

- Page titles identify the current authorized Project, review queue, policy area, or investigation.
- Compact section titles organize operational content without hero-scale typography.
- Body text explains evidence, intended effects, approval reasons, qualification limits, and recovery.
- Metadata text carries timestamps, provenance, authority class, policy version, classifier version, detector version, retry-profile version, canonical state and reason, operation identity, owner revision, and correlation identity.
- Monospace is reserved for stable identifiers and canonical command/state/reason codes, never for primary business data.

Source evidence, canonical records, derived projections, and AI-generated interpretation use distinct inherited text roles and explicit headings. Labels such as `AI summary`, `Canonical record`, `Investigation view`, `Partial response`, and `Qualification blocked` precede their content; typography reinforces but never substitutes for those text distinctions.

## Layout & Spacing

Every routable page uses `FcPageLayout` and `FcPageHeader` inside the single FrontComposer shell. Compose content with `FluentStack`, `FluentCard`, `FluentGrid`, and `FluentDataGrid` according to semantics. The scoped `Hexalith.ChatBot.UI.styles.css` bundle may add layout that Fluent/FrontComposer does not own, but must not recreate controls or theme tokens; live acceptance verifies `.fluent-layout { display: grid; }`.

Desktop/laptop is primary. Tablet stacks navigation and complementary panels. At 320 CSS pixels and 400% zoom, every in-scope task remains readable and operable without page-level horizontal scrolling or loss of content or actions. Labeled rows, details, and steps replace every wide grid unless its two-dimensional relationship is essential; only that essential content may scroll within its own bounded region. A larger-screen handoff is optional continuity, never a substitute for the complete task.

A page-like surface with two or more sibling titled sections uses one `Surface section group` (`FluentAccordion`) with one `FluentAccordionItem` per section and the primary item expanded. A single primary grid, form, detail view, or workflow stays outside it. Association Review is the sole documented carve-out: its `Association candidate group` and `Association decision bar` stay visible together outside the accordion; complementary evidence and source metadata are accordion items. `EXPERIENCE.md.Information Architecture` owns the surface-by-surface composition map.

## Elevation & Depth

Elevation separates active conversation, complementary evidence/review, dialogs, and transient feedback. It is never decorative hierarchy. Persistent workflow, connectivity, qualification, and operation state remain inline on their owning surface even when a toast announces a transition.

Source evidence, canonical records, investigation projections, and AI summaries must read as different nested regions without suggesting equivalent authority. A blocked state remains part of the relevant review unit rather than floating as an unrelated alert.

## Shapes

Shapes inherit Fluent UI and FrontComposer defaults. Compact badges are appropriate for actor type, classification, freshness, risk, state, runtime control, and SLO disposition; entire panels must not become pill-shaped. Product wrappers follow the inherited radius of their base component.

## Components

The frontmatter declares only recognized inherited visual sub-tokens. The table below owns exact component bases and visual anatomy; `EXPERIENCE.md.Component Patterns` owns behavior. Every base is a current Fluent UI v5 or FrontComposer component. A no-equivalent fallback must be documented before custom interactive markup is introduced.

The matching [behavioral component catalog](EXPERIENCE.md#component-patterns) uses the same names and order for direct lookup.

| Component | Canonical base and visual contract |
|---|---|
| Project context header | `FcPageHeader`; compact persistent title/context for the authorized Project, tenant on tenant-scoped or administrative surfaces, current surface, increment availability, and safe status. |
| Conversation shell | `FcPageLayout` with `FluentStack` and `FluentGrid`; stable frame relating Project context, stream, composer, and complementary context. |
| Conversation stream | `FluentCard` and `FluentText` groups; chronological attributed events with state before content; system decisions never appear as participant chat. |
| Composer action entry | `FluentField`, `FluentTextArea`, and `FluentButton`; governed message and AI-request actions remain distinct, with a stable Stop/Cancel position and no modifier-free shortcut competing with text entry. |
| Actor badge | `FluentBadge`; text-and-icon actor type, never color-only. |
| Message classification | `FluentBadge`; `informational` or `actionable` plus text/icon visibly attached to its message. |
| Surface section group | One `FluentAccordion` owning named `FluentAccordionItem` children; primary item expanded and all outside content named in the composition map. |
| Source evidence | `FluentAccordionItem` with `FluentCard`; authoritative source identity, redaction, and per-reference freshness. |
| AI summary | `FluentAccordionItem` with `FluentCard`; collapsed by default, labeled `AI summary`, with provenance before content. |
| Why this project | `FluentAccordionItem` with labeled `FluentText`; decision provenance and superseding links. |
| Evidence freshness | `FluentBadge` and `FluentText`; `fresh`, `stale`, or `expired` plus snapshot time. |
| Task intent review | `FluentCard` and `FluentButton`; source-linked detector result or detector-unavailable review state. |
| Attachment row | `FluentDataGrid` row; storage, scan, quarantine, retry, governed-folder, retention, and AI-context eligibility. |
| Association candidate group | `FluentRadioGroup` and `FluentRadio`; one authorized candidate choice with confidence and evidence. |
| Association decision bar | `FluentStack` and `FluentButton`; persistent safe next actions tied to the selected candidate and current canonical state. |
| Action classification | `FluentBadge`, `FluentText`, and labeled facts; valid classifier output stays distinct from pre-classification denial, unsupported work, and classifier unavailability. |
| AI proposal panel | `FluentCard`; visibly pending proposal linking source request, Project scope, classification, and decision unit. |
| Approval authority and effects | `FluentCard` and labeled `FluentText`; requester, human reviewer authority, command, resources, recipients, six-effect classification, expected post-state, and evidence freshness. |
| Approval controls | `FluentStack` and `FluentButton`; one primary human decision with reject, revise, and cancel grouped separately; unavailable decisions remain explained. |
| Correction progress | `FluentProgressBar`, `FluentMessageBar`, and `FluentText`; separates pre-commit block, committed correction, owner acknowledgments, delay, and unresolved wider impact. |
| Qualification status | `FluentCard`, `FluentBadge`, `FluentText`, and `FluentButton`; capability/increment, gate state, safe reason, owner, permitted fallback, and evidence/recheck action. |
| Bounded admin scope | `FluentBadge` and `FluentDataGrid`; aggregate visibility and partition control stay visually distinct from Project-authorized item access. |
| Service-client grant | `FluentCard`, `FluentBadge`, and `FluentButton`; client class, exact scope, expiry, initiator, distinct approver, version, and immutable status. |
| Two-person approval | `FluentCard` and `FluentStack`; ordered proposer and distinct approver with changed values, scope, justification, policy version, and conflict/expiry. |
| Runtime control status | `FluentDataGrid`, `FluentBadge`, and `FluentMessageBar`; control source/version, scope, freshness, affected capability, owner, and fail-closed result. |
| Shared operation status | `FluentCard`, `FluentBadge`, and `FluentText`; coordinator, effect-owner, committed owner revision, delivery/projection, immutable attempt, retry profile, prior outcome, and correlation. |
| Operation diagnostic | `FluentCard` with labeled `FluentText`; authorized correlation ID, tenant ID, mailbox ID, workflow-item ID, current state, retry count, failure reason, and next action. It also shows the last transition's timestamp, actor, and source state, plus predecessor/successor links, without exposing business detail. |
| Queue row | `FluentDataGrid` row; state, age, owner, scope, risk/confidence, freshness, next action, attempts, and terminality. |
| Operational SLO dashboard | `FluentDataGrid` and `FluentCard`; labeled `SLO qualification backlog` until qualified, with target, budget, evidence provenance, freshness, owner, support state, and informational approval-load/quality observations. |
| Audit timeline | `FluentDataGrid`; attributed sequence distinguishing canonical record references, laggable investigation projection, replay, annotation, correction, and outcome. |
| Inbound authenticity and sender authority | `FluentCard` and `FluentBadge`; provider evidence, anomaly/block posture, external sender, delegation, and outbound authority class. |
| Retention and export request | `FluentCard` and `FluentProgressBar`; data-class scope, authorization, legal-hold/redaction limit, explicit exposure confirmation, owner, and canonical status. |
| Redacted support bundle | `FluentCard` and `FluentButton`; included safe diagnostics, excluded restricted content, and explicit external-exposure approval. |
| Blocked state | `FluentMessageBar` and `FluentButton`; existence-neutral stable reason, affected scope, owner, and one safe next action. |
| Connectivity status | `FluentMessageBar`; scoped offline, pending-unknown, reconciling, and restored messages without implying submission outcome. |
| Controlled update bar | `FcPageToolbar`, `FluentText`, and `FluentButton`; pending-update count, pause/apply or manual-refresh action, and preserved active-row context. |
| Status toast banner | `FluentMessageBar` or inherited Fluent toast; transition feedback only while persistent state remains inline. |
| Busy region | `FluentSkeleton`; layout-matched replacement without shift or decorative loading narrative. |
| Error summary | `FluentMessageBar`; focusable validation landing point before the affected form/review unit. |
| Review dialog | `FluentDialog`; one contained modal layer with inherited focus containment and return focus. |
| Queue filter bar | `FcPageToolbar` containing labeled `FluentField`, `FluentTextInput`, `FluentSelect<TOption,TValue>`, and `FluentButton`; active filters, active server-side sort, and omission-safe result count remain visible on reflow. |

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit Fluent UI v5 and FrontComposer visual defaults and exact component identities. | Declare a local theme, use obsolete Fluent names, or substitute raw/third-party widgets where an inherited equivalent exists. |
| Keep source, authority, intended effects, freshness, qualification, canonical state, and audit status near each decision. | Make reviewers infer safety or readiness from color, a hidden drawer, or surface existence. |
| Label AI-mediated project mutation, file exposure, external communication, task creation/assignment, external-tool invocation, and acting on behalf as permanently `approval-required`. | Offer a tenant-policy control that downgrades any of the six or style machine approval as equivalent to a human decision. |
| Distinguish direct authorized human commands from AI-mediated proposals while governing both through their applicable contract. | Route every human state change through AI-proposal approval merely because it writes state. |
| Distinguish coordinator, committed owner effect, delivery/projection, partial output, and investigation-view freshness. | Present projection lag, advisory progress, or partial streamed text as a failed or completed effect. |
| Use existence-neutral blocked copy and omit unsafe candidates on every surface. | Reveal suppressed identities, ordering, evidence, or cardinality. |
| Keep every in-scope task complete at 320 CSS pixels and 400% zoom. | Replace required content or actions with a mandatory larger-screen handoff. |
| Keep operational lists dense, labeled, controlled on refresh, and responsive. | Use oversized cards, raw monospace dumps, infinite lists, silent reordering, or hover-only critical actions. |
| Preserve text/icon/border meaning in dark and forced-colors modes. | Depend on fill color, motion, toast-only feedback, or tooltip-only disabled reasons. |
