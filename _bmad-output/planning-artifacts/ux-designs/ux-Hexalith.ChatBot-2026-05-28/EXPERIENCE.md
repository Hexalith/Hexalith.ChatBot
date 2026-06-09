---
name: Hexalith.ChatBot
status: final
created: 2026-05-28
updated: 2026-06-05T12:12:05+02:00
sources:
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
  - ../../product-brief-Hexalith.ChatBot.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd-validation-report.md
---

# Hexalith.ChatBot - Experience Spine

## Foundation

Responsive enterprise web application. The UI foundation is Hexalith.FrontComposer, integrated as a submodule of the main project, with Microsoft Blazor Fluent UI v5 as the component system. Visual inheritance chain: Fluent UI v5 → Hexalith.FrontComposer (theme and component integration) → `DESIGN.md` (semantic narrowing) → this document (behavioral spec). `DESIGN.md` is the visual identity reference; this document owns information architecture, behavior, states, interactions, accessibility, and journeys.

Primary MVP context: governed email-to-project collaboration where conversations, attachments, project association, AI actions, approvals, and audit history stay tied to tenant, project, party, folder, memory, and command boundaries.

Decision: primary use is desktop/laptop for project managers, contributors, tenant admins, developers, support, and compliance reviewers. Mobile/responsive use supports triage, reading, simple decisions, and status lookup, but not full admin configuration.

UX scope boundary:

| Source tension | UX decision |
|---|---|
| Product brief names generic email integration; PRD narrows MVP to controlled mailbox collaboration. | Generic email provider differences inherit the same intake, association, authorization, attachment, and audit surfaces. Provider-specific authoring is not a separate UX surface unless the PRD changes. |
| Product brief names scheduled-time and file-addition automated triggers; PRD narrows first UX contract to mailbox-driven collaboration and approved commands. | Scheduled-time and file-addition triggers appear as command/event origins in Operational Queues, Conversation Detail, AI Action Review, and Audit Investigation. Trigger-authoring UI beyond tenant policy/configuration is later workflow scope unless the PRD changes. |

Visual reference decision: this update intentionally keeps the UX contract spine-only. No mockups, wireframes, or imports are required for MVP handoff; downstream builders should implement Project Workspace, Conversation Detail, Association Review, AI Action Review, Files and Context, Operational Queues, Audit Investigation, Tenant Configuration, and Command Surface Reference from the IA, component, state, interaction, and accessibility tables in this file. Future visual mockups may extend the handoff, but the spines win on conflict.

## Information Architecture

| Surface | Reached from | Purpose |
|---|---|---|
| Project Workspace | App open, project switcher, deep link | Project-centered conversation, context, files, AI interaction, and current work state. |
| Conversation Detail | Project workspace, search, audit link | Multi-actor conversation stream with messages, parties, attachments, task intent, AI proposals, approvals, and outcomes. |
| Association Review | Workspace alert, queue, mailbox status | Resolve ambiguous or failed email-to-project association with candidate evidence. |
| AI Action Review | Conversation proposal, approval queue, notification | Approve, reject, revise, or cancel proposed risky AI actions before execution. |
| Files and Context | Project workspace, conversation attachment, AI proposal | Show governed folders, stored attachments, memory/index status, and context eligibility. |
| Operational Queues | Navigation, admin dashboard, notification | Ambiguous associations, unresolved parties, pending approvals, failed ingestion, retryable work, quarantine. |
| Audit Investigation | Conversation event, project audit, support search | Reconstruct association decisions, approval history, command execution, correction, retry, and AI outcomes. |
| Tenant Configuration | Admin navigation | Mailbox patterns, party resolution rules, confidence thresholds, approval policies, service clients, notifications. |
| Command Surface Reference | Developer navigation or docs link | Explain UI/CLI/MCP parity, stable command names, status codes, reason codes, and audit attribution. |

IA closure: every stated need maps to a surface. Conversation work lands in Project Workspace and Conversation Detail; ambiguous association lands in Association Review; AI risk lands in AI Action Review; files and memory land in Files and Context; operations land in Operational Queues; traceability lands in Audit Investigation; tenant governance lands in Tenant Configuration; CLI/MCP parity lands in Command Surface Reference and shared backend behavior.

## Voice and Tone

