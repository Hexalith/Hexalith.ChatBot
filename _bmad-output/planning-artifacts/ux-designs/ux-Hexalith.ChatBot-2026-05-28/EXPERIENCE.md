---
name: Hexalith.ChatBot
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
---

# Hexalith.ChatBot — Experience Spine

## Foundation

Hexalith.ChatBot is a responsive enterprise web application. Microsoft Blazor Fluent UI v5 → Hexalith.FrontComposer → `DESIGN.md` is the visual inheritance chain; this document owns information architecture, behavior, states, interactions, accessibility, and journeys. Every routable page uses the single FrontComposer shell with `FcPageLayout` and `FcPageHeader`.

Primary use is desktop/laptop for contributors, project owners, tenant administrators, operators, developers, support, and compliance reviewers. Tablet and phone support reading, triage, governed AI requests, and safe decisions; dense administration and investigation provide a state-preserving larger-screen handoff.

The current MVP is governed email-to-project collaboration plus the governed project conversation/composer. Its safe scope is explicit:

| Source tension | Binding UX decision |
|---|---|
| Product brief includes scheduled-time and file-addition automation. | These triggers and their authoring surfaces are future-only. MVP flows and origin lists do not imply that they execute. Conversation-triggered work is the only current automation trigger. |
| Product brief includes general user upload. | General user upload is post-MVP. MVP file intake is governed mailbox attachment capture only. |
| Product brief names generic email; current PRD centers controlled Microsoft 365/Exchange-style intake. | Any provider must satisfy the same controlled mailbox, identity, authenticity, attachment, idempotency, audit, and fail-closed contract. Provider-specific authoring is not a separate MVP surface. |
| PRD/addendum text has historically conflicted on boundary-effect downgrade. | Interim decision approved 2026-09-14: all six boundary-crossing effect classes require human approval. No tenant policy can downgrade them until upstream reconciliation is formally complete. |
| Risk classifier can be indeterminate. | Create a reviewable `approval-required` proposal only when its inputs, project, actor, command, and evidence are otherwise authorized and supported. Otherwise show an existence-neutral `denied` or `unsupported` outcome and disclose no unsafe candidate. |
| Association score is below `T_low`, empty, conflicted, non-finite, stale, or unsafe. | Enter `NeedsReview`. `T_low` affects ranking/presentation only. Show only candidates safe for the actor; an empty safe set remains reviewable without revealing suppressed candidates. |

This package is intentionally spine-only: there are no `imports/`, `mockups/`, or `wireframes/`. The IA and component/state tables are the implementation reference. Future visuals may illustrate them; these spines win on conflict.

## Information Architecture

### Source-surface crosswalk and closure

| PRD surface | UX surface | Reached from | Journey coverage | Load-bearing purpose |
|---|---|---|---|---|
| S1 — Project conversation view | Project Workspace / Conversation Detail, including Files and Context | `/`, project switcher, deep link, search | UJ1, UJ3, System Journey | Email-derived events, parties, attachments, classification, task intent, governed composer, AI results, approvals, and status. |
| S2 — Ambiguous association review | Association Review | Workspace alert, queue, mailbox status | UJ2, UJ3 | Compare authorized candidates/evidence and confirm, reject all, defer, or escalate without auto-attachment. |
| S3 — AI action approval | AI Action Review | Conversation proposal, approval queue, notification | UJ1, UJ8, System Journey | Review classification, authority, effects, evidence freshness, and approve/reject/revise/cancel. |
| S4 — Correction surface | Correction Surface within Association Review and Conversation Detail | Association details, audit link | UJ4 | Supersede an association, track derived-store acknowledgements, and block contaminated AI context. |
| S5 — Tenant admin configuration | Tenant Administration | Admin navigation | UJ5 | Govern mailbox, policy, approval routing, service clients, notifications, and bounded admin roles. |
| S6 — Outbound approval | Outbound Approval within AI Action Review | Outbound proposal | UJ8 | Freeze and review content, recipients, sender authority/delegation, files, and effects before send. |
| S7 — Cross-surface attribution view | Cross-surface Attribution / Command Surface Reference | Operation or audit link, developer reference | UJ6, UJ7, System Journey | Show normalized UI/CLI/MCP outcomes, immutable origin, operation identity, and parity version. |
| S8 — Operational dashboards | Operational Dashboards | Admin/operator navigation | UJ5, UJ7 | Health, queues, SLOs, budgets, freshness, ownership, and escalation. |
| S9 — Compliance investigation | Compliance Investigation | Audit link, support search | UJ7 | Reconstruct authorized evidence, policy, approval, command, correction, replay, and outcome history. |
| S10 — Admin queue operations | Admin Queue Operations | Dashboard or queue row | UJ5, UJ6 | Claim/assign, retry, requeue, quarantine, or dismiss at queue scope without mutating project records. |

The crosswalk is the IA closure proof. Files and Context is an S1 panel; approval queues feed S3/S6; correction queues feed S4; aggregate operational queues feed S8/S10. Every surface has a named journey and every stated MVP need lands on a surface.

