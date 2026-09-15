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
implementationReferences:
  - ../../architecture/architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md
reconciliationSources:
  - reconcile-product-contracts-2026-09-14.md
  - reconcile-architecture-2026-09-14.md
  - reconcile-existing-ux-inputs-2026-09-14.md
decisionLog: .memlog.md
---

# Hexalith.ChatBot — Experience Spine

## Foundation

Hexalith.ChatBot is a responsive enterprise conversation workspace in which authorized people understand Project context, review ambiguous intake, govern AI assistance, perform bounded administration, and reconstruct outcomes. Microsoft Blazor Fluent UI v5 → Hexalith.FrontComposer → `DESIGN.md` is the visual inheritance chain; this document owns information architecture, behavior, states, responsive continuity, and accessibility.

Desktop/laptop is primary, but every in-scope task remains complete at 320 CSS pixels and 400% zoom. A larger-screen handoff is optional. Source records, AI interpretation, canonical outcomes, laggable projections, authority, effects, freshness, qualification, and recovery remain distinguishable in words—not only by color, placement, or icons.

The PRD, addendum, and product brief define product intent. The surface/conformance supplements elaborate it. The architecture spine is a downstream implementation reference: it may clarify safe status behavior, but does not override product intent. The reconciliation files identify applied decisions and unresolved contracts. If a future mockup, wireframe, prototype, or imported reference conflicts with this spine pair, `DESIGN.md` and `EXPERIENCE.md` take precedence until the spine is intentionally revised.

### Contract map

