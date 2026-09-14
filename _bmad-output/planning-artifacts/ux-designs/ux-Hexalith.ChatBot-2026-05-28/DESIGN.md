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
decisionLog: .memlog.md
components:
  project-context-header:
    base: 'FcPageHeader'
    emphasis: 'authorized project and tenant context'
  conversation-shell:
    base: 'FcPageLayout with FluentStack and FluentGrid'
    emphasis: 'stable operational workspace'
  conversation-stream:
    base: 'FluentCard and FluentText groups'
    emphasis: 'chronological attributed events'
  composer-action-entry:
    base: 'FluentTextArea and FluentButton'
    emphasis: 'governed message and AI request'
  actor-badge:
    base: 'FluentBadge'
    distinction: 'text and icon'
  message-classification:
    base: 'FluentBadge'
    distinction: 'text, icon, and message association'
  source-evidence:
    base: 'FluentAccordion and FluentCard'
    default: 'expanded'
  ai-summary:
    base: 'FluentAccordion and FluentCard'
    default: 'collapsed'
  why-this-project:
    base: 'FluentAccordion and labelled facts'
    emphasis: 'decision provenance'
  evidence-freshness:
    base: 'FluentBadge and FluentText'
    distinction: 'state label and timestamp'
  task-intent-review:
    base: 'FluentCard and FluentButton group'
    emphasis: 'source-linked intent decision'
  attachment-row:
    base: 'FluentDataGrid row'
    emphasis: 'storage, scan, and context status'
  association-candidate-group:
    base: 'FluentRadioGroup and FluentRadio'
    emphasis: 'single governed candidate choice'
  association-decision-bar:
    base: 'FluentStack and FluentButton group'
    emphasis: 'safe next actions'
  action-classification:
    base: 'FluentBadge and labelled metadata'
    distinction: 'classifier output and user-visible disposition'
  ai-proposal-panel:
    base: 'FluentCard and FluentAccordion'
    emphasis: 'pending action preview'
  approval-authority-and-effects:
    base: 'FluentCard and labelled facts'
    emphasis: 'authority, effect, and expected post-state'
  approval-controls:
    base: 'FluentButton group'
    emphasis: 'approve, reject, revise, and cancel'
  correction-progress:
    base: 'FluentProgress and FluentMessageBar'
    distinction: 'state, acknowledgements, owner, and estimate'
  bounded-admin-scope:
    base: 'FluentBadge and FluentDataGrid'
    emphasis: 'scope without project-detail elevation'
  two-person-approval:
    base: 'FluentCard and FluentStack sequence'
    emphasis: 'distinct proposer and approver'
  shared-operation-status:
    base: 'FluentCard and FluentBadge'
    emphasis: 'stable identity, state, and replay outcome'
  queue-row:
    base: 'FluentDataGrid row'
    emphasis: 'owner, age, state, and next action'
  operational-slo-dashboard:
    base: 'FluentDataGrid and FluentCard'
    emphasis: 'target, budget, freshness, and support state'
  audit-timeline:
    base: 'FluentAccordion and FluentDataGrid'
    emphasis: 'reconstructable attributed sequence'
  inbound-authenticity-and-sender-authority:
    base: 'FluentCard and FluentBadge'
    emphasis: 'provider evidence and authority class'
  retention-and-export-request:
    base: 'FluentCard and FluentProgress'
    emphasis: 'data-class scope and request status'
  redacted-support-bundle:
    base: 'FluentCard and FluentButton'
    emphasis: 'safe diagnostic handoff'
  blocked-state:
    base: 'FluentMessageBar and FluentButton'
    emphasis: 'existence-neutral reason and safe next action'
  status-toast-banner:
    base: 'FluentMessageBar or FluentToast'
    emphasis: 'transition feedback only'
  busy-region:
    base: 'FluentSkeleton'
    emphasis: 'layout-matched replacement'
  error-summary:
    base: 'FluentMessageBar'
    emphasis: 'focusable validation landing point'
  review-dialog-sheet:
    base: 'FluentDialog or FrontComposer sheet'
    emphasis: 'single contained review layer'
  queue-filter-bar:
    base: 'FluentToolbar and Fluent input controls'
    emphasis: 'active filters and result count'
---

## Brand & Style

Hexalith.ChatBot is a quiet enterprise command workspace for project-centered collaboration. It keeps authorized project context, source evidence, human authority, intended effects, and audit outcomes visually close to the work. It must not resemble a playful assistant, social chat feed, or marketing surface.

The supported inheritance chain is Microsoft Blazor Fluent UI v5 → Hexalith.FrontComposer → this DESIGN.md → `EXPERIENCE.md`. FrontComposer and Fluent own the palette, typography ramp, spacing, radii, elevation, focus rings, density, and base controls. This spine adds product semantics and component anatomy only. The implementation reference is `references/Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md`.