Page composition follows the binding implementation addendum. A page with two or more sibling titled sections uses one `FluentAccordion`, primary item expanded, except for a single primary grid/form/workflow. On Association Review, `Association candidate group` and `Association decision bar` form one visible primary workflow outside the accordion; evidence comparison and source metadata are complementary accordion content.

Each surface story owns live-route acceptance for primary success, loading/empty, validation, unauthorized/redacted, degraded, retryable, terminal, keyboard/focus, responsive, light/dark/forced-colors, reduced-motion, English/French, and governed-command behavior. Static fixtures do not replace live-route evidence.

## Voice and Tone

Microcopy is factual, specific, existence-neutral, and action-oriented. Brand posture lives in `DESIGN.md`.

| Do | Don't |
|---|---|
| “This message needs project review.” | “We found a possible project!” |
| “No project is available for this decision. Escalate for authorized review.” | “You do not have access to Project Atlas.” |
| “Approval required: external communication.” | “This seems risky.” |
| “Approval unavailable (`evidence-expired`). Refresh evidence to continue.” | “Try again.” without cause or consequence. |
| “Correction delayed. Operations owns the next step.” | “Still working…” without owner or escalation. |
| “Prior outcome returned. No duplicate action occurred.” | “Done.” for a replay, conflict, or partial success. |
| “AI service unavailable. Manual review remains available.” | “The system is unavailable.” when only generation is affected. |

Every blocked/degraded/failed/denied state uses a versioned stable code, a headline no longer than 80 characters, one safe sentence, and a safe next action. User-facing copy never names an unauthorized project, file, party, candidate, or audit detail. Precise causes remain in authorized audit evidence.

## Component Patterns

Behavioral specs below pair exactly with `DESIGN.md.Components`; the visual reference column resolves to DESIGN frontmatter.