[Information Architecture](#information-architecture) · [Component Patterns](#component-patterns) · [Governed Action Boundary](#governed-action-boundary) · [State Patterns](#state-patterns) · [Interaction Primitives](#interaction-primitives) · [Accessibility Floor](#accessibility-floor) · [Key Flows](#key-flows)

### Terminology

`Safe` means the presentation reveals only data authorized for the current actor and offers only a permitted next action. `Qualified` means the named capability or increment has accepted gate evidence. `Current` authority, evidence, or revision is unexpired and not superseded when the decision runs; a `present human` is an authenticated human with a recorded user-presence signal for that decision. Prefix `owner` with its role: Project owner, effect owner, source owner, or rebuild owner. An `authority tuple` is the source-defined set of actor identity/type, role, scope, delegation, and freshness fields. A `decision_slot_id` identifies the first-commit-wins decision. A `typed status` contains a contract state plus a stable reason. A `successor` is a new immutable workflow or attempt linked to its preserved predecessor.

### Scope and increment availability

| Increment | Available experience | Entry condition shown to users and operators |
|---|---|---|
| M0 | S1, S2, S2a, S3, and restricted O1. Mailbox/attachment/participant workflows and minimum bootstrap automation support them but do not imply an M1/M2 editor. | May be enabled only when A5, A6, and A13 gates pass. A blocked capability shows the failed gate, owner, evidence age, permitted fallback, and recheck action. |
| M1 | S1a and S4–S7, including FR28a–FR28f attempt controls. | M0 gates are revalidated; A8, A9, A11-M1, and A12 apply where required. Finalized architecture alignment and fresh product approval still prevent an unconditional readiness claim. |
| M2 | S8–S10. | A10 and A11-M2 evidence pass and changed lower-increment gates are revalidated. |

The six AI-mediated effect classes are permanently `approval-required`: Project-state mutation, file exposure, external communication, task creation or assignment, external-tool invocation, and acting on behalf of a participant. Tenant policy cannot downgrade them. A direct authorized human command follows its applicable command/audit contract; it does not become an AI proposal merely because it writes state.

### Upstream blockers and open contracts

| Open contract | Why UX cannot resolve it | In-review posture |
|---|---|---|
| Product source approval | The current PRD/addendum revisions are draft and the latest product validation remains STOP, so they are not fresh approval evidence. | Apply their explicit fail-closed interaction contracts, keep both UX spines `in-review`, and make no release-readiness claim. |
| Architecture alignment | The finalized architecture still reflects earlier authenticity, correction, milestone, and classifier contracts. | Use the conservative source-supported UX posture and block downstream implementation of disputed paths until product and architecture are reconciled and revalidated. |
| A6 data-protection gate | O1 now has an interaction contract, but pilot use still requires accepted purpose, retention, hold, erasure, backup, key-custody, and surviving-metadata evidence. | Show the restricted O1 request/status/result experience as qualification-blocked until A6 passes; do not claim GDPR satisfaction. |
| A12 identity evolution | `IdentityEvolved` remains a proposed external dependency until each producer accepts its schema and reconciliation contract. | Preserve original identifiers; unresolved current identity blocks mutation and routes to authorized reconciliation without rewriting history. |

This pair is the maintained UX spine, not a story-level component specification.

## Information Architecture

| Surface | Increment | Primary purpose |
|---|---:|---|
| S1 — Project conversation view | M0 | Land at `/`, select from authorized Projects/recents, and read conversation, source evidence, attachments, task intent, approvals, and governed outcomes. |
| S1a — Governed chat composer | M1 | Submit Project-scoped assistance and control immutable generation attempts. |
| S2 — Ambiguous association review | M0 | Decide email-to-Project association using safe, current evidence. |
| S2a — Inbound authenticity review | M0 | Let a current `mailbox-admin` initiate review of fresh mailbox-scoped provider/header/delegation evidence and a distinct current `policy-admin` approve the frozen decision without exposing Project candidates. |
| S3 — AI action approval | M0/M1 | Review authority, effects, proposal revision, expiry, and human decision. |
| S4 — Correction surface | M1 | Supersede a wrong association and track owner acknowledgments, irreversible dispositions, and blocked context. |
| S5 — Tenant admin configuration | M1; M0 bootstrap is automation-only | Manage bounded mailbox, service-client, role, policy, notification, limit, and safety controls. |
| S6 — Outbound approval | M1 | Review frozen content, files, recipients, sender authority, expiry, and send outcome. |
| S7 — Cross-surface attribution view | M1 | Compare operation membership and normalized outcomes across UI, CLI, and MCP. |
| S8 — Operational dashboards | M2 | Observe qualified health, queue, freshness, SLO, budget, owner, and escalation evidence. |
| S9 — Compliance investigation | M2 | Reconstruct authorized canonical evidence, laggable projections, replay, correction, and data-right status. |
| S10 — Admin queue operations | M2 | Control opaque partitions and operate Project-authorized items without widening authority. |
| O1 — Data-rights operations API/provisioning interface | M0+ before persisted pilot data | Let a restricted `compliance-admin` and independent current TenantOwner initiate and track export, erasure, hold, and retention work without direct owner-context mutation. |

O1 is a governed operator interface, not a general application surface and not a UI/CLI/MCP parity member. It remains visibly unavailable while A6 evidence is open.

### Surface composition

A page-like surface with sibling titled sections uses one `{components.surface-section-group}` with the primary `FluentAccordionItem` expanded. A single primary grid, form, detail, or workflow stays outside the accordion.

| Surface | Accordion items | Outside the accordion |
|---|---|---|
| S1 — Project conversation view | Source evidence (expanded), AI summary, Why this project | `/` landing, authorized Project picker/recents, header, stream, and persistent operation/connectivity state; no-project-selected and empty-conversation states remain distinct |
| S1a — Governed chat composer | Request context (expanded), Source evidence, Attempt history | Composer, current attempt, Stop/Cancel, and advisory progress |
| S2 — Ambiguous association review | Source evidence (expanded), Source metadata | Candidate radiogroup and decision bar remain visible together as the sole carve-out |
| S2a — Inbound authenticity review | Provider/header/delegation evidence (expanded), Retention and immutable history | Mailbox scope, fresh evidence digest, expected revision, operation identity, distinct `mailbox-admin` initiator/`policy-admin` approver, safe reason, Accept/Reject decision, and successor-reprocess guidance; no Project candidate data |
| S3 — AI action approval | Authority/effects (expanded), Source evidence, Execution/audit | Proposal summary, expiry, and human approval controls |
| S4 — Correction surface | Impact/owner status (expanded), Evidence, Immutable history | Rationale/action form and correction progress |
| S5 — Tenant admin configuration | Mailbox/policy (expanded), Service clients/admin grants, Runtime controls, Notifications | Header, qualification, filters, and primary editor/grid |
| S6 — Outbound approval | Frozen content/recipients (expanded), Sender authority, Source evidence, Send/reconciliation status | Proposal summary and human approval controls |
| S7 — Cross-surface attribution view | None when the operation grid is the sole primary region | Parity/membership grid and selected normalized outcome |
| S8 — Operational dashboards | Qualification and metric details (expanded), Freshness, Alerts | Dashboard grid, filter bar, and controlled-update bar |
| S9 — Compliance investigation | Canonical evidence (expanded), Decisions, Derived investigation status | Search/filter form and result grid |
| S10 — Admin queue operations | Selected state/actions (expanded), Diagnostics, History | Queue grid, filter bar, and controlled-update bar |
| O1 — Data-rights operations API/provisioning interface | Request authority/scope (expanded), Owner acknowledgments, Hold/redaction evidence, Immutable history | Qualification block or request/status/result controls, independent approval, retry/appeal guidance, and recipient-bound result expiry |

## Voice and Tone

Use calm, specific, operational language. Lead with what happened, what remains safe, and what the person can do next. Canonical codes are secondary evidence, not the whole explanation.

- “Review required: the highest score is below the automatic-association threshold.”
- “Approval unavailable: the evidence snapshot expired. Refresh evidence to create a new proposal.”
- “The command committed. The activity view is still catching up.”
- “We could not determine the send outcome. Do not send again while reconciliation is in progress.”
- “This request cannot be shown with your current Project access.”

Do not say “complete” while an effect, owner acknowledgment, projection, qualification gate, or evidence check is unresolved. Never expose hidden candidates or resource existence through error wording.

## Component Patterns

This behavioral catalog has the same names and order as the paired [visual component catalog](DESIGN.md#components).

| Component | Experience contract |
|---|---|
| Project context header | `{components.project-context-header}` keeps authorized Project, tenant where relevant, surface, increment, and qualification visible across reflow. |
| Conversation shell | `{components.conversation-shell}` preserves context, stream, composer, and complementary evidence while distinguishing source, AI, and system items. |
| Conversation stream | `{components.conversation-stream}` shows attributed immutable attempts and committed outcomes; partial streamed text never appears as committed. |
| Composer action entry | `{components.composer-action-entry}` separates governed chat from effectful AI requests, keeps Stop/Cancel stable and keyboard operable, and suppresses single-character or modifier-free application shortcuts while focus is in the composer without interfering with text entry or assistive-technology commands. |
| Actor badge | `{components.actor-badge}` names human, AI, service-client, mailbox, worker, or system origin without color alone. |
| Message classification | `{components.message-classification}` shows a user-facing classification with provenance and never disguises a proposal/system event as a participant message. |
| Surface section group | `{components.surface-section-group}` owns the sibling section items; expansion never hides the primary task or warning. |
| Source evidence | `{components.source-evidence}` identifies authoritative source, redaction, reference freshness, and decision evidence. |
| AI summary | `{components.ai-summary}` starts collapsed, is labeled AI interpretation, and never outranks source evidence. |
| Why this project | `{components.why-this-project}` explains evidence, score/band, policy, decision provenance, and superseding links. |
| Evidence freshness | `{components.evidence-freshness}` shows per-reference snapshot time and `fresh`, `stale`, or `expired`. |
| Task intent review | `{components.task-intent-review}` shows a source-linked detector result or detector-unavailable manual-review path. |
| Attachment row | `{components.attachment-row}` shows scan/storage/quarantine, folder, retention, retry, and AI-context eligibility; unsafe content never opens. |
| Association candidate group | `{components.association-candidate-group}` is one radiogroup; selection does not commit, and unsafe candidates/cardinality are omitted. |
| Association decision bar | `{components.association-decision-bar}` keeps selection and safe actions visible; from `Deferred`, Resume is the only settled action. |
| Action classification | `{components.action-classification}` separates valid classifier output from denial, unsupported work, and classifier unavailability. |
| AI proposal panel | `{components.ai-proposal-panel}` links source request, Project, proposal revision, classification, and one human decision unit. |
| Approval authority and effects | `{components.approval-authority-and-effects}` shows requester/origin, human authority, command, resources, recipients, six effects, expected post-state, freshness, and versions. |
| Approval controls | `{components.approval-controls}` keeps safe high-consequence actions in a stable location. An unavailable action uses `aria-disabled="true"` with a visible, programmatically associated reason, or an adjacent focusable reason when the component cannot expose the disabled control; a forbidden action remains absent when its existence would leak capability. |
| Correction progress | `{components.correction-progress}` distinguishes pre-commit blocking, committed correction, owner acknowledgments, delay, and irreversible effects. |
| Qualification status | `{components.qualification-status}` names capability/increment, gate, evidence age, owner, fallback, and recheck without treating intent as release proof. |
| Bounded admin scope | `{components.bounded-admin-scope}` separates aggregate visibility/partition control from Project-authorized item access. |
| Service-client grant | `{components.service-client-grant}` shows class, exact scope, expiry, initiator, distinct approver, owner evidence, version, and successor status. |
| Two-person approval | `{components.two-person-approval}` requires distinct eligible people and shows changed values, scope, reason, version, expiry, and conflict. |
| Runtime control status | `{components.runtime-control-status}` shows subject, mode, scope, version, freshness, capability, owner, and fail-closed result. |
| Shared operation status | `{components.shared-operation-status}` separates coordinator, owner effect, delivery, projection, attempt, retry, prior outcome, and correlation. |
| Operation diagnostic | `{components.operation-diagnostic}` discloses only authorized runbook metadata: correlation ID, tenant ID, mailbox ID, workflow-item ID, current state, retry count, failure-reason code, next safe action, and last-transition timestamp, actor, and from-state, plus safe version and predecessor/successor links. |
| Queue row | `{components.queue-row}` shows state, age, owner, scope, risk/confidence, freshness, attempts, next action, and terminality without hidden detail. |
| Operational SLO dashboard | `{components.operational-slo-dashboard}` says “SLO qualification backlog” until target, source, window, budget, alert, owner, and A11 evidence qualify. It also shows approval volume, median/p95 queue age, reviewer load, rejection/revision, and rubber-stamp indicators as informational signals that can tune routing/staffing but never bypass approval. |
| Audit timeline | `{components.audit-timeline}` separates canonical records, rebuildable projection, replay, annotation, correction, and outcome. |
| Inbound authenticity and sender authority | `{components.inbound-authenticity-and-sender-authority}` separates provider evidence from identity, delegation, and outbound sender authority. |
| Retention and export request | `{components.retention-and-export-request}` shows data classes, authorization, owner, hold/redaction limit, recipient expiry, and canonical state. |
| Redacted support bundle | `{components.redacted-support-bundle}` previews safe metadata/exclusions; external exposure requires explicit human confirmation. |
| Blocked state | `{components.blocked-state}` gives an existence-neutral reason, affected scope, safe owner, and one next action. |
| Connectivity status | `{components.connectivity-status}` distinguishes before-submit disconnect, pending-unknown, reconciling, and restored without guessing outcomes. |
| Controlled update bar | `{components.controlled-update-bar}` holds list updates during review and applies them without moving the active row. |
| Status toast banner | `{components.status-toast-banner}` announces a transition once; persistent status stays inline. |
| Busy region | `{components.busy-region}` replaces in place, preserves layout/focus context, and honors reduced motion. |
| Error summary | `{components.error-summary}` receives focus after invalid submit, links fields, and preserves valid input. |
| Review dialog | `{components.review-dialog}` contains one modal decision, returns focus, and never discards edits silently. |
| Queue filter bar | `{components.queue-filter-bar}` provides labeled server filters, explicit server-side sort with deterministic tie-break, active summary, omission-safe result count/order, ≤100 pagination, narrow-screen reflow, and focus/selection preservation. |

## Governed Action Boundary

### Classifier and detector outcomes

| Evaluation | Stored proposal/state? | User-visible behavior |
|---|---:|---|
| Valid, qualified classifier returns eligible `low-risk`; request is authorized and has no boundary-crossing effect | No approval proposal | Execute through the governed low-risk path; show provenance, immutable attempt, and result. |
| Valid, qualified classifier returns `approval-required` with a fully declared effect and authority tuple | Yes | Explain classification and require an authorized, present human decision. |
| Classifier artifact is missing, invalid, unqualified, fails, or returns an unknown/undeclared effect or authority tag | No | Return the candidate product outcome `classifier-indeterminate`; create no proposal or durable idempotency state, perform no effect, and expose no approval action. `classifier-unavailable` may appear only as a non-canonical availability reason. Show safe remediation/escalation and the separate auditable attempt; a remediated request is new and linked. |
| Authorization, Project/tenant/actor scope, evidence safety, audit readiness, or supported-operation admission fails | No | Deny safely before classification where applicable; approval cannot override it. |
| Operation is outside the supported catalog | No | Show `unsupported`; perform no mutation or external effect. |

Task intent keeps detector availability separate from risk classification. A valid detector artifact may produce `Detected` or `NeedsReview`. A missing, invalid, unqualified, or failed artifact produces `detector-unavailable`, creates no detector-derived domain state, and offers authorized manual review or retry with a separate auditable attempt.

Immediately before execution, revalidate requester and reviewer authority, recorded human presence, Project/tenant scope, files/redaction, recipients/sender/delegation, evidence freshness, operation and AI allowlists, classifier/policy versions, effect set, proposal revision/digests, expected owner revision, approval lifetime, runtime controls, identity, and audit readiness. A material change blocks execution and requires a new linked proposal and human decision.

### Batch approval

Batch approval is never inferred from visual grouping. It is available only when every item has the same requester, command, Project, authority tuple, normalized input shape, policy snapshot, effect set, freshness class, recipients, files, content digest, sender authority, tool target, approval revision, and expected resource revision. It is prohibited for irreversible actions, external sends, file exposure, external-tool invocation, and acting on behalf. Every item remains individually visible and authorized, retains its own decision slot and revision check, and commits its own audit record atomically with its decision. Any changed frozen value breaks the batch before execution.

## State Patterns

### Canonical source states and UX presentation

These exact codes come from the current PRD workflow tables. Friendly labels may explain them, but the UX does not invent substitute durable enums. Availability, qualification, connectivity, safe reason codes such as `classifier-unavailable`, and local view states display separately.

| Family | Exact canonical states / transitions the UX may claim |
|---|---|
| Inbound authenticity | `AuthenticityAccepted`, `AuthenticityReviewRequired`, `AuthenticityBlocked`, `AuthenticityRejected`. A current `mailbox-admin` initiates and a distinct current `policy-admin` approves the frozen fresh-evidence digest, expected revision, and stable operation identity before accepted or terminal rejected; paranoid anomalies block terminally. Blocked/rejected intake exposes no Project candidates. Reprocessing requires the same separation of duty plus changed policy/provider evidence and creates a new audit-linked intake instance. |
| Association | `Received`, `Associated`, `NeedsReview`, `Deferred`, `Rejected`, `Failed`, `Skipped`, `Correcting`, `CorrectionDelayed`, `Corrected`. `Proposed` and `Correction-delayed` are not canonical spellings. Only Resume moves `Deferred` → `NeedsReview`; direct confirm/reject is not permitted. |
| Participant resolution | `Resolved`, `Unresolved`, `Rejected`, `Quarantined`; resuming `Quarantined` creates a linked `Unresolved` successor. |
| Attachment handling | `PendingScan`, `Stored`, `Unsafe`, `Failed`; retryable `Failed` creates a linked `PendingScan` attempt. |
| Task intent | `Detected`, `NeedsReview`, `Converted`, `NotActionable`, `Duplicate`, `AlreadyHandled`, `OutOfScope`. |
| AI action proposal/decision/execution | `AwaitingApproval`, `Approved`, `Expired`, `Rejected`, `RevisionRequested`, `Cancelled`, `Executing`, `Succeeded`, `Failed`. Revised/retried work creates a linked successor where permitted. |
| Low-risk assistance | `Executing`, `Succeeded`, `Failed`. |
| Governed chat attempt | `Admitted`, `ApprovalRequired`, `Denied`, `Unsupported`, `Failed`, `Cancelled`, `Streaming`, `Completed`, `Stopped`. Terminal attempts are immutable; retry creates one linked attempt. |
| Outbound email | `Drafted`, `Sending`, `Sent`, `Failed`, `SendOutcomeUnknown`, `Reconciling`, `NotSent`, `Unresolved`. Unknown/reconciling/unresolved forbids resend. |
| Command execution | `Received`, `Admitted`, `Committed`, `Rejected`, `Failed`; a committed outcome separately projects as `ProjectionPending`, `Projected`, or `ProjectionLagging`. |
| Audit projection | `ProjectionPending`, `Projected`, `Lagging`, `Failed`; projection repair never changes the canonical effect. |
| Service-client permission | `Active`, `Revoked`, `Expired`; new access is an immutable successor grant. |
| Mailbox configuration | `Active`, `Paused`, `Disabled`; re-enable after `Disabled` is a new admitted configuration. |
| Emergency safety control | `Active`, `Disabled`, `Quarantined`, `RateLimited`; subject is mailbox source, service client, AI actor, or command capability. |
| ChatBot admin-role grant | `Active`, `Revoked`; role change creates a new `Active` successor. |
| Queue partition / item | Partition: `Active`, `Paused`. Item: `Claimed`, `Assigned`; terminal disposition uses the owner workflow command. |
| Data export | `Requested`, `Completed`, `PartiallyCompleted`, `Rejected`, `Failed`. |
| Data erasure | `Requested`, `BlockedByHold`, `Completed`, `PartiallyCompleted`, `Rejected`, `Failed`. |
| Legal hold | `Active`, `Released`. |
| Retention disposition | `DispositionPending`, `Disposed`, `BlockedByHold`, `Failed`. |
| Notification delivery | `Queued`, `Sent`, `Suppressed`, `Failed`; delivery never changes the originating workflow decision. |

### Association disposition

Only an authorized, conflict-free candidate with required deterministic evidence and `score >= T_high` is auto-eligible. Every other disposition enters `NeedsReview`: no candidate, any `score < T_high`, deterministic conflict, scorer error or non-finite output, stale evidence, or unauthorized evidence. A scorer error or non-finite output records the failure and exposes an empty candidate list; unauthorized candidates, evidence, order, and cardinality are omitted. `T_low` may change presentation only and never permits automatic association.

### Coordinator, effect, delivery, and projection

Long-running status is not one synthetic state. Always show coordinator and effect-owner lanes. Add a delivery/provider lane for provider-facing work and a projection/investigation lane for projected work:

1. Coordinator/admission: received, admitted, rejected, or failed before ownership.
2. Effect owner: authoritative committed revision or uncommitted/unknown outcome.
3. Delivery/provider: sender, recipient, or provider outcome, including outbound reconciliation.
4. Projection/investigation: freshness, watermark, lag, failure, rebuild owner, and next action.

“Accepted,” advisory progress, queue position, a projection row, or an audit search result never proves the owner effect committed. Projection lag does not undo a canonical commit. Status links canonical evidence and labels derived evidence explicitly.

### Correction pre-commit and completion

Before correction commits, validate source/destination authority, predecessor revision, complete impact-manifest shape, owner commands, invalidation delivery, runtime dependencies, and canonical audit durability. If the invalidation queue or atomic audit boundary is unavailable, no correction state commits; the predecessor remains authoritative and the UX shows a pre-commit block. Only an atomic start may show `Correcting`.

After commit, show each owner acknowledgment, repair/rebuild, compensation, or explicit irreversible-effect disposition. Affected AI context remains blocked. The UX may show `Corrected` only after every item in the frozen complete manifest—including Conversations/Folders-owned records and irreversible effects—has an immutable acknowledgment or explicit disposition.

### Governed chat attempts

Streaming text is advisory and uncommitted. Metadata such as phase, elapsed time, attempt ID, stop availability, and safe reason may update; the UI re-queries typed attempt status and never treats tokens or percentages as effect evidence. One uniquely named polite status region is scoped to the current attempt and announces deduplicated `Generating`, `Completed`, `Response stopped`, or failure transitions; streamed chunks remain outside live announcement. Stop/Cancel stays in a stable Tab position and returns focus to the composer or the linked proposal/status. A winning stop discards uncommitted output. `Completed` presents one committed response; `Stopped` and `Failed` preserve no partial committed message. Retry creates a new immutable linked attempt and never resumes or overwrites its predecessor.

### Cross-surface connectivity

Connectivity descriptions are UX-only, not canonical durable states:

- Disconnected before submit: preserve draft and do not claim admission.
- Disconnected while pending: prevent duplicate submit; show “Outcome not yet known” while querying the typed status (contract state plus stable reason).
- Response lost after admission: reconcile by stable attempt/operation identity before retry.
- Restored: announce once, preserve focus/selection, and show the authoritative status found.

### Runtime control and qualification

If an applicable safety control, policy snapshot, service-client grant, sender authority, audit boundary, or gate evidence is missing, invalid, stale, unqualified, or unreadable, the affected action blocks closed. Show only safe scope, reason, owner, evidence age, and recheck/escalation; never fall back to weaker policy or broader permission.

Before A11 evidence is accepted, dashboards say “SLO qualification backlog,” “target awaiting evidence,” or “not qualified,” not “target achieved.” Targets and observations show source, window, tenant scope, freshness, sample, budget, and owner.

### Controlled updates

Decision-list refresh never silently reorders the reviewed row, discards selection, or changes facts under a pending decision. Updates wait behind `{components.controlled-update-bar}`; pending-count changes are not live-announced. After Apply, announce one concise added/removed/changed/result-count summary. Preserve identity and focus when the active row still exists and remains authorized; otherwise, clear its action and explain the removal. Chronological conversation or audit streams never force scroll and expose a keyboard-reachable “new updates” action. Scoped operation/connectivity status updates in place without moving focus. Security revocation or a terminal safety block may interrupt immediately but explains the change.

## Interaction Primitives

### Core rules

- Inspect only authorized Projects, conversations, evidence, candidates, files, proposals, queue items, and audit records. Unsafe candidates, identifiers, order, and cardinality are omitted consistently.
- Every mutation from UI, CLI, MCP, service client, AI actor, worker, or mailbox event uses the shared command spine. No adapter bypasses authorization, audit, idempotency, approval, or runtime controls.
- Candidate review is one radiogroup with one Tab stop. Arrow keys move selection, accessible descriptions carry evidence, and selection never commits.
- Refresh/conflict preserves valid input. Invalid submit or attempted activation of an unavailable action focuses `{components.error-summary}`; prior-outcome/retry results focus `{components.shared-operation-status}`. Valid fields, selections, and drafts remain intact.
- Aggregate admin actions operate only on opaque partitions. Per-item claim, assign, retry, reprocess, quarantine, dismiss, or domain decision requires current Project authority and the owner command.

### Operation membership is separate from outcome parity

An operation belongs to a surface only when every membership gate passes. Outcome parity is evaluated only after membership; it cannot make an unexposed or unauthorized operation invocable.

| Membership gate | Required answer |
|---|---|
| Product operation catalog | This exact normalized operation is supported in this increment. |
| Surface exposure | It is explicitly exposed on UI, CLI, and/or MCP for this actor class. |
| MCP tool tag | The typed tool declares the exact operation and effect profile. |
| AI allowlist | AI-originated proposal eligibility and all applicable six-effect classes are explicit. |
| Owner executable/qualification | The target owner contract is accepted, qualified, current, and callable. |

For valid members, UI/CLI/MCP preserve normalized input, authorization/redaction, state transition, stable identity, origin attribution, audit envelope, retry/conflict, and long-running outcome. Presentation may differ. Human-delegated MCP approval is candidate M1 behavior only when current user presence and `actorType=human` are recorded; it remains implementation/qualification-blocked until product approval and architecture alignment. AI, tool, and service principals are structurally denied and cannot approve or co-sign.

### Omission parity

Authorization failure yields the same existence-neutral omission across candidates, counts, filters, typeahead, detail links, exports, notifications, diagnostics, CLI, and MCP. Do not return placeholders, rank gaps, total counts, timing distinctions, or errors that reveal a hidden item. Authorized-empty, filtered-empty, and denied results differ only where that distinction is safe.

The authorization/redaction transform runs before content enters the rendered Document Object Model (DOM) or accessibility tree. It covers accessible names and descriptions, live/status regions, hidden or collapsed content, table/grid metadata, clipboard, transcript/download, export, and read-aloud output. Announce that content is redacted only when that fact is safe; never retain the removed value, candidate count/order, or identifying description in accessibility metadata.

When historical identity has no accepted current successor, show `unresolved-current-identity`, preserve the original identifier, reject mutation, and route to authorized reconciliation. History is never rewritten; immutable migration links appear only after the producing context accepts the versioned evolution contract.

### Retry and replay

Equivalent replay returns the recorded outcome. Changed input under the same identity returns a typed conflict and performs no new effect. Retry is offered only for the exact retryable pre-effect/failed state and current revision, creating the defined linked successor.

### Outbound reconciliation

Outbound `SendOutcomeUnknown`, `Reconciling`, and `Unresolved` never offer resend. Reconciliation shows its evidence status and four-hour default deadline; exhaustion enters terminal `Unresolved` with owner and P2 escalation. Only an audited `NotSent` permits a new draft and fresh human approval, never a retry of the uncertain send. Pre-send revalidation checks frozen content, recipients, authority tuple, approval lifetime, and revision.

### Service-client administration

M0 bootstrap exposes only the four enumerated M0 service-client classes and minimum grants. The first grant/policy snapshot uses two distinct current TenantOwner principals plus required Security approval. M1 editors show exact client/role scope, expiry, predecessor, initiator, distinct approver, owner evidence, schema/policy version, and immutable audit link. Revoked/expired grants never reactivate.

### Audit and diagnostics

Security-sensitive denials, restricted reads, and service-client failures use the separate auditable-attempt path. They never appear as a canonical mutation envelope or repair a missing one. Diagnostics are metadata-only and authorization-filtered; support bundles require redaction and explicit human confirmation before external exposure.

Across S5, S8, and S10, every successful admin dashboard read, committed mutation, and rejected attempt records admin identity, scope used, affected opaque items, outcome, and server time. Successful mutations use canonical audit; successful reads, sensitive denials, and restricted reads use the separate attempt path. Service-client and AI principals cannot initiate or approve role or policy mutation.

## Accessibility Floor

- Meet WCAG 2.2 AA: 4.5:1 normal text and 3:1 non-text controls/focus. Status never depends on color alone.
- Give every control/group a programmatic name; use semantic grids, status, errors, dialogs, accordions, and progress.
- Keep keyboard order aligned with visible order. Contain focus only in the active `{components.review-dialog}`, then return it. Provide skip links to main content, filters, results, and active review. Keep every keyboard or programmatic focus target at least partially visible above persistent chrome and outside overlays; use scroll margins and reveal only enough context after errors, refresh, Project switch, proposal surfacing, dialog close, or safety interruption.
- Move focus to the documented new-context heading and announce once after a Project switch updates authorized context. Link each surfaced proposal programmatically to its originating request and expose a predictable “Review proposal” focus target.
- Announce meaningful transitions once through a uniquely named, scoped status region. Do not announce streaming chunks, polling ticks, queued-count ticks, background rows, or historical content. Assertive announcements are reserved for a block caused by the current action.
- Remove skeleton shimmer, streaming cursor/typing animation, animated row movement/reordering, and nonessential dialog/toast transitions under reduced motion. Keep state text and real determinate values; textual indeterminate status must remain understandable without animation.
- Reflow actions into labeled full-width rows/steps at 320 CSS pixels and 400% zoom; wrap facts and preserve control order. Convert wide data to labeled records; allow bounded scrolling only for intrinsically two-dimensional content. Support WCAG text-spacing overrides without loss, clipping, overlap, obscured focus, or unavailable actions. Rows may grow or become labeled details; truncation never hides authority, state, reason, freshness, qualification, or the safe next action.
- Make every pointer target at least 24×24 CSS pixels or satisfy the applicable spacing, inline, or essential exception. Make touch-primary actions 44×44 CSS pixels; an essential inline control may instead use the AA minimum or spacing exception. Apply this rule to interactive badges, grid and pagination actions, disclosures, Stop/Cancel, approval controls, and narrow-screen overflow actions. Never make hover the only access path.
- Label source evidence, AI summary, canonical record, investigation projection, partial output, qualification, and restricted content explicitly.

### Localization and language

English and French have feature, state, action, unavailable-reason, and screen-reader parity. Root/page `lang` follows the selected locale, which persists across navigation and authenticated sessions. Known-language messages, quotations, attachment extracts, and AI summaries carry language-of-parts metadata. Dates, numbers, durations, and plurals are locale-aware; stable identifiers and canonical codes remain untranslated and are not tagged as prose. Accessible strings are complete localized messages, never concatenated fragments. French expansion is accepted at 320 CSS pixels, 400% zoom, and the text-spacing settings above.

### Per-surface acceptance

Every delivered surface has a real live route exercised through the running application. It independently passes server-verified primary-path success, direct assertions of functional requirements, automated checks, keyboard-only review, and screen-reader review for every state that its contract exposes: cold-load/loading, empty, validation, unauthorized/redacted, degraded, offline or pending-unknown, retryable, and terminal. Static fixtures, source scans, snapshots, and handler-only tests do not substitute. Each row also proves focus order and visibility, scoped announcements, 320 CSS pixel/400% reflow, text spacing, target size, forced colors, reduced motion, and English/French behavior. When an upstream transition is blocked, acceptance tests the accessible blocked/unavailable posture and does not invent the missing action.

| Surface | Required surface-specific acceptance |
|---|---|
| S1 — Project conversation view | `/` shows an existence-neutral authorized Project picker/recents. No-project-selected and empty-project-conversation are distinct; the latter offers the composer only when S1a is qualified, otherwise an accessible M1 availability reason. Chronology and new-update action do not force scroll. |
| S1a — Governed chat composer | Project switch, draft preservation, shortcut suppression during text entry, current-attempt live region, Stop/Cancel focus return, and proposal-link focus are deterministic. |
| S2 — Ambiguous association review | One radiogroup, safe empty candidates, evidence expiry, disabled reasons, controlled refresh, and omission parity are operable nonvisually. |
| S2a — Inbound authenticity review | Mailbox evidence, frozen digest/revision/operation identity, distinct `mailbox-admin` initiator/`policy-admin` approver, accept/reject, terminal block, retention guidance, and changed-evidence successor-only reprocess are operable without Project disclosure. |
| S3 — AI action approval | Authority/effects, expiry, unavailable reasons, revision conflict, and decision focus are explicit before any human action. |
| S4 — Correction surface | Pre-commit block, owner progress, delay, irreversible disposition, and completion remain distinguishable without color or motion. |
| S5 — Tenant admin configuration | Two-person review, scope, version conflict, bootstrap-vs-editor availability, and audited read/mutation outcomes are explicit. |
| S6 — Outbound approval | Frozen content/recipients, sender authority, classifier reason, send uncertainty, reconciliation evidence/deadline, terminal P2 escalation, and audited-`NotSent`-only new draft remain reachable and announced once; any approval-load link is informational. |
| S7 — Cross-surface attribution view | Membership, parity, unsupported/denied states, origin, and omitted results are understandable in labeled responsive records. |
| S8 — Operational dashboards | Qualification backlog, freshness, approval-volume/quality observations, controlled updates, aggregation-only scope, and audited reads work without auto-reorder, hidden detail, or approval bypass. |
| S9 — Compliance investigation | Canonical/projection distinction, redaction, identity reconciliation, chronology, and expiring data-right references are nonvisually explicit. |
| S10 — Admin queue operations | Partition/item authority, filters, deterministic server sort, pagination, assignment, approval-load observations, diagnostics, and retry/terminal states retain active context after updates. |
| O1 — Data-rights operations | A6 block, independent approval, owner-by-owner progress, hold precedence, retry/appeal, redaction, and 24-hour recipient-bound expiry are explicit. |

## Responsive & Platform

Desktop uses the FrontComposer shell, persistent Project context, dense grids, and side-by-side regions after the active FrontComposer breakpoint provides each region its specified minimum width. Tablet stacks complementary regions after the primary task. Phone and 400% zoom use one reading column, convert grids to labeled row/details, keep decision summary before actions, and move filters into normal flow without hiding active criteria.

Dialogs use `FluentDialog`. Queue filters use `FcPageToolbar` with `FluentField`, `FluentTextInput`, and `FluentSelect<TOption,TValue>`. Progress uses `FluentProgressBar`. A larger-screen continuation may preserve route, filters, item, and draft, but is optional.

## Inspiration & Anti-patterns

The experience combines professional conversation chronology, evidence-first audit discipline, and explicit authority/effect previews. It avoids consumer-chat novelty, oversized cards, raw event dumps, hidden hover actions, and optimistic completion language.

## Key Flows

### Journey 1: Business Contributor Requests AI Help From a Project Conversation

**Source mapping:** UJ1 · FR21–FR28f, FR33, FR35–FR46 · S1, S1a, S3.

1. Amira opens the authorized Project conversation and sees source evidence, attachment eligibility, classification, freshness, and qualification before AI interpretation. Switching Project rebinds authorized context, focuses the new-context heading, and announces it once.
2. At M1 she submits a governed request; admission and an immutable attempt identity appear before streaming starts.
3. A read-only/no-external-effect response may stream as visibly partial, while a six-effect request produces a human-review proposal.
4. Amira reviews the proposal's authority, files, recipients, effects, versions, expected owner revision, and expiry.
5. **Climax:** A permitted result returns to the conversation with its canonical attempt/effect and audit links, distinct from participant messages and laggable projections.

**Failure/recovery:** Missing context asks for clarification. Connectivity reconciles by identity before retry. Detector, classifier, runtime-control, or audit unavailability blocks without implying that work started.

### Journey 2: Business Contributor Resolves an Ambiguous Project Association

**Source mapping:** UJ2 · FR3–FR12, FR64–FR69, FR76–FR80 · S2, S4.

1. Marc opens an authorized `NeedsReview` item without learning whether hidden Projects exist.
2. Only authorized, conflict-free deterministic evidence with `score >= T_high` is auto-eligible. No candidate, any lower score, conflict, non-finite/scorer error, stale evidence, or unauthorized evidence enters `NeedsReview`; `T_low` may organize presentation only.
3. He moves through one radiogroup, opens current evidence, and selects without committing.
4. **Climax:** Confirm commits one decision slot after authority, revision, and evidence checks; controlled refresh preserves context.
5. He may reject all or defer with owner and revisit condition. A deferred item later offers Resume to `NeedsReview`.

**Failure/recovery:** Expired evidence blocks confirmation and offers refresh. Conflict returns the recorded winner. Direct confirm/reject from `Deferred` is not exposed; Resume first returns the item to `NeedsReview`.

### Journey 3: External Party Sends Project Context Into Hexalith

**Source mapping:** UJ3 · FR1–FR4, FR13–FR20, FR29–FR34 · S1, S2a, S2.

1. Elena sends an email and attachment through a controlled mailbox pattern without needing portal access.
2. Intake preserves mailbox-scoped provider/header/delegation evidence and resolves participant authority separately. Accepted evidence enters association; strict anomalies enter `AuthenticityReviewRequired`, and paranoid anomalies enter terminal `AuthenticityBlocked` without exposing Project candidates.
3. A current `mailbox-admin` initiates a decision over the frozen evidence digest, expected revision, and operation identity; a distinct current `policy-admin` approves before S2a accepts or terminally rejects it. A blocked/rejected record remains retained under A6; changed policy/provider evidence and the same separation of duty create a new audit-linked intake instance rather than reopening it.
4. Association uses deterministic evidence; ambiguous outcomes enter S2. Attachments remain unavailable until scan, Project authority, retention, and owner storage succeed.
5. **Climax:** Elena continues ordinary email collaboration while the authorized team receives attributable Project context and governed file status only after authenticity acceptance.

**Failure/recovery:** Authenticity block/rejection, unresolved identity, scanner failure, or audit/owner unavailability fails closed. S2a always shows a safe reason, retention guidance, and its successor-only reprocess posture; reprocessing or retry never exposes candidate Projects, unsafe bytes, or duplicate storage.

### Journey 4: Project Owner Corrects a Wrong Association

**Source mapping:** UJ4 · FR7–FR8, FR23–FR28, FR60–FR63, FR87–FR96 · S4, S1, S9.

1. Priya sees the authoritative association, revision, downstream consumers/effects, and required owner contexts.
2. She supplies rationale; pre-commit checks validate source/destination authority, the complete impact-manifest shape, invalidation path, and canonical audit.
3. **Climax:** Correction commits atomically to `Correcting`; affected source and destination context remain blocked while every owner acknowledgment or irreversible-effect disposition arrives.
4. The surface separates owner effect, repair/compensation, delay, projection, and audit evidence.

**Failure/recovery:** A pre-commit failure leaves the predecessor authoritative. `Corrected` remains unavailable until the complete manifest contract is accepted and every item is dispositioned; delay becomes `CorrectionDelayed` with owner and P2 escalation.

### Journey 5: Tenant Admin Configures Governed Email Collaboration

**Source mapping:** UJ5 · FR9, FR18–FR20, FR51–FR53, FR67–FR75g · S5, S8, S10.

1. Nora sees her bounded TenantOwner/admin authority and the difference between M0 bootstrap automation and the M1 editor.
2. For a service-client, role, mailbox, policy, limit, or safety-control change, she sees the exact scope, expiry, owner evidence, version, and required independent approver.
3. A distinct eligible person reviews the same frozen values; service and AI principals cannot initiate or approve admin-role/policy authority.
4. At M2, operational updates wait behind an accessible apply action; aggregate partition controls remain separate from Project-authorized item actions.
5. Successful dashboard reads, mutations, and rejected attempts show their appropriate audit/attempt reference with identity, scope, affected opaque items, outcome, and server time.
6. **Climax:** An accepted successor version appears with qualification, approvers, scope, and canonical audit without widening Project authority.

**Failure/recovery:** Stale authority, self-approval, invalid scope, unavailable controls, or audit failure writes no version. Revoked/expired grants require successors; unqualified SLOs stay in the qualification backlog.

### Journey 6: Developer Uses CLI To Inspect and Resolve Project Email Workflow

**Source mapping:** UJ6 · FR80–FR86, FR90–FR95a · S7, S10.

1. Leo selects a normalized operation in the Cross-surface attribution view.
2. He verifies catalog membership, surface exposure, MCP tagging, AI allowlist eligibility, owner qualification, and increment before invocation.
3. He confirms an association through an exposed surface and receives stable identity, origin, canonical transition, and audit outcome.
4. **Climax:** An authorized lookup returns the equivalent outcome through another surface while omitted resources remain omitted from data, counts, diagnostics, and timing.

**Failure/recovery:** Membership or authority failure blocks invocation; parity cannot grant access. Replay returns the prior outcome, changed input conflicts, AI/tool/service MCP approval is denied, and candidate human-delegated MCP approval remains implementation/qualification-blocked until product approval and architecture alignment.

### Journey 7: Compliance or Support Reviewer Investigates a Risky Action

**Source mapping:** UJ7 · FR54–FR63, FR85–FR86, FR90–FR91a · S8, S9, O1.

1. Sofia searches authorized audit by source, actor, origin, policy, decision, correlation, and time.
2. The view distinguishes canonical record references from projection freshness/lag, AI interpretation, replay, appended non-authoritative annotations, corrections, and outcomes.
3. She opens the authorized per-item diagnostic and sees correlation, tenant, mailbox, workflow-item, current state, retry count, reason, next action, and last-transition timestamp/actor/from-state without hidden business detail.
4. For a validated data-subject or tenant request, Sofia uses restricted O1 to initiate export, erasure, hold, or retention work; a distinct current TenantOwner with `compliance-admin` grant approves the exact scope.
5. O1 shows each source owner's authoritative acknowledgment, partial completion, hold precedence, retry or appeal eligibility, immutable audit, and a redacted recipient-bound result that expires after 24 hours. It grants no direct owner-context mutation authority and remains blocked while A6 is open.
6. **Climax:** Sofia reconstructs who acted, under which authority/evidence, what committed, and what remains projection or qualification evidence without relying on screenshots.

**Failure/recovery:** Restricted detail stays existence-neutral; stale projections show watermark and rebuild owner. Legal hold prevails, retry creates an eligible linked request, and a rejected request can be appealed only with new evidence/basis through a linked request. An unresolved current identity preserves its original ID, blocks mutation, and routes to authorized reconciliation.

### Journey 8: User Reviews an AI Action Before It Leaves the Project Boundary

**Source mapping:** UJ8 · FR39–FR50, including FR48a–FR48d · S3, S6.

1. Amira sees requester/origin, Project, command, frozen content/resources, recipients, sender/delegation evidence, versions, revision, expiry, and all effects.
2. Any six-effect boundary is permanently `approval-required`; classifier unavailability or an unknown/undeclared effect or authority tag is `classifier-indeterminate` and exposes no proposal, approval action, or execution path.
3. As a present authorized human, she approves, rejects, requests revision, or cancels. Batch appears only for the fully identical reversible safe shape.
4. **Climax:** Execution revalidates every authority, digest, effect, version, lifetime, control, and owner revision, then shows owner result separately from delivery and projection.

**Failure/recovery:** Drift or expiry requires a new linked proposal. Machine approval is denied. `SendOutcomeUnknown` enters reconciliation with a four-hour default deadline and never offers blind resend; exhaustion reaches terminal `Unresolved` with owner/P2 escalation, and only audited `NotSent` permits a new draft with fresh approval.

### System Journey: Governed AI Execution

**Source mapping:** System Journey · FR28a–FR28f, FR33, FR39–FR46, FR81–FR89 · S1a, S3, S6, S7, S9.

1. Ari receives bounded Project, requester, files, policy, operation/effect catalog, evidence, origin, and trust labels.
2. Untrusted email, attachments, filenames, retrieved context, and tool output are data, never authority or instructions that can change governance.
3. Eligible read-only assistance enters the low-risk attempt path; any six-effect operation requires a human-reviewed proposal.
4. **Climax:** Human approval is revalidated before the shared command atomically records the owner effect and canonical audit.
5. Advisory progress, delivery, and projections update independently and never become effect authority.

**Failure/recovery:** Unresolved association, missing context, authorization, classifier/runtime/audit unavailability, denial, or unsupported work blocks safely. A retriable terminal attempt creates one immutable linked successor and never resumes partial output or duplicates an effect.
