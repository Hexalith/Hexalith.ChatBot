---
name: Hexalith.ChatBot
description: Enterprise project conversation workspace for governed AI, email, files, approvals, and multi-actor collaboration. Inherits Hexalith.FrontComposer and Microsoft Blazor Fluent UI v5.
status: final
created: 2026-05-28
updated: 2026-06-05T12:12:05+02:00
sources:
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
  - ../../product-brief-Hexalith.ChatBot.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd-validation-report.md
colors:
  fluent-neutral-background: 'var(--colorNeutralBackground1)'
  fluent-neutral-background-raised: 'var(--colorNeutralBackground2)'
  fluent-neutral-foreground: 'var(--colorNeutralForeground1)'
  fluent-neutral-foreground-muted: 'var(--colorNeutralForeground3)'
  fluent-neutral-stroke: 'var(--colorNeutralStroke1)'
  fluent-brand: 'var(--colorBrandBackground)'
  fluent-brand-foreground: 'var(--colorNeutralForegroundOnBrand)'
  status-success: 'var(--colorStatusSuccessBackground1)'
  status-success-foreground: 'var(--colorStatusSuccessForeground1)'
  status-warning: 'var(--colorStatusWarningBackground1)'
  status-warning-foreground: 'var(--colorStatusWarningForeground1)'
  status-danger: 'var(--colorStatusDangerBackground1)'
  status-danger-foreground: 'var(--colorStatusDangerForeground1)'
  status-info: 'var(--colorStatusInformationBackground1)'
  status-info-foreground: 'var(--colorStatusInformationForeground1)'
typography:
  page-title:
    note: 'Inherited from Fluent UI type ramp; use FrontComposer page/header convention.'
  section-title:
    note: 'Inherited from Fluent UI type ramp; compact enterprise surface heading.'
  body:
    note: 'Inherited from Fluent UI body token.'
  metadata:
    note: 'Inherited from Fluent UI caption/metadata token.'
  code:
    note: 'Use platform monospace for IDs, command names, correlation IDs, and CLI/MCP examples.'
rounded:
  sm: '4px'
  md: '8px'
  lg: '12px'
spacing:
  '1': '4px'
  '2': '8px'
  '3': '12px'
  '4': '16px'
  '6': '24px'
  density-compact: '8px'
  density-comfortable: '12px'
  panel-gap: '16px'
  row-gap: '8px'
components:
  project-context-header:
    background: '{colors.fluent-neutral-background-raised}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.fluent-neutral-stroke}'
    radius: '{rounded.md}'
  conversation-shell:
    background: '{colors.fluent-neutral-background}'
    foreground: '{colors.fluent-neutral-foreground}'
    radius: '{rounded.md}'
  conversation-stream:
    background: '{colors.fluent-neutral-background}'
    foreground: '{colors.fluent-neutral-foreground}'
    radius: '{rounded.md}'
  composer-action-entry:
    background: '{colors.fluent-neutral-background-raised}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.fluent-neutral-stroke}'
    radius: '{rounded.sm}'
  actor-badge:
    background: '{colors.fluent-neutral-background-raised}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.fluent-neutral-stroke}'
    radius: '{rounded.sm}'
  evidence-chip:
    background: '{colors.status-info}'
    foreground: '{colors.status-info-foreground}'
    radius: '{rounded.sm}'
  risk-chip:
    background: '{colors.status-warning}'
    foreground: '{colors.status-warning-foreground}'
    radius: '{rounded.sm}'
  attachment-row:
    background: '{colors.fluent-neutral-background}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.fluent-neutral-stroke}'
    radius: '{rounded.sm}'
  evidence-drawer:
    background: '{colors.fluent-neutral-background-raised}'
    foreground: '{colors.fluent-neutral-foreground}'
    radius: '{rounded.lg}'
  ai-proposal-panel:
    background: '{colors.fluent-neutral-background-raised}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.status-warning}'
    radius: '{rounded.md}'
  approval-controls:
    primary-background: '{colors.fluent-brand}'
    primary-foreground: '{colors.fluent-brand-foreground}'
    radius: '{rounded.sm}'
  blocked-state:
    background: '{colors.status-danger}'
    foreground: '{colors.status-danger-foreground}'
    radius: '{rounded.md}'
  approval-panel:
    background: '{colors.fluent-neutral-background}'
    foreground: '{colors.fluent-neutral-foreground}'
    radius: '{rounded.md}'
  association-candidate-row:
    background: '{colors.fluent-neutral-background}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.fluent-neutral-stroke}'
    radius: '{rounded.sm}'
  queue-row:
    background: '{colors.fluent-neutral-background}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.fluent-neutral-stroke}'
    radius: '{rounded.sm}'
  audit-timeline:
    background: '{colors.fluent-neutral-background}'
    foreground: '{colors.fluent-neutral-foreground}'
    radius: '{rounded.md}'
  status-toast-banner:
    background: '{colors.fluent-neutral-background-raised}'
    foreground: '{colors.fluent-neutral-foreground}'
    border: '{colors.fluent-neutral-stroke}'
    radius: '{rounded.sm}'