| Component | Visual reference | Behavioral rules |
|---|---|---|
| Project context header | `{components.project-context-header}` | Shows only authorized project identity, tenant context when relevant, current surface/state, and safe status. Project switch updates context and announces once. |
| Conversation shell | `{components.conversation-shell}` | Keeps project context, stream, composer, and complementary panels related while preserving selection, focus, and scroll. |
| Conversation stream | `{components.conversation-stream}` | Orders attributed human, external-party, mailbox, AI, UI/CLI/MCP, worker, and system events. System decisions are distinct events, not anonymous messages. |
| Composer/action entry | `{components.composer-action-entry}` | Separates user message from AI request; submits through the shared command spine; shows optimistic state only after admission; boundary effects create proposals. |
| Actor badge | `{components.actor-badge}` | Identifies actor type and permitted identity before content. Unresolved actors show a safe unresolved state without inferred identity. |
| Message classification | `{components.message-classification}` | Associates `informational` or `actionable` with the message in visible and accessible descriptions. Actionable items expose detected intent plus review/capture/dismiss. |
| Source evidence | `{components.source-evidence}` | Expanded by default. Each reference exposes source identity, permitted content, redaction, timestamp, and Evidence freshness; it remains authoritative over AI interpretation. |
| AI summary | `{components.ai-summary}` | Collapsed by default and labelled `AI summary`. Provenance (`model+version`, generated time, source-evidence IDs) precedes content; disclosure is keyboard-operable and preserves focus. |
| Why this project | `{components.why-this-project}` | Keyboard disclosure announces expanded state and returns focus. Labelled facts include signal class, matched value, confidence/band, actor, timestamp, and superseding correction links. |
| Evidence freshness | `{components.evidence-freshness}` | One chip per evidence reference; exposes timestamp and `fresh`/`stale`/`expired`. Expiry transitions announce once on the current review and block the affected decision with `evidence-expired`. |
| Task intent review | `{components.task-intent-review}` | Shows source message, ≤280-character intent summary, action kind, evidence offsets/excerpts, detector/kernel version, confidence, detected time, and state. Convert creates a governed proposal; dismiss dispositions remain auditable. |
| Attachment row | `{components.attachment-row}` | Shows capture/storage, scan/quarantine, folder, duplicate/retry, retention, and AI-context eligibility. Mailbox attachments only in MVP; no user-upload affordance. |
| Association candidate group | `{components.association-candidate-group}` | One named radiogroup and one Tab stop; arrows move selection and announce position/count. Selection never commits. Each option references confidence/evidence; unsafe candidates never render. |
| Association decision bar | `{components.association-decision-bar}` | Repeats the selected project in its accessible description and offers confirm, reject all, defer, or escalate. Confirm without selection focuses Error summary. |
| Action classification | `{components.action-classification}` | Keeps internal `low-risk`/`approval-required` classifier output distinct from user-visible `allowed-read-only`/`approval-required`/`denied`/`unsupported`. Shows classifier version and input tuple when reviewable. |
| AI proposal panel | `{components.ai-proposal-panel}` | Programmatically links the source request, project/context package, classification, and Approval authority and effects. It remains pending until a valid human decision and execution revalidation succeed. |
| Approval authority and effects | `{components.approval-authority-and-effects}` | Shows requester/origin, project, command/allowlist version, files/redaction/freshness, recipients, sender authority/delegation, classifier input/output/version, policy snapshot, reversibility, expected resource changes, side effects, audit events, and operation/proposal identity. |
| Approval controls | `{components.approval-controls}` | Approve, reject, request revision, cancel. Approve is focusable `aria-disabled` with an associated reason or has an adjacent focusable explanation. Submission revalidates authority, policy, evidence, allowlist, and effects. |
| Correction progress | `{components.correction-progress}` | Shows predecessor/successor, `Correcting`/`Correction-delayed`, acknowledged and remaining stores, estimate, owner, next action, and P2 escalation. Blocks affected AI context until all required stores acknowledge. |
| Bounded admin scope | `{components.bounded-admin-scope}` | Distinguishes aggregate see-only, queue-operate, mailbox, policy, and compliance scopes. Aggregate visibility never grants per-project detail or mutation authority. |
| Two-person approval | `{components.two-person-approval}` | Security-sensitive changes require a proposer, a distinct authorized second admin, justification, changed values, effective scope/time, policy version, and audit link. Self-approval is unavailable with a reason. |
| Shared operation status | `{components.shared-operation-status}` | Shows stable operation/idempotency identity, canonical state/reason, origin, retry count/ceiling, next attempt/eligibility, partial output, original-outcome link, and correlation. Prevents duplicate submit while pending. |
| Queue row | `{components.queue-row}` | Shows state, age, owner/assignee, risk/confidence, freshness, next action, retry count, and terminality. Per-item detail is redacted unless project authority succeeds. |
| Operational SLO dashboard | `{components.operational-slo-dashboard}` | Shows metric, numeric target or missing-support reason, window, error budget, alert threshold, calibration source, tenant scope, freshness, owner, and disposition. `unsupported` blocks the related M2 readiness claim. |
| Audit timeline | `{components.audit-timeline}` | Reconstructs source, actors, candidates/decisions, policy, approval, command, correction, replay, redaction, and outcome. Replay entries are labelled and excluded from production-completeness views by default. |
| Inbound authenticity and sender authority | `{components.inbound-authenticity-and-sender-authority}` | Shows provider-supplied DMARC/DKIM/SPF, header discrepancies, external-sender posture, delegate and `principal_for`, outbound authority class, membership/delegation evidence, and revalidation state. |
| Retention and export request | `{components.retention-and-export-request}` | Shows requested data classes, authorized scope, retention/legal-hold constraints, redaction, owner, operation status, and completed/partial/blocked outcome. Export or other exposure requires explicit authorized human confirmation; AI-mediated exposure is `approval-required`. Does not promise deletion where immutable audit handling requires tombstone/key-shred behavior. |
| Redacted support bundle | `{components.redacted-support-bundle}` | Exports correlation, state, reason, and next-action context only after redaction checks and explicit authorized human confirmation; never includes restricted project/party/file/message/evidence or secrets. External sharing is `approval-required`. Shows included/excluded summary before creation. |
| Blocked state | `{components.blocked-state}` | Uses existence-neutral copy, stable safe code, owner when applicable, and one safe action. Never exposes a suppressed candidate or confirms a resource exists. |
| Status toast/banner | `{components.status-toast-banner}` | Announces transitions; persistent state stays inline. Scope degradation narrowly and deduplicate repeated poll/update announcements. |
| Busy region | `{components.busy-region}` | Sets `aria-busy=true` on the replacing region, clears it on the same node, preserves/relands focus, and does not announce historical content. Reduced motion removes shimmer. |
| Error summary | `{components.error-summary}` | Appears before the affected form/review, receives focus on invalid submission, links to errors, and preserves valid selection/draft state. |
| Review dialog/sheet | `{components.review-dialog-sheet}` | One modal layer; traps and returns focus; Escape closes only non-destructively and never discards edits without confirmation. |
| Queue filter bar | `{components.queue-filter-bar}` | Server-side filters, pagination ≤100, visible active-filter summary/result count, stable focus/selection on refresh, and labelled small-screen reflow. |

## Governed Action Boundary

### Classifier output versus user-visible disposition

| Evaluation result | Internal classifier output | User-visible disposition | UX behavior |
|---|---|---|---|
| Authorized, supported, versioned read-only/no-external-effect subtype and policy allows | `low-risk` | `allowed-read-only` | Execute through the governed path; show source/provenance and audit outcome. |
| Any project-state mutation, file exposure, external communication, task creation/assignment, external-tool invocation, or acting on behalf | `approval-required` | `approval-required` | Create proposal and require authorized human approval. Policy cannot downgrade any of the six. Mixed requests inherit this or a stricter disposition. |
| Classifier indeterminate, but inputs, project, actor, command, evidence, and policy context are otherwise authorized and supported | normalized fail-closed to `approval-required` | `approval-required` | Create a reviewable proposal explaining indeterminate classification; no execution before approval and revalidation. |
| Authorization, tenant/project/actor scope, sender authority, evidence safety, audit readiness, or allowlist fails | not invoked or ignored | `denied` | Refuse safely, audit when security-sensitive, and expose no restricted candidate/detail. Human approval cannot override denial. |
| Product/allowlist does not support the requested operation | not invoked | `unsupported` | Decline or allow separate task-intent capture; no mutation or external effect. |