No local color, typography, radius, or spacing tokens are declared because the product has no approved visual delta from the inherited system. Downstream implementations must use Fluent component parameters or Fluent 2 tokens and must not recreate inherited styling in raw CSS.

## Colors

All colors inherit from the active FrontComposer/Fluent UI v5 theme. Use the inherited neutral roles for work surfaces, brand roles for the single primary action, information roles for evidence and non-terminal status, warning roles for ambiguity or review, danger/error roles for blocked or failed outcomes, and success roles for completed outcomes.

Color never carries meaning alone. Classification, freshness, approval disposition, correction, replay/conflict, SLO, and denial states always include visible text plus an icon or border that survives dark mode and forced colors. The same status meaning applies across UI examples, CLI documentation, and MCP descriptions.

Load-bearing combinations must meet WCAG 2.2 AA: 4.5:1 for normal text and 3:1 for non-text UI and focus indicators. Focus appearance must remain visible in light, dark, and forced-colors modes. Muted text that communicates status, authority, provenance, or recovery is still functional text and must meet the normal-text contrast target.

## Typography

Typography inherits the Fluent UI v5 ramp through FrontComposer. `FcPageHeader` owns page titles; Fluent heading, body, label, caption, and code conventions own the remaining hierarchy.

- Page titles identify the current authorized project, review queue, policy area, or investigation.
- Compact section titles organize operational content without hero-scale typography.
- Body text explains evidence, intended effects, approval reasons, and recovery.
- Metadata text carries timestamps, provenance, authority class, policy/classifier version, stable state and reason codes, operation identity, and correlation identity.
- Monospace is reserved for stable identifiers and command/state codes, never for primary business data.

Source evidence and AI-generated interpretation use distinct inherited text roles and explicit headings. The visible label `AI summary` and its provenance precede generated content; typography reinforces but never substitutes for that text distinction.

## Layout & Spacing

Every routable page uses `FcPageLayout` and `FcPageHeader` inside the single FrontComposer shell. Compose content with `FluentStack`, `FluentCard`, `FluentGrid`, and `FluentDataGrid` according to semantics. The scoped `Hexalith.ChatBot.UI.styles.css` bundle may add layout that Fluent/FrontComposer does not own, but must not recreate controls or theme tokens; live acceptance verifies `.fluent-layout { display: grid; }`.

Desktop/laptop is primary. Tablet stacks navigation and complementary panels. Phone retains reading, status, and safe decision actions while dense administration and investigation use the documented larger-screen handoff. Layout changes must preserve labels, authority, state, reason, and safe next action.

A page-like surface with two or more sibling titled sections uses one `FluentAccordion`, with the primary item expanded, unless it contains one primary grid, form, detail view, or workflow. Association Review is the sole documented carve-out: its `Association candidate group` and `Association decision bar` stay visible together outside the accordion; complementary evidence and source metadata use an accordion.

## Elevation & Depth

Elevation separates active conversation, complementary evidence/review, dialogs or sheets, and transient feedback. It is never decorative hierarchy. Persistent workflow state remains inline on its owning surface even when a toast announces the transition.

Source evidence and AI summaries must read as different nested regions without suggesting that the generated summary has equal authority. A blocked state must remain part of the relevant review unit rather than floating as an unrelated alert.

## Shapes

Shapes inherit Fluent UI and FrontComposer defaults. Compact badges are appropriate for actor type, classification, freshness, risk, state, and SLO disposition; entire panels must not become pill-shaped. Product wrappers follow the inherited radius of their base component.

## Components

All components below inherit their base visual implementation from the frontmatter `components` map. Their names are the canonical names shared with `EXPERIENCE.md.Component Patterns`.

