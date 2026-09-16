---
name: Hexalith.ChatBot
description: Enterprise project conversation workspace for governed email, files, AI assistance, approvals, and multi-actor collaboration. Inherits Hexalith.FrontComposer and Microsoft Blazor Fluent UI v5.
status: in-review
created: 2026-05-28
updated: 2026-09-16
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
  - reconcile-product-contracts-2026-09-16.md
  - reconcile-architecture-and-change-2026-09-16.md
  - reconcile-existing-ux-inputs-2026-09-16.md
  - ../../sprint-change-proposal-2026-09-15.md
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
  association-correction-entry: { size: 'Fluent UI v5 default' }
  association-correction-detail: { size: 'FrontComposer default' }
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

The finalized PRD and approved normative addendum are the product authority. `.memlog.md` records durable UX decisions and overrides. The product brief supplies historical context and user language only; supplemental UX files may elaborate behavior only within that authority. The architecture spine is a downstream implementation reference. It may clarify how an approved product state remains visible, but it does not override product intent or introduce a visual-system delta.

If a future mockup, wireframe, prototype, or imported reference conflicts with this spine pair, `DESIGN.md` and `EXPERIENCE.md` take precedence until the spine is intentionally revised.

This revision is intentionally spine-only: there are no approved files in `imports/`, `mockups/`, or `wireframes/`, so no visual artifact supplies an additional requirement or remains orphaned from the contract.

No local color, typography, radius, or spacing tokens are declared because the product has no approved visual delta from the inherited system. Downstream implementations must use Fluent UI v5 or FrontComposer whenever an equivalent exists. Equivalent raw HTML, custom CSS controls, JavaScript widgets, or third-party components require a reviewed no-equivalent exception; layout-only CSS must not recreate inherited styling.

## Colors

All colors inherit from the active FrontComposer/Fluent UI v5 theme. Use inherited neutral roles for work surfaces, brand roles for the single primary action, information roles for evidence and non-terminal status, warning roles for ambiguity or review, danger/error roles for blocked or failed outcomes, and success roles only for qualified completed outcomes.

Color never carries meaning alone. Outside generated projection status cells, classification, freshness, approval disposition, correction, coordinator/effect/projection state, runtime controls, connectivity, qualification, replay/conflict, service-level objective (SLO), and denial states include visible text plus an icon or border that survives dark mode and forced colors. A generated `[ProjectionBadge]` cell may use FrontComposer's shape-distinct `FcStatusIcon` compact-grid exception: it has a contextual accessible name and keyboard/hover tooltip, and responsive labeled-record presentation exposes the status text. The same status meaning applies across user-interface examples, command-line interface (CLI) documentation, and Model Context Protocol (MCP) descriptions.

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

Every routable page uses `FcPageLayout` and `FcPageHeader` inside the single FrontComposer shell. Compose content with `FluentStack`, `FluentCard`, and `FluentGrid` according to semantics; use `FluentDataGrid` for a read-only projection only through generated FC-TBL or its reviewed Level-2/Level-3 exception. The scoped `Hexalith.ChatBot.UI.styles.css` bundle may add layout that Fluent/FrontComposer does not own, but must not recreate controls or theme tokens; live acceptance verifies `.fluent-layout { display: grid; }`.

Desktop/laptop is primary. Tablet stacks navigation and complementary panels. At 320 CSS pixels and 400% zoom, every in-scope task remains readable and operable without page-level horizontal scrolling or loss of content or actions. Labeled rows, details, and steps replace every wide grid unless its two-dimensional relationship is essential; only that essential content may scroll within its own bounded region. A larger-screen handoff is optional continuity, never a substitute for the complete task.

A page-like surface with two or more sibling titled sections uses one `Surface section group` (`FluentAccordion`) with one `FluentAccordionItem` per section and the primary item expanded. A single primary grid, form, detail view, or workflow stays outside it. Association Review is the sole documented carve-out: its `Association candidate group`, `Association decision bar`, and M0 `Association correction entry`/persistent status summary stay visible together outside the accordion; complementary evidence and source metadata are accordion items. In the durable S2-owned correction detail, the primary rationale/replacement-evidence form stays outside the accordion before commit; after commit, persistent canonical correction status occupies that same outside position. One accordion contains only complementary impact/evidence, owner progress/recovery, and immutable history. Correction never uses a modal; S1 may only deep-link to the S2 detail, and M1 S4 extends the same visual contract. `EXPERIENCE.md.Information Architecture` owns the surface-by-surface composition map.