All six boundary-crossing effects remain mandatory-approval while upstream text is being reconciled. Tenant Administration must not expose any control that weakens that invariant.

Immediately before execution, revalidate actor and reviewer authority, project/tenant scope, file access/redaction/freshness, recipient and sender/delegation authority, command/allowlist version, policy snapshot, effect set, proposal revision, idempotency identity, and audit readiness. Any change blocks execution and creates a refreshed proposal requiring a new decision; approval is never silently carried forward.

## State Patterns

### Canonical state families

| Family | States and treatment |
|---|---|
| Association | `Received` → `Proposed` → `Associated` / `NeedsReview` / `Deferred` / `Rejected` / `Failed` / `Skipped`; `Associated` may be superseded through `Correcting` → `Correction-delayed` or `Corrected`. `Rejected`, `Failed`, and `Skipped` stay terminal; authorized reprocess creates a linked new workflow instance. |
| Evidence | `fresh` is usable; `stale` is visibly warned, never auto-associates, and remains reviewable only where policy allows; `expired` makes confirm/approve unavailable with `evidence-expired` until refresh/re-evaluation. |
| Task intent | `detected` → `under-review` → `converted`; terminal dispositions are `not-actionable`, `duplicate`, `already-handled`, and `out-of-scope`, preserving the source link and rationale. |
| AI action | `proposal-ready`, `approval-required`, `approved`, `rejected`, `revision-requested`, `cancelled`, `execution-pending`, `succeeded`, `retryable-failure`, `terminal-failure`, `denied`, `unsupported`. Approval never changes `denied` into executable. |
| Two-person policy change | `draft` → `proposed` → `pending-second-admin` → `active`; alternatives are `rejected`, `expired`, `cancelled`, or `conflicted`. Activation requires a distinct approver and current-version revalidation. |
| Operation | `pending`, `accepted/projection-pending`, `succeeded`, `retryable`, `retry-exhausted`, `terminal`, `prior-outcome-returned`, `identity-conflict`, `operation-conflict`, `decision-conflict`, `revision-conflict`. |
| Inbound authenticity | `verified-as-supplied`, `anomaly-needs-review`, `external-sender`, `delegated`, `blocked`; provider verdict is labelled as provider-supplied, never as ChatBot re-verification. |
| Retention/export | `requested`, `authorized`, `in-progress`, `partial`, `completed`, `blocked`, `cancelled`; each state names data classes, owner, next action, and redaction/legal-hold limit. |
| AI availability | `available`, `degraded`, `unavailable`. During AI outage, manual association/correction, existing-proposal approval/rejection, mailbox retry, deterministic classification, operation status, and audit remain available when their non-AI dependencies are healthy; new generation is blocked with scope and recovery. |

### Correction propagation

After correction, show acknowledgements for candidate ranking, evidence snapshot, every AI proposal that consumed the old context, operational queue projections, and M2 vector/index material when applicable. `Correcting` blocks affected AI actions. M0/M1 target p95 is 10 minutes; M2 target p95 is 60 minutes. Crossing the applicable target produces `Correction-delayed`, names the responsible owner and next safe action, and triggers P2 escalation. Completion announces once and preserves current focus/selection.

### Replay, idempotency, and conflict outcomes

| Operation class | Repeated equivalent input | Changed/conflicting input |
|---|---|---|
| Message intake | Return prior outcome; no duplicate message/file/task/audit decision. | `identity-conflict`. |
| Durable command/mutation | Return prior outcome by stable `operation_id`. | `operation-conflict`. |
| Association decision | Return prior decision for the workflow `decision_slot_id`. | `decision-conflict`. |
| Approval decision | Return prior decision for the proposal `decision_slot_id`. | `decision-conflict`. |
| Outbound send | Return prior send outcome for frozen draft/recipients/authority/approval. | `operation-conflict`; never resend. |
| AI action proposal | Return prior proposal inside the bounded suppression window. | Create a new proposal with a new identity. |
| Correction | Return prior correction for predecessor revision. | `revision-conflict`. |
| Retry | Return prior attempt result for the same failed step/revision. | Reject stale revision. |

Replay/simulation additionally shows `replay_run_id`, test-tenant scope, intercepted effects, and production-store invariance. Production audit views exclude replay by default. Replay safety failure is terminal and never offers a production-effect retry.

### Per-surface coverage