| Component | Visual contract |
|---|---|
| Project context header | Compact persistent header for the authorized project and tenant context, current surface, and safe status. |
| Conversation shell | Stable operational frame; project context, stream, composer, and complementary context remain visibly related. |
| Conversation stream | Chronological event groups with actor, origin, timestamp, and state before content; system decisions never masquerade as chat. |
| Composer/action entry | Fluent input and actions with a stable Stop/Cancel position; user message and AI request affordances are visibly distinct. |
| Actor badge | Text-and-icon badge for human, external party, service client, AI actor, background worker, CLI, MCP, or mailbox event; never color-only. |
| Message classification | Text-and-icon `informational` or `actionable` badge visibly associated with its message; actionable treatment includes task-intent status. |
| Source evidence | Authoritative evidence region, expanded by default, with heading, source identity, redaction, and freshness attached to each reference. |
| AI summary | Collapsed-by-default region labelled `AI summary`; provenance appears before content and the surface is structurally distinct from source evidence. |
| Why this project | Labelled-facts disclosure for signal class, matched value, confidence/band, decision actor/time, and correction links. |
| Evidence freshness | Compact text-and-icon state `fresh`, `stale`, or `expired` paired with its snapshot timestamp; forced colors preserve the boundary and label. |
| Task intent review | Source-linked card showing intent summary, action kind, confidence, detector version, evidence excerpts, state, and one primary disposition. |
| Attachment row | Labelled grid row for storage, scan, quarantine, duplicate/retry, governed-folder link, retention, and AI-context eligibility. |
| Association candidate group | One named Fluent radiogroup; each option presents only authorized project identity, confidence, and evidence references. |
| Association decision bar | Persistent decision unit repeating the selected safe candidate and presenting confirm, reject-all, defer, or escalate. |
| Action classification | Shows the user-visible disposition prominently and the internal classifier output/version/input tuple as subordinate labelled metadata. |
| AI proposal panel | Clearly pending proposal, never styled as completed work; links the source request, project scope, and approval unit. |
| Approval authority and effects | Labelled facts for requester/origin, approver authority, sender/delegation, command/version, files, recipients, effects, reversibility, expected post-state, and audit events. |
| Approval controls | One primary permitted decision and grouped reject/revise/cancel actions; unavailable approval remains visually explained. |
| Correction progress | `Correcting` or `Correction-delayed` label, acknowledged/remaining stores, estimate, owner, next action, and AI-use block. |
| Bounded admin scope | Scope badges and aggregate data distinguish see-only, queue-operate, mailbox, policy, and compliance powers from project-detail authority. |
| Two-person approval | Ordered proposal and distinct-approver steps with changed values, scope, justification, policy version, expiry/conflict, and audit link. |
| Shared operation status | Persistent status card with operation/idempotency identity, canonical state/reason, attempt count/ceiling, retry eligibility, origin, and prior-outcome link. |
| Queue row | Dense labelled row showing state, age, owner, risk/confidence, freshness, next action, and terminal/non-terminal status. |
| Operational SLO dashboard | Grid/cards showing metric, target, window, error budget, alert threshold, freshness, owner, and `within-budget`/`approaching`/`exhausted`/`unsupported`. |
| Audit timeline | Filterable attributed sequence with source, policy, approval, command, replay, redaction, correction, and outcome relationships. |
| Inbound authenticity and sender authority | Provider-evidence and authority-class card; discrepancies and delegation are text-labelled and visually tied to the affected message/action. |
| Retention and export request | Data-class scoped request card with authorized scope, redaction, legal-hold limits, progress, owner, explicit human confirmation for exposure, and completion/blocked status. |
| Redacted support bundle | Handoff card naming included correlation/state/reason material, explicitly excluded restricted content, and the approval-required boundary for external exposure. |
| Blocked state | Persistent, existence-neutral message with stable safe code, short reason, owner when applicable, and one safe next action. |
| Status toast/banner | Transient transition feedback only; persistent status remains inline. Scope global banners to the whole app only when the whole app is affected. |
| Busy region | Layout-matched Fluent skeleton; the eventual content replaces it in place without layout shift or decorative loading narrative. |
| Error summary | Visually prominent, focusable summary before the affected form/review unit, with links to field or decision errors. |
| Review dialog/sheet | One modal layer using inherited containment and return-focus treatment; never hides the only primary content by default. |
| Queue filter bar | Compact Fluent toolbar with active-filter summary and result count; filters retain labels when the grid reflows. |

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit Fluent UI v5 and FrontComposer visual defaults. | Declare local theme colors, type ramps, radius scales, or spacing scales without an approved product delta. |
| Keep source, authority, intended effects, freshness, and audit status near each decision. | Make reviewers infer safety from a badge color or a hidden drawer. |
| Label AI summaries and classifier metadata as generated/derived. | Make AI interpretation look like source evidence or completed work. |
| Show `approval-required` for every boundary-crossing effect until upstream reconciliation is formally complete. | Offer a visual tenant-policy control that downgrades project mutation, file exposure, external send, task creation/assignment, tool invocation, or acting on behalf. |
| Use existence-neutral blocked copy and suppress unsafe candidates. | Reveal that a forbidden project, file, party, or audit record exists. |
| Keep operational lists dense, labelled, and responsive. | Use oversized cards, raw monospace data dumps, infinite lists, or hover-only critical actions. |
| Preserve text/icon/border meaning in dark and forced-colors modes. | Depend on fill color, motion, toast-only feedback, or tooltip-only disabled reasons. |