## Elevation & Depth

Elevation separates active conversation, complementary evidence/review, dialogs, and transient feedback. It is never decorative hierarchy. Persistent workflow, connectivity, qualification, and operation state remain inline on their owning surface even when a toast announces a transition.

Source evidence, canonical records, investigation projections, and AI summaries must read as different nested regions without suggesting equivalent authority. A blocked state remains part of the relevant review unit rather than floating as an unrelated alert.

## Shapes

Shapes inherit Fluent UI and FrontComposer defaults. Compact badges are appropriate for actor type, classification, non-status categories, counts, chips, and optimistic summaries; entire panels must not become pill-shaped. Semantic freshness, risk, workflow state, runtime-control, health, qualification, and SLO dispositions use visible-label `FcStatusBadge` outside grids. Generated `[ProjectionBadge]` cells use shape-distinct `FcStatusIcon` with contextual accessible name and keyboard/hover tooltip; they do not add a duplicate badge pill. Raw `FluentBadge` is not a semantic-status substitute. Product wrappers follow the inherited radius of their base component.

## Components

The frontmatter declares only recognized inherited visual sub-tokens. The table below owns exact component bases and visual anatomy; `EXPERIENCE.md.Component Patterns` owns behavior. Every base is a current Fluent UI v5 or FrontComposer component. A no-equivalent fallback must be documented before custom interactive markup is introduced.