| Surface | Required states |
|---|---|
| Project Workspace / Conversation Detail | Cold load; no project; empty/active conversation; message classification; task intent; generating/complete/stopped/failed AI output; attachment scan; AI outage; proposal; operation pending; correction; degraded; unauthorized/redacted. |
| Association Review | Candidate loading; no safe candidates; below-`T_low`/conflict/scorer failure `NeedsReview`; radiogroup selection; stale/expired evidence; validation; confirm/reject/defer/escalate; retryable; quarantined/terminal. |
| AI Action Review / Outbound Approval | Loading; approval-required; indeterminate-but-reviewable; missing context; fresh/stale/expired evidence; insufficient authority; revalidation drift; approved/rejected/revised/cancelled; execution pending/success/retryable/terminal; denied/unsupported. |
| Correction Surface | Eligible; rationale editing; stale revision; Correcting; Correction-delayed; completed; permission denied; invalidation dependency failed; retry/escalation. |
| Tenant Administration | Loading/empty; bounded scope; draft/edit/validation; proposed/pending second admin/rejected/expired/cancelled/conflicted/active; mailbox permission degraded; unauthorized; rollback-capable non-destructive change; terminal failure. |
| Cross-surface Attribution / Command Surface Reference | Loading; parity version current/stale; normalized operation selected; allowed/approval-required/denied/unsupported; operation pending/prior outcome/conflict; redacted; adapter parity failure; MCP/CLI recovery. |
| Operational Dashboards / Admin Queue Operations | Loading/empty; fresh/stale data; within-budget/approaching/exhausted/unsupported SLO; owner/alert/escalation; row selection; bounded detail; claim/assign; retry/requeue/quarantine/dismiss; conflict; degraded; terminal. |
| Compliance Investigation | Loading/no results; filters; selected event; projection pending; source/AI distinction; redacted detail; replay excluded/included; correction trace; retention/export/support-bundle status; terminal outcome; escalation. |

### Feedback and focus

| State | Feedback rule |
|---|---|
| Loading | Busy region replaces in place; clear `aria-busy`; no historical-content announcement. |
| User-triggered success | Inline status plus optional polite Status toast/banner; keep audit/operation link. |
| Evidence becomes expired | One polite announcement on the current review; preserve focus; approval/confirm becomes explained-unavailable; attempted submit focuses Error summary. |
| Generating AI output | Separate status region announces `Generating`, `Response complete`, `Response stopped`, or failure once each; never announce tokens/chunks/polls. |
| Correction progress | Deduplicated polite state changes; meaningful determinate progress only when a real value exists, otherwise textual indeterminate status. |
| Validation/conflict | Focus Error summary; preserve valid fields, selected candidate, draft, and filter state. |
| Retry/prior outcome | Focus Shared operation status; state whether a new attempt occurred and whether duplicate effects were prevented. |
| Denied/terminal | Persistent Blocked state; assertive only when caused by the current user's action; existence-neutral safe next step. |
| Observed/background change | Inline update or keyboard-reachable “new updates”; no live announcement for off-screen items and no forced scroll. |

## Interaction Primitives

### Core interaction rules

- Select authorized projects, conversations, candidates, files, approvals, queue items, and audit events; expand permitted evidence; confirm, reject, defer, correct, retry, quarantine, approve, revise, cancel, or escalate.
- State-mutating operations from UI, CLI, MCP, service clients, AI actors, workers, and mailbox events use one shared command spine. No surface offers an authorization, approval, audit, or idempotency bypass.
- `Association candidate group` uses radiogroup semantics: one Tab stop, arrow-key movement, announced position/count, option evidence via programmatic description, and no commit on selection. Refresh preserves group focus/selection unless the option becomes unsafe or expired; then clear it, announce once, and keep safe next actions available.
- Approval shows all `Approval authority and effects` fields before a decision and performs immediate pre-execution revalidation. Batch approval is available only for items sharing requester, command, project, authority, input shape, policy, effect set, and freshness; each item receives its own audit event.
- Approval queues prioritize by risk, affected-party authority, and age; group only the safely batchable shape above. A reviewer with more than 25 open items receives a load alert. Notification ceilings roll excess notices into a digest but never hide the persistent queue item or bypass approval.
- Queue operations use stable filters and pagination, never infinite scroll. Queue-level retry/requeue/quarantine/dismiss cannot mutate project records and always records admin identity, queue, affected items, and reason.

### UI / CLI / MCP outcome parity

Every parity-set operation uses the same normalized input, authorization decision, lifecycle transition, redaction/reason code, operation identity, immutable origin attribution, audit envelope, and long-running status. Presentation may differ.