Microcopy is factual, specific, and safe. Brand posture lives in `DESIGN.md`.

| Do | Don't |
|---|---|
| "This message needs project review." | "We found a possible project!" |
| "3 candidate projects. Confidence is close." | "AI is unsure." |
| "Approval required: external reply may include project files." | "This seems risky." |
| "Association blocked. You do not have access to this project." | "Project exists but permission denied." |
| "Retry queued. No duplicate files were created." | "Retrying..." without consequence. |
| "Audit projection is pending. Command accepted." | "Done." when only part of the workflow completed. |

Error and denial language must not reveal unauthorized project names, file metadata, candidate evidence, or sensitive audit details.

## Component Patterns

Behavioral specs. Visual specs live in `DESIGN.md.Components` and inherited Fluent UI/FrontComposer components.

| Component | Use | Behavioral rules |
|---|---|---|
| Project context header | Workspace, conversation, approval, audit | Always shows authorized project identity, tenant context when relevant, current conversation/state, and safe status. |
| Conversation shell | Project workspace, conversation detail | Owns the two-part relationship between project context and active conversation. It keeps workflow state visible while panels, evidence, and approvals open. |
| Conversation stream | Conversation detail | Orders human, external party, mailbox, AI, CLI/MCP, background, trigger, and system events with actor attribution. System decisions are not hidden as chat messages. |
| Composer/action entry | Conversation detail | Supports user messages and AI requests. When a request implies risky action, create a proposal instead of executing. |
| Actor badge | Conversation, audit, approvals | Identifies actor type and resolved party/user/client. Must distinguish all eight categories named in `DESIGN.md.Components` (human user, external party, service client, AI actor, background worker, CLI, MCP, mailbox event) with a stable label and icon affordance; the same visual token applies across categories — differentiation is by accessible label and icon, not color. Unresolved actors show an unresolved state and safe actions. |
| Attachment row | Conversation, files, approval | Shows storage status, scan status, folder link, duplicate/retry state, and whether the file is eligible for AI context. |
| Association candidate row | Association review | Shows project candidate, confidence band, evidence chips, unavailable/unauthorized suppression, and actions: confirm, reject all, defer, escalate/manual review. |
| Evidence chip | Association, approval, audit | Summarizes one evidence reason with text and semantic status. Chip click or keyboard activation opens the supporting evidence when permitted. |
| Risk chip | AI proposal, approval, audit, queue | Names the risk class in plain language and exposes the policy reason that caused review. |
| Evidence drawer | Association, approval, audit | Expands source evidence without forcing users to read the full email thread. Redacts inaccessible details. |
| AI proposal panel | Conversation, approval queue | Shows requester, project scope, input files, intended command, risk class, destination, policy reason, and expected result. |
| Approval controls | AI action review | Approve, reject, request revision, or cancel. Disabled approval, association, and correction controls must either remain focusable with `aria-disabled="true"` and an announced reason, or be paired with an adjacent focusable "Why unavailable?" affordance. Tooltip-only or default-non-focusable disabled state is insufficient. |
| Approval panel | AI action review | Presents proposed action details and controls as one review unit. It remains pending until the required authorization path succeeds. |
| Queue row | Operational queues | Displays state, age, risk, confidence, assignee, next required action, retry count, and terminal/non-terminal status. |
| Audit timeline | Audit investigation | Chronological, filterable reconstruction of source message, association, corrections, approvals, commands, AI actions, and outcomes. |
| Blocked state | Any secured surface | Explains denial, unresolved association, quarantine, failed dependency, or unsafe context with safe next action and redacted details. |
| Status toast/banner | Global, queues, detail | Used for transition feedback only. Long-lived operational states belong on the relevant surface. |

## State Patterns