The matching [behavioral component catalog](EXPERIENCE.md#component-patterns) uses the same names and order for direct lookup.

Read-only projection grids use the generated FC-TBL path by default: a partial read model annotated with `[Projection]` emits the `FluentDataGrid<T>` view and inherited filter, notice, status, prioritization, and row-detail chrome. Direct `FluentDataGrid` composition is permitted only as a reviewed FrontComposer Level-2/Level-3 customization around the generated grid, with the same canonical view key and all FC-TBL accessibility and state contracts preserved. The required FC-TBL behavior selects `Items` below `FcShellOptions.VirtualizationServerSideThreshold` or `ItemsProvider` at or above it on first mount, then changes lane only through explicit grid invalidation/remount; the threshold remains strictly below `MaxUnfilteredItems`. The pinned generator currently re-evaluates item count on render instead of honoring that latch, so affected grids remain FrontComposer-qualification-blocked until the platform fixes the lane or proves an equivalently safe explicit rebind. ChatBot does not fork the grid. Every row has a stable domain/projection key. Loading, query error, unfiltered-empty, filter-empty/summary, max-cap, and slow-query notices sit adjacent to the grid, and expanded row detail remains outside the virtualized body. Conversation and audit history are bounded, server-windowed chronological collections with explicit older/newer navigation; neither is an unbounded client list or infinite DOM.

Semantic status surfaces outside generated grids use visible-label `FcStatusBadge`. Generated projection status cells use `[ProjectionBadge]`/`FcStatusIcon` as the approved compact-grid exception, with a distinct glyph, contextual accessible name, keyboard/hover tooltip, and status text in responsive labeled records. Raw `FluentBadge` remains available only for non-status categories, counts, filter chips, and optimistic command summaries. If an inherited component exposes no required semantic-status slot, the adopter records a reviewed FrontComposer extension before adding a product wrapper.

| Component | Canonical base and visual contract |
|---|---|
| Project context header | `FcPageHeader`; compact persistent title/context for the authorized Project, tenant on tenant-scoped or administrative surfaces, current surface, and increment availability. Ordinary Project surfaces show only computed qualification status, safe reason, owner, fallback, and recheck; they never expose a raw gate-set identifier. Operator/admin/release-evidence variants may link to a safe gate-set reference. |
| Conversation shell | `FcPageLayout` with `FluentStack` and `FluentGrid`; stable frame relating Project context, stream, composer, and complementary context. |
| Conversation stream | A uniquely named transcript region containing semantic `list`, `feed`, `log`, or equivalent navigation over `FluentCard` and `FluentText` items; chronological attributed events expose stable item ID, sequence, source/origin, and state before content, programmatic relations to their originating request/evidence/proposal/outcome, and a keyboard-reachable first-unseen/new-updates target. System decisions never appear as participant chat. |
| Composer action entry | `FluentField`, `FluentTextArea`, and `FluentButton`; governed message and AI-request actions remain distinct, with a stable Stop/Cancel position and no modifier-free shortcut competing with text entry. |
| Actor badge | `FluentBadge`; text-and-icon actor type, never color-only. |
| Message classification | `FluentBadge` with adjacent labeled metadata; `informational` or `actionable`, where actionable resolves to `request-information`, `request-action`, or `request-decision`, plus detector version, evidence offsets, confidence, and review/capture/dismiss affordances without implying action risk. |
| Surface section group | One `FluentAccordion` owning named `FluentAccordionItem` children; primary item expanded and all outside content named in the composition map. |
| Source evidence | `FluentAccordionItem` with `FluentCard`; default-visible authoritative source identity, source-evidence ID, immutable origin/trust label, redaction state, extraction boundary, and per-reference freshness. |
| AI summary | `FluentAccordionItem` with `FluentCard`; collapsed by default, labeled `AI summary`, and preceded by exactly `Generated by <model+version> at <timestamp> from <source-evidence-IDs>`. |
| Why this project | `FluentAccordionItem` with labeled `FluentText`; originating signal class, matched value, confidence score, `auto`/`needs-review` disposition, typed reason, visible-candidate rule, decision actor/timestamp, and superseding-correction links. |
| Evidence freshness | `FcStatusBadge` and `FluentText`; `fresh`, `stale`, or `expired` plus snapshot time. |
| Task intent review | `FluentCard` and `FluentButton`; source message and evidence offsets, tenant/Project/requester, summary, `request-information|request-action|request-decision|informational`, detector version, confidence, detection time/state, and detector-unavailable review state. |
| Attachment row | Generated `[Projection]` FC-TBL row with `[ProjectionBadge]`/`FcStatusIcon` status where applicable; storage, scan, quarantine, retry, governed-folder, retention, and AI-context eligibility. |
| Association candidate group | `FluentRadioGroup` and `FluentRadio`; one authorized candidate choice with confidence and evidence. |
| Association decision bar | `FluentStack` and `FluentButton`; persistent safe next actions tied to the selected candidate and current canonical state. |
| Association correction entry | Inline `FluentCard`, `FcStatusBadge`, and `FluentButton`; S2-only authorized correction entry plus persistent canonical status summary and a stable link to the durable S2 detail. It is never a dialog. |
| Association correction detail | `FcPageLayout`, `FcPageHeader`, `FluentStack`, and one `FluentAccordion`; S2-owned detail with the primary rationale/replacement-evidence form outside before commit or persistent canonical status outside after commit. The accordion contains complementary impact/evidence, owner progress/recovery, and immutable history; origin-context return stays stable. |
| Action classification | `FcStatusBadge`, `FluentText`, and labeled facts; `low-risk` or `approval-required` shows the exact input tuple (allowlist entry, effect surface, policy snapshot, requester authority, and Project/file/recipient/tool scopes), while pre-classification `denied`/`unsupported` and `classifier-indeterminate` remain visually distinct. |
| AI proposal panel | `FluentCard`; visibly pending proposal linking source request, Project scope, classification, and decision unit. |
| Approval authority and effects | `FluentCard` and labeled `FluentText`; current allowlist command, tappable input-file evidence plus redaction, outbound recipients, sender-authority class, classifier input tuple, policy snapshot, `approved_at`/`expires_at`, requester/human independence, six-effect classification, expected resource changes/side effects/audit events, and evidence freshness. |
| Approval controls | `FluentStack` and `FluentButton`; `approve|reject|request-revision|cancel`, with every affordance visibly `enabled`, `disabled-with-reason`, or `not-applicable-hidden`; disabled reason is one of `insufficient-authority|state-not-permitted|dependency-degraded|awaiting-other-actor|policy-blocked`. |
| Correction progress | `FluentProgressBar`, `FluentMessageBar`, and `FluentText`; separates pre-commit block, committed correction, owner acknowledgments, delay, and unresolved wider impact. It identifies immutable manifest items by stable item identity, accepts acknowledgment/disposition only from the A13-mapped authenticated owner adapter/actor, and marks `CorrectionDelayed` with owner, next safe action, and P2 escalation. |
| Qualification status | `FluentCard`, `FcStatusBadge`, `FluentText`, and `FluentButton`; capability/increment, exact gate state `open|approved-current|expired|invalidated|superseded`, immutable gate-record provenance, candidate/dependency/environment/evidence binding, safe reason, owner, permitted fallback, and evidence/recheck action. Ordinary business surfaces omit gate-set identifiers; operator/admin/release-evidence views may show a safe gate-set reference. |
| Bounded admin scope | Generated `[Projection]` FC-TBL with `[ProjectionBadge]`/`FcStatusIcon` status; aggregate visibility and partition control stay visually distinct from Project-authorized item access. |
| Service-client grant | `FluentCard`, `FcStatusBadge`, and `FluentButton`; client class, exact scope, expiry, initiator, distinct approver, version, and immutable status. |
| Two-person approval | `FluentCard` and `FluentStack`; ordered proposer and distinct approver with changed values, scope, justification, policy version, and conflict/expiry. |
| Runtime control status | Generated `[Projection]` FC-TBL with `[ProjectionBadge]`/`FcStatusIcon` status and `FluentMessageBar`; control source/version, scope, freshness, affected capability, owner, and fail-closed result. |
| Shared operation status | `FluentCard`, `FcStatusBadge`, and `FluentText`; operation identity and current typed status appear within five seconds p95. After 30 seconds it exposes a retrievable detailed status with retry count, partial-output marker, terminal reason, next safe action, and correlation, while keeping coordinator, effect owner/revision, delivery/projection, immutable attempt, retry profile, and prior outcome distinct. |
| Operation diagnostic | `FluentCard` with labeled `FluentText`; authorized correlation ID, tenant ID, mailbox ID, workflow-item ID, current state, retry count, failure reason, and next action. It also shows the last transition's timestamp, actor, and source state, plus predecessor/successor links, without exposing business detail. |
| Queue row | Generated `[Projection]` FC-TBL row with `[ProjectionBadge]`/`FcStatusIcon` status; queue/health name, current depth or exact status `healthy|degraded|failed|unknown`, oldest item age, owner role, authorized detail link, scope, categorical action-risk class where applicable, association/task-intent confidence only when sourced from that evidence, freshness, next action, attempts, and terminality. Numeric confidence never represents action risk. |
| Operational SLO dashboard | Generated `[Projection]` FC-TBL with `[ProjectionBadge]`/`FcStatusIcon` dispositions and `FluentCard`; labeled `SLO qualification backlog` until qualified, with accompanying queue/health name, current depth or exact health status, oldest age, triage owner/detail link, target source/window, budget, evidence provenance, freshness, exact disposition `within-budget|approaching|exhausted|unsupported`, and informational approval-load/quality observations. |
| Audit timeline | A bounded, server-windowed generated `[Projection]` FC-TBL chronology with stable row keys and explicit older/newer navigation; attributed sequence distinguishes canonical record references, laggable investigation projection, replay with `replay_run_id`, annotation, correction, and outcome; replay is visibly excluded from production audit completeness. |
| Inbound authenticity and sender authority | `FluentCard` and `FcStatusBadge`; provider evidence, anomaly/block posture, external sender, delegation, and outbound authority class. |
| Retention and export request | `FluentCard` and `FluentProgressBar`; data-class scope, authorization, legal-hold/redaction limit, explicit exposure confirmation, owner, and canonical status. |
| Redacted support bundle | `FluentCard` and `FluentButton`; included safe diagnostics, excluded restricted content, and explicit external-exposure approval. |
| Blocked state | `FluentMessageBar` and `FluentButton`; versioned catalog message with stable code, user-safe headline of at most 80 characters, one non-leaking reason sentence, affected safe scope/owner, and one action from `retry|escalate|dismiss|request access`. |
| Connectivity status | `FluentMessageBar`; scoped offline, pending-unknown, reconciling, and restored messages without implying submission outcome. |
| Controlled update bar | `FcPageToolbar`, `FluentText`, and `FluentButton`; pending-update count, pause/apply or manual-refresh action, and preserved active-row context. |
| Status toast banner | `FluentMessageBar` or inherited Fluent toast; transition feedback only while persistent state remains inline. |
| Busy region | An owning named region with `aria-busy="true"`, one localized deduplicated loading-status message, and layout-matched `FluentSkeleton` children excluded from the accessibility tree as decorative; success, empty, error, blocked, or cancellation clears busy state and announces the terminal result once without shift or loading narrative. Authorized-context replacement clears the stale busy state without announcing the displaced result. |
| Error summary | `FluentMessageBar`; focusable validation landing point before the affected form/review unit. Each invalid control sets `aria-invalid="true"`, references stable localized error/help IDs with `aria-describedby`, and clears stale invalid/error associations after correction or state replacement. |
| Review dialog | Dialog content rendered as `FluentDialogBody` and opened through inherited `IDialogService`, which is the sole lifetime owner. A required FrontComposer shell arbitration service owns opening across review, shell settings, command palette, and destructive confirmation. While one modal is active, a second opener is rejected with localized scoped status and retains focus; dialogs never queue or nest. Safe initial focus, Cancel/Escape, containment, and return to the invoking control or documented successor are required. Use `FcDestructiveConfirmationDialog` for eligible destructive confirmations, but French destructive flows remain qualification-blocked until FrontComposer localizes its framework-owned Cancel/default Confirm/default body and generated title/body/label seams. ChatBot does not fork the dialog. |
| Queue filter bar | `FcPageToolbar` containing labeled `FluentField`, `FluentTextInput`, `FluentSelect<TOption,TValue>`, and `FluentButton`; active filters, active server-side sort, and omission-safe result count remain visible on reflow. |

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit Fluent UI v5 and FrontComposer visual defaults and exact component identities. | Declare a local theme, use obsolete Fluent names, or substitute raw/third-party widgets where an inherited equivalent exists. |
| Keep source, authority, intended effects, freshness, qualification, canonical state, and audit status near each decision. | Make reviewers infer safety or readiness from color, a hidden drawer, or surface existence. |
| On operator/admin/release-evidence views, derive status from immutable gate records and the published gate set; fail closed on incomplete, mixed-candidate, expired, invalidated, or superseded sets. | Treat file presence, prose approval, a local Boolean, or an aggregate badge as qualification authority, or expose raw gate-set identifiers in an ordinary Project header. |
| Label AI-mediated project mutation, file exposure, external communication, task creation/assignment, external-tool invocation, and acting on behalf as permanently `approval-required`. | Offer a tenant-policy control that downgrades any of the six or style machine approval as equivalent to a human decision. |
| Keep M0 AI execution to approved `Project.AppendConversationMessage`; introduce read-only `ChatBot.ExecuteLowRiskAssistance` only in M1. | Show M0 low-risk autonomous execution or make catalog membership look AI-invocable. |
| Keep M0 correction entry, status, and durable detail owned by S2; let S1 deep-link and S4 extend it. | Put correction controls in S1, place M0 correction behind a dialog, or make S4 a prerequisite. |
| Distinguish direct authorized human commands from AI-mediated proposals while governing both through their applicable contract. | Route every human state change through AI-proposal approval merely because it writes state. |
| Distinguish coordinator, committed owner effect, delivery/projection, partial output, and investigation-view freshness. | Present projection lag, advisory progress, or partial streamed text as a failed or completed effect. |
| Use existence-neutral blocked copy and omit unsafe candidates on every surface. | Reveal suppressed identities, ordering, evidence, or cardinality. |
| Keep every in-scope task complete at 320 CSS pixels and 400% zoom. | Replace required content or actions with a mandatory larger-screen handoff. |
| Keep operational lists dense, labeled, controlled on refresh, and responsive. | Use oversized cards, raw monospace dumps, infinite lists, silent reordering, or hover-only critical actions. |
| Use generated `[Projection]` FC-TBL grids, semantic FrontComposer status primitives, and `IDialogService`-owned `FluentDialogBody` dialogs. | Hand-author ordinary read-only grids, use raw `FluentBadge` for semantic status, or introduce nested/multiply owned modal state. |
| Preserve text/icon/border meaning in dark and forced-colors modes. | Depend on fill color, motion, toast-only feedback, or tooltip-only disabled reasons. |