| Parity-set operation | UI | CLI | MCP | Shared outcome contract |
|---|---|---|---|---|
| Intake-status inspection | Labelled status panel | Structured status record | Typed tool result | Same state, freshness, reason, safe action, and redaction. |
| Candidate review | Radiogroup/evidence panels | Ordered structured candidates | Ordered typed candidates | Same safe ordered candidates, evidence IDs, confidence/band, and suppressed unsafe set. |
| Confirm/reject/defer/correct | Decision controls | Named command | Typed operation | Same normalized decision, expected revision, state transition, conflict, and audit result. |
| Attachment storage/status | Attachment row | Structured file status | Typed file status | Same scan/storage/context eligibility, redaction, retry, and identity. |
| Task-intent capture/status | Task intent review | Structured capture/status | Typed capture/status | Same detector data, source evidence, disposition, and linked proposal identity. |
| AI approval decision | Approval controls | Named decision command | Typed decision tool | Same authority/freshness/revalidation gate, decision conflict, and audit record. |
| Approved-command execution | Proposal/operation status | Named execute command | Typed execute tool | Same allowlist/effect gate, operation ID, transition, partial/terminal result, and origin. |
| Retry | Queue/operation control | Named retry command | Typed retry tool | Same eligibility, attempt ID/ceiling, stale-revision result, and prior-outcome behavior. |
| Operation status | Shared operation status | Structured status record | Typed status result | Same canonical state/reason, partial output, correlation, next action, and terminality. |
| Audit lookup | Audit timeline | Structured events | Typed event results | Same authorized evidence, redaction, origin, replay distinction, and reconstruction links. |

An MCP timeout, revoked scope, malformed argument, or attempted validation bypass returns the same safe denial/retry semantics as UI/CLI, including operation identity when one was accepted. Recovery is re-authentication or a status query, not adapter-specific execution.

### Inbound authenticity and outbound authority

- Inbound review exposes provider-supplied DMARC/DKIM/SPF verdicts, relevant header disagreements, `external_sender`, delegated identity, and `principal_for` without claiming ChatBot re-verification.
- MVP authenticity modes are `strict` and `paranoid`; anomaly routes to `NeedsReview` or blocks. No permissive mode or broad fallback.
- Outbound authority is one of `draft-only`, `authenticated-user send`, `shared-mailbox send`, `send-on-behalf`, or `approved service-send`. Show the evidence and requester for the chosen class.
- Immediately before send, revalidate mailbox membership, delegation, tenant policy, recipients, frozen content, file exposure, proposal revision, and approval. Drift returns `policy-blocked`, `delegation-mismatch`, `membership-revoked`, or `approval-missing`; content is not sent.

### Streaming, keyboard, and focus

- Incremental AI content is regular document content with `aria-live=off`. A separate deduplicated status region announces only generating, complete, stopped, and failure transitions for the current user's request.
- Stop/Cancel stays keyboard-reachable in a stable location while generation is active, does not steal focus, announces “Response stopped” once, and returns focus to the composer or proposal.
- Single-character/modifier-free shortcuts are disabled inside text entry and can be globally remapped or disabled. Equivalent labelled controls are always available.
- Initial focus lands on the surface heading or first actionable review item. Dialogs/sheets contain and return focus. Escape never discards unsaved work without confirmation.
- Focused content remains at least partially unobscured by sticky headers, navigation, drawers, or panels at every breakpoint and after programmatic moves. Apply scroll margins for persistent chrome; at 200% and 400% zoom, scroll only enough to reveal focus and never force the reader away from conversation/audit history.
- New stream/audit content never forces scroll while the reader is in history; a keyboard-reachable “new updates” affordance moves on request.

## Accessibility Floor

- WCAG 2.2 AA applies per PRD increment to every shipped UI surface; each surface receives automated, keyboard-only, and screen-reader acceptance. CLI/MCP are outside WCAG scope but preserve equivalent safety semantics.
- Landmarks identify navigation, project context, main content, complementary evidence/review, filters, and status. Repeated roles have unique accessible names.
- Every control exposes role, label, state, keyboard operation, and reachable unavailable reason. Tooltip-only explanations are prohibited.
- Focus order follows visible order; Focus Not Obscured behavior follows §Interaction Primitives. Error recovery focuses Error summary without losing safe state.
- Classification, source/AI distinction, freshness, risk/disposition, correction, SLO, and redaction use text and structure, not color alone, and survive forced colors.
- Touch-primary actions meet 44×44 CSS pixels where layout permits; compact controls meet at least 24×24 CSS pixels or equivalent spacing. Destructive/approval controls are never compact-only on phone/tablet.
- Reduced motion suppresses shimmer, row movement, streaming animation, and non-essential transitions. Every progress state has non-motion text.
- The root/page `lang` follows the selected UI locale. Known-language message bodies, AI summaries, and quoted source passages use language-of-parts metadata when they differ. The selected locale persists across navigation and authenticated sessions. Stable codes/IDs remain untranslated and are not misleadingly tagged as prose.
- English and French share feature, state, action, disabled-reason, and screen-reader parity. Locale-aware dates/numbers/plurals apply; concatenated accessible strings are prohibited; French expansion cannot remove critical labels.
- Copy, export, transcript download, read-aloud, retention results, and support bundles use the same redaction as the visual surface. Accessible names/descriptions contain no hidden source text and announce when output is redacted.

## Responsive & Platform