---

## Brand & Style

Hexalith.ChatBot is an enterprise work surface for project-centered conversations where people, external parties, service clients, and governed AI actors operate in the same traceable context.

The visual posture is inherited rather than invented: Hexalith.FrontComposer and Microsoft Blazor Fluent UI v5 define the component grammar, density, accessibility affordances, focus rings, and baseline color behavior. This DESIGN.md specifies product-level emphasis only: project context first, evidence visible, risk legible, and audit history close to the action.

Decision: the product should read as a quiet operational SaaS tool, not as a playful assistant, marketing chatbot, or consumer messaging app. The interface should feel closer to an enterprise command workspace than to a social chat feed.

## Colors

The palette inherits Fluent UI v5 tokens through FrontComposer. The authoritative implementation source is FrontComposer's Fluent UI v5 integration and theme CSS custom properties in `Hexalith.FrontComposer`, currently pinned through `Microsoft.FluentUI.AspNetCore.Components` and documented in `Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md`. Product-specific color meaning is semantic:

- **Neutral background and foreground** provide the default project workspace, conversation panes, queues, and audit surfaces.
- **Brand** identifies primary actions and selected navigation only.
- **Information** marks evidence, context, candidate rationale, and non-terminal status.
- **Warning** marks ambiguity, approval-required actions, stale evidence, degraded dependencies, and manual review.
- **Danger/Error** marks blocked, unauthorized, failed, quarantined, rejected, and terminal states.
- **Success** marks completed association, approved action, stored attachment, command success, and completed audit projection.

Do not create a separate chatbot color language. The same status semantics must hold across project conversations, association queues, approval panels, admin views, audit views, CLI documentation snippets, and MCP tool descriptions.

Contrast requirements:

| Pair | Required token pair | Minimum |
|---|---|---|
| Default text on page/workspace surface | `{colors.fluent-neutral-foreground}` on `{colors.fluent-neutral-background}` | WCAG 2.2 AA normal text, 4.5:1 minimum. |
| Metadata, muted helper text, disabled explanation | `{colors.fluent-neutral-foreground-muted}` on `{colors.fluent-neutral-background}` or `{colors.fluent-neutral-background-raised}` | 4.5:1 when text communicates status or recovery. Purely decorative muted text follows Fluent's non-essential-text guidance. |
| Primary action and selected navigation | `{colors.fluent-brand-foreground}` on `{colors.fluent-brand}` | 4.5:1 text, 3:1 non-text UI. |
| Evidence/info chip or banner | `{colors.status-info-foreground}` on `{colors.status-info}` | 4.5:1 text, 3:1 chip boundary/focus affordance. |
| Warning/risk chip or banner | `{colors.status-warning-foreground}` on `{colors.status-warning}` | 4.5:1 text, 3:1 chip boundary/focus affordance. |
| Danger/blocked/error state | `{colors.status-danger-foreground}` on `{colors.status-danger}` | 4.5:1 text, 3:1 non-text UI. |
| Success/completed state | `{colors.status-success-foreground}` on `{colors.status-success}` | 4.5:1 text, 3:1 non-text UI. |
| Inline code, identifiers, links, focus ring | Inherited Fluent/FrontComposer code, link, and focus-ring tokens | 4.5:1 text; focus indicator area and contrast must satisfy WCAG 2.2 Focus Appearance. |

High-contrast and dark-mode variants are inherited from Fluent UI and FrontComposer. The same minima (4.5:1 text, 3:1 non-text) apply to both light and dark themes. Under Windows High Contrast / forced-colors, status meaning for evidence, risk, danger, and success chips and banners must survive via icon, text label, or border — not background fill alone. Product-specific wrappers must not override these pairs with raw CSS colors unless the replacement is tested against the same ratios.

## Typography

Typography inherits Fluent UI and FrontComposer defaults.

- Page titles identify the current project, queue, policy area, or audit investigation.
- Section titles stay compact and scannable; this product has operational density.
- Body text explains evidence, proposed actions, approval reasons, and recovery paths.
- Metadata typography is used for source message IDs, timestamps, confidence, policy snapshots, actor type, correlation ID, and command surface.
- Monospace appears only for stable identifiers, command names, state names, and technical evidence.

Avoid oversized hero type inside authenticated product surfaces. Users are returning to resolve work, not reading a landing page.

## Layout & Spacing

The layout should favor stable, scan-friendly work surfaces:

- Desktop/laptop is the primary surface for MVP workflows.
- Responsive web remains usable on tablet and phone for reading, simple approval, and status lookup.
- Dense lists and queues use FrontComposer list/table conventions.
- Conversation surfaces should preserve a clear project context header and avoid hiding workflow state behind decorative chat bubbles.
- Details appear in side panels, drawers, or inline expanders depending on available width and FrontComposer conventions.

Decision: the default authenticated layout uses persistent navigation on desktop and collapses navigation to a drawer/sheet on small screens, following FrontComposer and Fluent UI responsive patterns.

## Elevation & Depth

Depth is functional. Use elevation to separate:

- Active conversation from surrounding project navigation.
- Open review or approval panel from underlying conversation.
- Dialogs and sheets from the base workflow.
- Toasts and alerts from page content.

Do not use decorative cards to create visual richness. When a surface is important, importance comes from placement, state, and action availability, not extra shadow.

## Shapes

Shapes inherit Fluent UI and FrontComposer radius tokens.

- Buttons, inputs, menus, tabs, drawers, dialogs, cards, and panels use library defaults.
- Evidence chips, status tags, actor badges, and risk labels use compact tokenized shapes.
- Avoid large pill-heavy layouts. Status chips are acceptable; whole sections should not become pill-shaped.

## Components

- **Project context header**: Compact persistent anchor for authorized project identity, tenant context when relevant, current conversation/state, and safe status. Use `{components.project-context-header}`.
- **Conversation shell**: The main work surface. Shows project context, conversation stream, composer/action entry, actor identity, attached files, and current workflow state. Use `{components.conversation-shell}`.
- **Conversation stream**: Ordered event surface for messages, mailbox events, AI proposals, approvals, commands, retries, corrections, and system events. Use `{components.conversation-stream}`.
- **Composer/action entry**: Message and AI-request entry point. It inherits Fluent input/button styling and uses `{components.composer-action-entry}` for containing surface and border.
- **Actor badge**: Identifies human user, external party, service client, AI actor, background worker, CLI, MCP, or mailbox event. Visual treatment must remain compact and accessible. Use `{components.actor-badge}`.
- **Evidence chip**: Marks candidate association signals such as project alias, sender match, thread ID, attachment metadata, prior correction, or mailbox rule. Use `{components.evidence-chip}`.
- **Risk chip**: Marks why an AI action requires review: externally visible, file-exposing, project-mutating, tool-invoking, task-creating, or participant-representing. Use `{components.risk-chip}`.
- **Attachment row**: File/status row for stored, scanned, duplicate, retrying, blocked, or AI-context-eligible attachments. Use `{components.attachment-row}`.
- **Evidence drawer**: Side panel or drawer for source evidence. Use `{components.evidence-drawer}`; it must visually separate evidence from the active decision without appearing as a second conversation.
- **AI proposal panel**: Reviewable AI action preview. Use `{components.ai-proposal-panel}` and warning semantics until approved or rejected.
- **Approval panel**: Presents proposed action, requester, project, files, recipient/destination, policy reason, expected command, and approve/reject/revise/cancel actions. Use `{components.approval-panel}`.
- **Approval controls**: Action group for approve, reject, request revision, cancel, retry, or escalation. Primary approval uses `{components.approval-controls.primary-background}` only when all preconditions are satisfied.
- **Association candidate row**: Shows candidate project, confidence, evidence, authorization-safe status, and decision actions. Use `{components.association-candidate-row}`.
- **Queue row**: Shows workflow item age, state, next action, assignee, confidence/risk, and terminal/non-terminal status. Use `{components.queue-row}`.
- **Audit timeline**: Shows ordered events with actor, command surface, decision, policy snapshot, correlation ID, and outcome. Use `{components.audit-timeline}`.
- **Blocked state**: Explains denial, quarantine, unresolved party, missing authorization, failed dependency, or unsafe project context without leaking restricted detail. Use `{components.blocked-state}`.
- **Status toast/banner**: Transition feedback for accepted commands, background updates, retry queued, degraded dependencies, or validation failures. Use `{components.status-toast-banner}`; persistent states must also live on the relevant surface.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit Fluent UI v5 and FrontComposer defaults wherever possible. | Invent a custom visual design system for the chatbot. |
| Keep project, party, file, approval, and audit context visible near the conversation. | Treat the interface as a generic message feed. |
| Use semantic status color consistently for evidence, warning, failure, and success. | Use color decoratively or vary meanings by surface. |
| Make risky AI actions visually distinct before execution. | Let AI output look equivalent to completed work before approval. |
| Keep operational lists dense but readable. | Use oversized cards or marketing-style empty sections in workflow surfaces. |
| Show stable identifiers in metadata style or monospace when useful. | Make users copy IDs from paragraphs or hidden diagnostics. |