| State | Surface | Treatment |
|---|---|---|
| Cold app load | Workspace | Skeleton matching project navigation, conversation list, and detail pane. |
| No project selected | Workspace | Project picker or recent authorized projects. No marketing hero. |
| Empty project conversation | Conversation detail | Show project context and a simple start action; include mailbox setup/status if relevant. |
| Email received | Conversation/queue | Show intake item with source, party state, attachment count, and association status. |
| Candidate generated | Association review | Show ranked authorized candidates with evidence and confidence. |
| Ambiguous association | Association review | No auto-attach. Require confirm, reject, defer, or manual review. |
| Associated | Conversation detail | Message appears in project context with evidence available from details. |
| Corrected association | Conversation/audit | Preserve original audit event, update linkage, show correction rationale and derived-context invalidation where relevant. |
| Unresolved party | Queue/conversation | Show safe identity evidence and actions to link, create pending party, reject, or quarantine. |
| Attachment pending scan | Files/context | Block AI/file exposure until policy permits; show scan state and next action. |
| AI proposal ready | Conversation/approval | Pause before execution; show risk reason and approval controls. |
| Approval rejected | Conversation/audit | Keep rejection reason visible and prevent execution. |
| Command accepted, projection pending | Conversation/status | Show partial success with operation identity and audit projection status. |
| Retryable failure | Queue/detail | Show retry action, retry count, reason, and duplicate-safety note. |
| Terminal failure | Queue/detail | Show reason, escalation/manual resolution path, and audit availability. |
| Unauthorized | Any surface | Fail closed with redacted explanation; do not confirm resource existence. |
| Dependency degraded | Workspace/admin | Scope impact to tenant, mailbox, project, service, or operation; show current safe actions. |

Surface state coverage:

| Surface | Required states |
|---|---|
| Project Workspace | Cold load; no project selected; empty project conversation; active conversation selected; dependency degraded; unauthorized/redacted; project switch success. |
| Conversation Detail | Loading history; empty conversation; streaming/update pending; attachment scan pending; AI proposal ready; command accepted/projection pending; correction applied; retryable failure; terminal failure; unauthorized/redacted. |
| Association Review | Candidate loading; no authorized candidates; ambiguous candidates; candidate selected; validation error; confirm success; reject/defer/escalate success; unauthorized candidate suppressed; retryable intake failure; quarantined/terminal failure. |
| AI Action Review | Proposal loading; ready for review; missing context; approval blocked by permission; approve/reject/revise/cancel success; policy denied; execution pending; execution success; retryable execution failure; terminal execution failure. |
| Files and Context | Folder/context loading; no files; file selected; upload/intake pending; scan pending; duplicate suppressed; memory/index pending; AI-context eligible; unauthorized/redacted file; storage retryable failure; terminal storage failure. |
| Operational Queues | Queue loading; empty filtered queue; row selected; stale filters; retry queued; batch action validation error; dependency degraded; unauthorized row redacted; terminal queue item; completed item removed or archived. |
| Audit Investigation | Audit loading; no matching events; event selected; filters active; projection pending; redacted detail; export/copy unavailable; retry/correction trace present; terminal command outcome; investigation handoff/escalation logged. |
| Tenant Configuration | Settings loading; first-run empty; field editing; validation summary; save pending; save success; policy conflict; mailbox permission degraded; unauthorized admin action; rollback/cancel available; terminal configuration failure. |
| Command Surface Reference | Loading; no commands available; command selected; permission redacted; stale schema; example copied; version mismatch/degraded; parity test failure linked; successful parity state. |

State-to-feedback matrix:

| State family | Feedback primitive |
|---|---|
| Loading/cold load | Skeleton matching final layout with `aria-busy="true"` on the busy region. Clear `aria-busy` on the same node when content swaps in; preserve focus inside the region or move it to a labelled landing point. Newly loaded historical content does not announce. |
| User-triggered success | Inline status on the affected row/panel plus optional polite toast. Keep audit link when relevant. |
| AI proposal ready (current user's request) | One polite announcement on the user's own request; do not re-announce on view re-entry. |
| Command accepted / projection pending | One polite announcement with operation identity; do not repeat on each poll. Persistent inline status carries ongoing detail. |
| Approval rejected (current user's submitted action) | Assertive announcement plus inline rejection reason with focus reachable. |
| Approval rejected (observed in a queue for someone else) | No live announcement; row-level inline status only. |
| Projection pending / partial success | Persistent inline status or banner with operation identity and audit/projection status; polite live region. |
| Validation error | Error summary before the affected form/review panel, field-level errors where fields exist (each invalid input carries `aria-invalid="true"` and an `aria-describedby` link to its message), focus moved to summary. |
| Approval/association blocked | Persistent inline blocked state with reachable explanation and safe next action; do not rely on disabled control tooltip alone. |
| Retryable failure | Persistent row/panel status with retry action, retry count, duplicate-safety note, and polite announcement. |
| Terminal failure / policy denial | Persistent alert or blocked state with escalation/manual path; assertive announcement only when caused by the user's current action. |
| Dependency degraded | Scoped banner on affected surface, not global alarm unless the whole tenant/app is impacted. |
| Background update while reading history | Non-interrupting "new updates" affordance; no forced scroll. |

## Interaction Primitives

Primary interactions:

- Select a project, conversation, queue item, candidate, file, approval, or audit event.
- Expand evidence inline or in a side panel.
- Confirm, reject, defer, correct, retry, quarantine, approve, request revision, cancel, or escalate.
- Ask AI for help from a project conversation, with proposals generated for risky actions.
- Filter queues and audit views by state, age, risk, confidence, project, mailbox, actor, reason, correlation, and time.
- Open command palette/search where FrontComposer supports it. Keyboard-first shortcuts are valuable for developers and operators; equivalent labelled controls remain available for business contributors.
- Interrupt a streaming AI response or AI proposal generation: a Stop/Cancel control is always keyboard-reachable while streaming, occupies a stable focusable position (no inline appear/disappear that steals focus), announces "Response stopped" politely on activation, and returns focus to the composer or the AI proposal panel.

Keyboard shortcuts conform to WCAG 2.1.4 Character Key Shortcuts: any single-character or modifier-free shortcut is disabled by default inside text-entry controls (composer, search field, filter inputs, configuration forms) and is globally remappable or disable-able from a "Keyboard shortcuts" entry in user preferences.

Banned or constrained interactions:

- No hidden auto-association when confidence is ambiguous.
- No AI execution of risky actions from a plain message send.
- No hover-only critical actions.
- No modal stacks beyond one active dialog/sheet.
- No infinite scroll for operational queues; use pagination or virtualized list behavior with stable filters.
- No direct UI affordance that suggests CLI/MCP/admin bypass of authorization.

Keyboard and focus model:

- Keyboard operation is required for all workflows. Advanced shortcuts are optional enhancements for developers/operators, not the only accessible path.
- Page navigation exposes landmarks for navigation, project context, main conversation/detail, complementary evidence/review panel, queue filters, and status region. Repeated landmark roles within a single surface (for example a Conversation Detail with both an Evidence drawer and an AI proposal panel mapped to `complementary`) must carry a unique `aria-label` so screen-reader users can distinguish them.
- Initial focus lands on the surface heading or first actionable review item after navigation; dialogs/sheets trap focus and return focus to the invoking control on close.
- Conversation stream focus is stable: Tab reaches message/event groups and their actions; arrow-key or roving-focus behavior may be used inside timelines/lists only when labels announce position and count.
- Approval, association, retry, correction, and tenant-configuration submissions move focus to success status or error summary. Rejected/blocked actions keep focus in the review panel with the reason reachable.
- Escape closes the topmost non-destructive popover/sheet/dialog. It must not discard unsaved edits without an explicit confirmation path.
- Disabled or unavailable actions must have a reachable explanation through helper text, inline status, or an enabled "Why unavailable?" affordance; tooltip-only explanation is insufficient.

Conversation and audit semantics:

- The conversation stream is a chronological event list grouped by day or source thread where useful. Each group has an accessible heading.
- Every message/event exposes actor type, permitted identity label, timestamp, source surface, and state label. System events are labelled as system decisions, not anonymous messages. The actor-type label (for example "AI actor", "Service client", "External party") precedes message content in the accessible name and description so screen-reader users hear the actor before the content.
- Attachments render as labelled lists or tables with filename display, storage/scan/context state, and allowed actions. Restricted metadata is redacted consistently.
- AI proposal panels are programmatically related to the source request/message and name the risk class, policy reason, files, destination, and expected command.
- Audit timeline entries expose event type, actor, timestamp, correlation ID, command surface, policy snapshot, outcome, and links to permitted source evidence.
- Historical messages and audit events do not announce on initial load. Only new user-relevant changes use live regions, with politeness matching the state-to-feedback matrix.

Reduced motion and auto-scroll:

- Do not force-scroll when the user is reading earlier conversation or audit history; show a keyboard-reachable "new updates" control.
- For `prefers-reduced-motion`, suppress shimmer skeletons, row movement animation, streaming text animation, and non-essential panel transitions.
- Queue row insertion/reordering must preserve focus and selection; use status text rather than movement as the only cue.
- Progress must have non-motion text such as "Scanning attachment" or "Projection pending."

## Accessibility Floor

Behavioral floor; visual contrast lives in `DESIGN.md`.

- WCAG 2.2 AA for core UI workflows: project conversation, association review, AI approval, queues, audit, and tenant configuration.
- All action controls expose role, label, state, disabled reason, and keyboard operation.
- Focus order follows visible reading/action order.
- Status updates for association, approval, command, retry, and projection changes use appropriate live-region behavior without noisy repeated announcements.
- Evidence chips and risk chips must not rely on color alone; text labels are required.
- Queue filtering, candidate selection, approval actions, and audit timeline navigation must be fully keyboard-operable.
- Reduced motion suppresses non-essential transitions in conversation updates, queue row movement, and panel open/close behavior.
- Touch targets on responsive layouts meet Fluent UI/platform guidance and are testable: touch-primary controls use at least 44 by 44 CSS pixels where layout allows; compact table/list controls must meet WCAG 2.2 AA target size with at least 24 by 24 CSS pixels or equivalent spacing from adjacent targets. Destructive and approval controls must not rely on compact-only sizing on phone or tablet.
- Redacted/unauthorized states must remain understandable to screen reader users without leaking hidden content.
- Export, copy-to-clipboard, download-transcript, "read aloud", and any other off-surface affordance must apply the same redaction as the visual surface. The exported artifact's accessible name and description must not contain redacted source text, and the surface must expose a screen-reader-equivalent message that the export is redacted and full detail requires escalation.

Error recovery patterns:

| Flow | Recovery requirement |
|---|---|
| Association review | Error summary names the safe failure category, preserves candidate selection when allowed, focuses the summary, and offers confirm/reject/defer/escalate only when still valid. |
| AI action review | Externally visible, file-exposing, project-mutating, tool-invoking, or participant-representing actions require explicit confirmation copy before execution. Rejection/revision/cancel outcomes remain audit-visible. |
| Queue retry | Retry controls state duplicate-safety and retry count. A failed retry returns focus to the row status and keeps the next safe action visible. |
| Correction | Correction requires a rationale where policy demands it, previews affected attachments/derived AI context, and reports success, partial success, or blocked target without leaking unauthorized project details. |
| Tenant configuration | Validation summary appears before fields, field-level errors stay near controls, and save conflicts explain whether the current policy, mailbox permission, or stale data caused the failure. |

Cognitive-load guardrails:

- Each workflow item has one primary next action; secondary and destructive actions are grouped after the primary decision.
- Evidence, risk, status, actor, and timestamp appear in consistent order across candidate rows, proposals, queues, and audit entries.
- Plain-language summaries precede raw IDs; IDs remain available in metadata or expandable detail.
- Filters show a visible summary of active filters and result count.
- Prefer one consolidated banner/panel per surface state over stacked alerts.
- Dense tables reflow to labelled rows on small screens without dropping labels, state, reason, or safe actions.

Localization:

- English and French are supported UX languages unless product scope changes.
- Stable machine codes, status codes, reason codes, command names, and correlation IDs remain untranslated; display labels and explanations are translated.
- Dates, times, numbers, confidence bands, pluralization, and actor labels use locale-aware formatting.
- Avoid concatenated strings for accessible names and state descriptions.
- Buttons, chips, rows, and table columns allow text expansion for French without truncating critical state or action words. Critical state and action words wrap, use an approved short label, or move into labelled row detail before truncation. Columns allowed to collapse first: raw IDs, secondary timestamps, low-priority metadata, and repeated project/tenant context already visible in the surface header. Columns that must remain visible or move into labelled row detail: actor, risk, state, confidence, next action, and safe recovery reason.

## Responsive & Platform

| Breakpoint | Behavior |
|---|---|
| Desktop/laptop | Persistent navigation, project list/queue, conversation detail, and side panel can coexist. Best surface for full workflow. |
| Tablet | Navigation collapses; conversation and detail panel may stack. Association and approval remain complete. |
| Phone | Reading, approval, defer/reject/confirm, status lookup, and simple AI request. Full tenant configuration and dense audit analysis use the small-screen fallback pattern below. |

The product is responsive web through Blazor/FrontComposer, not a native mobile application. CLI and MCP are separate command surfaces with equivalent backend state transitions, not visual breakpoints of the UI.

Small-screen fallback pattern: when a workflow is too dense for phone, keep read-only summary, status, safe approve/reject/defer/confirm actions, copy/share handoff link, and "open on larger screen" guidance available. Disable dense editing or admin-only controls with reachable explanation and no tooltip-only dependency. Preserve draft or filter state when routing to a larger screen. Screen reader users hear the same limitation, remaining actions, and recovery path from the surface heading or first blocked-state panel.

Touch targets for phone/tablet approval, association, filters, attachment actions, timeline filters, search, drawer close, and destructive actions must use at least 44 by 44 CSS pixels where layout allows. Compact controls in dense rows must meet WCAG 2.2 AA target size with at least 24 by 24 CSS pixels or equivalent spacing. When dense tables collapse, each row must retain visible labels for project, actor, risk, state, confidence, time, and next action.

## Inspiration & Anti-patterns

Inspiration: Claude Code / OpenAI Codex (conversation as a work surface where AI proposes actions and reports outcomes) and ChatGPT (familiar conversational entry). Posture and rejected patterns are enforced by §Foundation, §Voice and Tone, §Component Patterns (`AI proposal panel`, `Blocked state`), and §Interaction Primitives (banned interactions) — see those sections rather than restating here.

## Product-Specific Concerns

| Concern | UX requirement |
|---|---|
| Internationalization | Product must support English and French UI text because stakeholder discovery is in French and project config outputs English. |

## Key Flows

### Flow 1 - Project contributor asks AI for help (Amira, delivery contributor, after receiving a customer email)

1. Amira opens the authorized project workspace.
2. The latest email-derived conversation item is visible with resolved external party, attachments, and association evidence.
3. She asks the AI to compare the attachment with current project folder content and draft a response.
4. The system creates an AI proposal instead of sending or mutating anything.
5. Amira opens the proposal and sees project scope, requester, input files, recipient, intended command, and risk reason.
6. **Climax:** Amira approves the proposed action with confidence that the system will use authorized project context and record the result.
7. The executed command outcome appears in the conversation with audit history available.

Failure: if context is insufficient, the AI asks for files or clarification. If approval is rejected, the proposal remains visible with rejection reason.

Source journey mapping: Journey 8 covers the review step in more detail as Flow 8.

### Flow 2 - Ambiguous association resolution (Marc, project contributor, morning triage)

1. Marc opens Association Review from the unresolved queue.
2. A message from a known external party has multiple authorized candidate projects.
3. Each row shows confidence and evidence: sender match, thread reference, project alias, attachment metadata, prior association, or prior correction.
4. Marc expands evidence for the top candidates without opening multiple systems.
5. He confirms the correct project, rejects all, defers, or escalates if evidence is insufficient.
6. **Climax:** The message becomes project context only after Marc's explicit decision, and the decision is audited.

Failure: if no candidate is viable, the item remains unresolved or quarantined with next action, not silently attached.

### Flow 3 - External party sends project context (Elena, supplier, using ordinary email)

1. Elena sends an email with a request and attachment to the controlled project mailbox pattern.
2. The system ingests the message, preserves source identifiers, and resolves Elena through Hexalith.Parties when possible.
3. Authorization and project association run before project context is exposed.
4. If deterministic evidence is safe, the message is associated; if not, it goes to review.
5. **Climax:** Elena keeps using email while the internal team receives governed project conversation context, stored attachments, and auditable follow-up.

Failure: unresolved or unauthorized sender states fail closed and expose only safe review actions.

### Flow 4 - Project owner repairs a wrong association (Priya, project owner, sensitive delivery project)

1. Priya notices an email-derived item that does not belong in her project.
2. She opens association details and sees original evidence, actor, timestamp, confidence, candidates shown, and downstream artifacts.
3. She corrects the association or marks the item misfiled.
4. The system preserves original audit history, updates project linkage, relinks or blocks attachments according to policy, and invalidates derived AI context where needed.
5. **Climax:** Priya repairs contaminated context without erasing the record of what happened.

Failure: if Priya lacks authority for the target project, the correction flow suppresses restricted details and routes to authorized review.

### Flow 5 - Tenant admin configures governed collaboration (Nora, tenant admin, rollout week)

1. Nora opens Tenant Configuration.
2. She configures monitored mailbox patterns, party resolution rules, confidence thresholds, low-risk AI policy, approval requirements, and audit visibility.
3. She reviews operational queues for unresolved parties, ambiguous matches, duplicate suppression, rejected associations, approval aging, and failed command execution.
4. She verifies that unauthorized projects never appear in candidate or evidence views.
5. **Climax:** Nora sees that the workflow fails closed and remains operable under messy mailbox conditions.

Failure: if Microsoft 365 permissions are revoked or throttled, the affected mailbox shows degraded state and safe recovery steps without broad fallback access.

### Flow 6 - Developer uses CLI parity (Leo, automation builder, incident support)

1. Leo uses the CLI to list unresolved associations.
2. The CLI returns the same ordered candidates, evidence fields, status codes, and redaction semantics as the UI.
3. He confirms an association, checks attachment status, and queries the audit record.
4. If projection is delayed, the CLI returns operation identity and partial-success status.
5. **Climax:** Leo can script governed operations without bypassing the same authorization, approval, and audit model.

Failure: stale credentials, tenant switch, or revoked service-client scope fail closed without revealing restricted project existence.

### Flow 7 - Compliance or support reviewer investigates a risky action (Sofia, support reviewer, after a reported concern)

1. Sofia opens Audit Investigation from a reported conversation event or support search.
2. She searches by source message ID, correlation ID, project, requester, command surface, actor, time range, or policy reason.
3. The audit timeline shows permitted details: source message, tenant, project, requester, party identities, candidate evidence, selected association, rejected alternatives, input files, approval policy, approval decision, command surface, model/agent identity, executed command, destination, and outcome.
4. Sofia filters for retries, corrections, rejections, deferrals, duplicate suppression, and projection-pending states.
5. If evidence is redacted by permission, the row explains that detail is restricted and offers an escalation path without revealing the hidden resource.
6. **Climax:** Sofia reconstructs who initiated the action, which project context and files influenced it, what policy applied, what output was produced, and where it went.

Failure: if Sofia lacks authority to mutate project state, investigation remains read/escalate only. If audit projection is delayed, the surface shows partial status and operation identity.

### Flow 8 - User reviews an AI action before it leaves the project boundary (Amira, delivery contributor, before an external reply)

1. Amira asks the AI to prepare a response that may include project file content and be sent externally.
2. The AI draft is ready, but execution pauses before email send, file exposure, project mutation, tool invocation, or participant representation.
3. Amira opens AI Action Review and sees context used, files referenced, proposed action, destination, policy rule, risk class, and expected command.
4. She expands evidence and confirms that the authorized project context is correct.
5. She approves, rejects, requests revision, or cancels.
6. **Climax:** Nothing leaves the project boundary until Amira's permitted decision succeeds, and the decision is audited with reason and policy rule.

Failure: if the request violates boundary or policy, the AI refuses or routes to approval. If authorization fails, denial is audited without leaking restricted resource details.

### Flow 9 - Governed AI execution (Ari, project-aware AI agent, handling a conversation request)

1. Ari receives a request from a conversation actor, UI, CLI, MCP, scheduled trigger, or file-addition trigger.
2. The system supplies scoped context: tenant, project, requester, authorized files, policy, permitted action types, approval requirement, and source traceability.
3. Ari performs low-risk read-only assistance when policy allows.
4. For risky operations, Ari creates a proposed action with expected command, risk class, destination, files, and policy reason instead of executing.
5. If association is unresolved, context is missing, or authorization fails, Ari refuses, asks for clarification, or routes to association/approval as appropriate.
6. **Climax:** Ari can move work forward without silently crossing tenant, project, file, tool, or external-communication boundaries.
7. Execution result is recorded through the same command/event/audit model as human, CLI, and MCP actions.

Failure: denied, rejected, retryable, terminal, and projection-pending outcomes remain visible on Conversation Detail, Operational Queues, AI Action Review, and Audit Investigation.