| Form factor | Behavior |
|---|---|
| Desktop/laptop | Persistent navigation plus conversation/grid and complementary panel may coexist; full administration and investigation. |
| Tablet | Navigation collapses; primary workflow and complementary panel stack; association and approval remain complete. |
| Phone | Reading, governed AI request, status, confirm/reject/defer/approve, and escalation remain available. Dense configuration/investigation uses a state-preserving handoff link and reachable explanation. |

The UI is responsive Blazor/FrontComposer. CLI and MCP are command surfaces, not visual breakpoints. When dense grids reflow, labelled rows retain actor, authority, disposition/risk, state, freshness, time, next action, and safe reason; raw IDs and repeated context may collapse into metadata.

## Inspiration & Anti-patterns

The conversation-as-work-surface inspiration comes from developer AI tools and familiar chat entry, but this product rejects consumer-chat behaviors. No ungoverned freeform action execution, hidden auto-association, hover-only critical action, modal stack beyond one, infinite operational list, decorative assistant persona, or UI/CLI/MCP bypass affordance is permitted.

## Key Flows

### Journey 1: Business Contributor Requests AI Help From a Project Conversation

**Source ID and mapping:** UJ1 · FR21–FR28, FR33, FR35–FR46 · NFR1–NFR11, NFR16, NFR49–NFR55, NFR60–NFR64 · S1, S3.

1. Amira opens the authorized Project Workspace; source email and attachments show Message classification, Source evidence, and Evidence freshness.
2. She reviews Task intent and asks AI to compare a governed mailbox attachment with current authorized project files.
3. Source evidence stays expanded; any AI summary stays collapsed and carries provenance.
4. Because drafting can expose files or communicate externally, the system creates an `approval-required` proposal instead of acting.
5. Amira reviews authority, effects, recipients, files, policy, classifier metadata, expected post-state, and audit events.
6. **Climax:** with fresh evidence and current authority, Amira approves a fully bounded proposal and sees the governed operation identity.
7. The outcome returns to the conversation with source/proposal/approval/audit links.

Failure: missing context asks for clarification; expired evidence blocks approval with `evidence-expired`; AI outage leaves manual review and existing-proposal decisions available.

### Journey 2: Business Contributor Resolves an Ambiguous Project Association

**Source ID and mapping:** UJ2 · FR3–FR12, FR64–FR69, FR76–FR80 · NFR13–NFR18, NFR23–NFR30, NFR37–NFR48 · S2, S4.

1. Marc opens Association Review for a `NeedsReview` item.
2. One named candidate radiogroup exposes only authorized candidates; arrow keys move without committing.
3. He opens Why this project and compares source evidence, confidence/band, actor/time, and freshness.
4. The decision bar repeats his selected candidate and offers confirm, reject all, defer, or escalate.
5. **Climax:** Marc confirms only after fresh or policy-permitted stale evidence, and the audited association enters the project once.

Failure: below-`T_low`, no match, conflict, scorer failure, expired evidence, or only unsafe candidates remains `NeedsReview`; the safe candidate list may be empty and never reveals what was suppressed.

### Journey 3: External Party Sends Project Context Into Hexalith

**Source ID and mapping:** UJ3 · FR1–FR4, FR13–FR20, FR29–FR34 · NFR1–NFR12, NFR31–NFR36, NFR49–NFR55 · S1, S2.

1. Elena sends an email and attachment through a controlled mailbox pattern.
2. Intake preserves source identity and provider-supplied authenticity/header evidence; Elena is resolved as a tenant-scoped party when possible.
3. Authorization and safe project association occur before project/file exposure.
4. The attachment enters governed mailbox capture, scan, storage, and retention states; there is no general-upload route.
5. **Climax:** Elena continues using ordinary email while the authorized internal team receives governed, attributable project context.

Failure: unresolved identity, authenticity anomaly, unauthorized scope, or scanner/audit failure routes to existence-neutral review, block, or quarantine with no broad fallback.

### Journey 4: Project Owner Corrects a Wrong Association

**Source ID and mapping:** UJ4 · FR7–FR8, FR23–FR28, FR60–FR63, FR87–FR96 · NFR13–NFR22, NFR49–NFR59 · S4, S1, S9.

1. Priya opens the Correction Surface from a misassociated conversation item.
2. She reviews predecessor evidence, downstream attachments/proposals, authority, and freshness, then supplies rationale.
3. The correction commits once and enters `Correcting`; affected AI use becomes unavailable with a reason.
4. Correction progress reports acknowledgements from every affected derived store, estimate, owner, and audit linkage.
5. **Climax:** only after all acknowledgements does the item become `Corrected`, with contaminated context removed and history preserved.

Failure: a stale revision returns `revision-conflict`; propagation beyond 10 minutes in M0/M1 or 60 minutes in M2 becomes `Correction-delayed`, triggers P2, and keeps AI blocked.

### Journey 5: Tenant Admin Configures Governed Email Collaboration

**Source ID and mapping:** UJ5 · FR9, FR18–FR20, FR51–FR53, FR67–FR75 · NFR23–NFR48, NFR65–NFR70 · S5, S8, S10.

1. Nora enters Tenant Administration with her bounded scope displayed.
2. She proposes a security-sensitive threshold or policy change with values, scope, justification, and version; no control can downgrade the six approval effects.
3. A distinct authorized admin reviews it; self-approval and insufficient scope are explained-unavailable.
4. Nora reviews Operational Dashboards for health, queue depth/age, freshness, SLO/error-budget status, owner, and alert route.
5. She uses aggregate queue operations without seeing project detail she is not separately authorized to access.
6. **Climax:** the second-admin-approved version activates with an audit link while the product remains fail-closed and operable.

Failure: conflict, expiry, rejection, revoked mailbox permission, `unsupported` SLO evidence, or missing second approver leaves the prior policy active and shows owner/next action.

### Journey 6: Developer Uses CLI To Inspect and Resolve Project Email Workflow

**Source ID and mapping:** UJ6 · FR80–FR86, FR90–FR95 · NFR24–NFR36, NFR67–NFR70 · S7, S10.

1. Leo opens the Cross-surface Attribution / Command Surface Reference and notes the parity-set version.
2. Through CLI he lists unresolved items and receives the same safe ordered candidates/evidence as UI and MCP.
3. He confirms an association and receives operation identity, immutable CLI origin, transition, and audit result.
4. Repeating the equivalent command returns the prior outcome; a changed decision returns `decision-conflict`.
5. **Climax:** Leo verifies the same status/audit outcome through an authorized MCP status lookup without bypassing governance.

Failure: revoked MCP scope or malformed bypass argument yields the same existence-neutral denial; accepted long-running work remains recoverable through operation status rather than blind resubmission.

### Journey 7: Compliance or Support Reviewer Investigates a Risky Action

**Source ID and mapping:** UJ7 · FR54–FR63, FR85–FR86, FR90–FR91 · NFR49–NFR59 · S8, S9.

1. Sofia opens Compliance Investigation from a reported operation.
2. She searches authorized audit by source, correlation, actor, origin, policy, decision, and time.
3. The timeline distinguishes source evidence, AI summaries, policy/approval, correction acknowledgements, replay events, redaction, and outcome.
4. She creates a redacted support bundle or scoped retention/export request when authorized, previewing included/excluded data classes.
5. **Climax:** Sofia reconstructs who acted, under which authority/policy/evidence, what crossed a boundary, and what outcome occurred without relying on screenshots.

Failure: restricted detail stays existence-neutral with escalation; stale projection shows operation identity; replay is excluded by default; unsupported recovery/SLO evidence is labelled, never inferred.

### Journey 8: User Reviews an AI Action Before It Leaves the Project Boundary

**Source ID and mapping:** UJ8 · FR39–FR50 · NFR16, NFR46–NFR48, NFR60–NFR64 · S3, S6.

1. Amira asks AI to prepare an external response using governed files.
2. The system marks the internal classification and user-visible `approval-required` disposition because file exposure and external communication are boundary effects.
3. Outbound Approval shows frozen content, recipients, sender authority/delegation, requester/origin, files/redaction/freshness, command/version, policy, effects, reversibility, expected post-state, and audit events.
4. Amira approves, rejects, requests revision, or cancels.
5. Immediately before send, the system revalidates every authority, evidence, policy, effect, revision, idempotency, and audit precondition.
6. **Climax:** the exact approved content is sent once under current authority, and the conversation shows the operation and audit outcome.

Failure: evidence expiry, membership/delegation drift, policy/allowlist/effect change, or approval mismatch blocks send and requires a refreshed proposal; denied/unsupported work cannot be approved.

### System Journey: Governed AI Execution

**Source ID and mapping:** System Journey · FR33, FR39–FR46, FR81–FR89 · NFR1–NFR22, NFR31–NFR36, NFR49–NFR55 · S1, S3, S6, S7, S9.

1. Ari receives an authorized conversation or UI/CLI/MCP command-surface request; scheduled-time and file-addition triggers are not MVP origins.
2. Admission validates tenant, project, actor, context package, files, policy, allowlist, evidence, intended effects, idempotency, and audit readiness.
3. A supported read-only/no-external-effect subtype may become `allowed-read-only`; any of the six boundary effects becomes `approval-required` without downgrade.
4. An indeterminate classification creates a reviewable proposal only when all other inputs and authorities are safe; otherwise Ari returns `denied` or `unsupported` without unsafe detail.
5. Approved work executes once through the shared command spine; status and audit preserve immutable origin across UI, CLI, and MCP.
6. **Climax:** Ari advances authorized work while every boundary crossing remains human-approved, attributable, idempotent, and reconstructable.

Failure: unresolved association/actor, unauthorized input, expired evidence, unavailable audit, or disallowed command fails closed. During AI outage, deterministic/manual workflows and existing-proposal decisions continue while new generation remains visibly unavailable.
