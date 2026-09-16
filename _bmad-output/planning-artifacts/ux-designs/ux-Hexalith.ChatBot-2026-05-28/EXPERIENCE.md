---
name: Hexalith.ChatBot
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
---

# Hexalith.ChatBot — Experience Spine

## Foundation

Hexalith.ChatBot is a responsive enterprise conversation workspace in which authorized people understand Project context, review ambiguous intake, govern AI assistance, perform bounded administration, and reconstruct outcomes. Microsoft Blazor Fluent UI v5 → Hexalith.FrontComposer → `DESIGN.md` is the visual inheritance chain; this document owns information architecture, behavior, states, responsive continuity, and accessibility.

Desktop/laptop is primary, but every in-scope task remains complete at 320 CSS pixels and 400% zoom. A larger-screen handoff is optional. Source records, AI interpretation, canonical outcomes, laggable projections, authority, effects, freshness, qualification, and recovery remain distinguishable in words—not only by color, placement, or icons.

The finalized PRD and approved normative addendum are the product authority. `.memlog.md` records durable UX decisions and overrides. The product brief is historical context for user needs and language, not competing scope. Surface/conformance supplements may elaborate interaction behavior only inside the product contract. The architecture spine is a downstream implementation reference: it may clarify safe status behavior, but does not override product intent. The reconciliation files identify applied decisions and unresolved non-UX contracts. If a future mockup, wireframe, prototype, or imported reference conflicts with this spine pair, `DESIGN.md` and `EXPERIENCE.md` take precedence until the spine is intentionally revised.

This revision is intentionally spine-only. No approved `imports/`, `mockups/`, or `wireframes/` artifacts exist, so every IA surface is specified by these spine tables and no visual artifact is orphaned.

### Contract map

[Information Architecture](#information-architecture) · [Component Patterns](#component-patterns) · [Governed Action Boundary](#governed-action-boundary) · [State Patterns](#state-patterns) · [Interaction Primitives](#interaction-primitives) · [Accessibility Floor](#accessibility-floor) · [Key Flows](#key-flows)

### Terminology

`Safe` means the presentation reveals only data authorized for the current actor and offers only a permitted next action. `Qualified` means every required immutable gate record is `approved-current` for the exact candidate, dependencies, environment, evidence, independent approvals, expiry, and reopen predicates, and that Release Governance has published one complete gate set consumed by CI, release, and runtime. Exact gate status is `open|approved-current|expired|invalidated|superseded`; incomplete, mixed-candidate, expired, invalidated, or superseded sets fail closed. Ordinary Project surfaces expose only computed status, safe reason, owner, fallback, and recheck; only operator/admin/release-evidence views may expose a safe gate-set reference. `Current` authority, evidence, or revision is unexpired and not superseded when the decision runs; a `present human` is an authenticated human with a recorded user-presence signal for that decision. Prefix `owner` with its role: Project owner, effect owner, source owner, or rebuild owner. An `authority tuple` is the source-defined set of actor identity/type, role, scope, delegation, and freshness fields. A `decision_slot_id` identifies the first-commit-wins decision. A `typed status` contains a contract state plus a stable reason. A `successor` is a new immutable workflow or attempt linked to its preserved predecessor.

### Scope and increment availability

| Increment | Available experience | Entry condition shown to users and operators |
|---|---|---|
| M0 | S1, S2 with its S2-owned correction detail, S2a, S3, and restricted O1. Mailbox/attachment/participant workflows and minimum bootstrap automation support them but do not imply an M1/M2 editor. | A5, A6, and A13 must be `approved-current`; exact TaskIntentDetector and ActionRiskClassifier artifacts are disabled until their A9a M0 records are `approved-current`. A blocked business capability shows computed status, safe reason, owner, evidence age, permitted fallback, and recheck action; it does not expose a raw gate-set identifier. |
| M1 | S1a and S4–S7, including FR28a–FR28f attempt controls; S4 extends the M0 S2 correction contract. | A5/A6/A13 and exact deployed A9a records must be revalidated `approved-current`; A11-M1 must be `approved-current`. A8 defines the two-member AI allowlist, not a separate release-gate status. |
| M2 | S8–S10. | Exact deployed A9a qualification remains `approved-current`; A10 and A11-M2 are `approved-current`; inherited A5/A6/A13/A11-M1 records are revalidated for the release candidate. |

The six AI-mediated effect classes are permanently `approval-required`: Project-state mutation, file exposure, external communication, task creation or assignment, external-tool invocation, and acting on behalf of a participant. Tenant policy cannot downgrade them. A direct authorized human command follows its applicable command/audit contract; it does not become an AI proposal merely because it writes state.

### Upstream blockers and open contracts

| Open contract | Why UX cannot resolve it | In-review posture |
|---|---|---|
| Release qualification | Product contract approval is resolved, but the final PRD claims no closure for A5, A6, A9a, A10, A11-M1, A11-M2, or A13. | Keep both UX spines `in-review`; business surfaces show computed safe qualification, while operator/admin/release-evidence views may show immutable gate-record provenance and a safe gate-set reference. Make no pilot/release/readiness claim from document status. |
| Implementation and owner-contract closure | The architecture is reconciled, but UX cannot close producer-owner adapter mappings, audit-boundary implementation, correction-owner acceptance, identity-evolution acceptance, or qualification/operational evidence. | Follow the aligned product and architecture contracts, fail closed where mapped owner implementations or evidence are missing, and keep the affected capability implementation/qualification-blocked. |
| A6 data-protection gate | O1 now has an interaction contract, but pilot use still requires accepted purpose, retention, hold, erasure, backup, key-custody, and surviving-metadata evidence. | Show the restricted O1 request/status/result experience as qualification-blocked until A6 passes; do not claim GDPR satisfaction. |
| A12 identity evolution | `IdentityEvolved` remains a proposed external dependency until each producer accepts its schema and reconciliation contract. | Preserve original identifiers; unresolved current identity blocks mutation and routes to authorized reconciliation without rewriting history. |
| FrontComposer grid and dialog conformance | The pinned generator re-evaluates the FC-TBL client/server lane on render rather than honoring the documented first-mount latch; the shell has no shared cross-modal opening arbiter; and `FcDestructiveConfirmationDialog` does not yet localize all framework/generated copy. | Keep affected grids and modal/destructive paths qualification-blocked until FrontComposer supplies the aligned lane, arbitration, and localization contracts. Do not introduce ChatBot-local grid or dialog forks. |

This pair is the maintained UX spine, not a story-level component specification.

## Information Architecture

| Surface | Increment | Primary purpose |
|---|---:|---|
| S1 — Project conversation view | M0 | Land at `/`, select from authorized Projects/recents, and read conversation, source evidence, attachments, task intent, approvals, and governed outcomes. A wrong-association indicator deep-links to S2; S1 owns no correction controls. |
| S1a — Governed chat composer | M1 | Submit Project-scoped assistance and control immutable generation attempts. |
| S2 — Ambiguous association review | M0 | Decide email-to-Project association using safe, current evidence and own M0 correction: inline authorized entry, persistent status, and a durable detail for rationale through immutable history. |
| S2a — Inbound authenticity review | M0 | Let a current `mailbox-admin` initiate review of fresh mailbox-scoped provider/header/delegation evidence and a distinct current `policy-admin` approve the frozen decision without exposing Project candidates. |
| S3 — AI action approval | M0/M1 | Review authority, effects, proposal revision, expiry, and human decision. |
| S4 — Correction surface | M1 | Extend—not originate—the S2 correction contract for cross-surface M1 work while preserving the same states, detail anatomy, context restoration, and safety behavior. |
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
| S1 — Project conversation view | Source evidence (expanded), AI summary, Why this project | `/` landing, authorized Project picker/recents, header, stream, persistent operation/connectivity state, and correction deep-link only; no-project-selected and empty-conversation states remain distinct |
| S1a — Governed chat composer | Request context (expanded), Source evidence, Attempt history | Composer, current attempt, Stop/Cancel, and advisory progress |
| S2 — Ambiguous association review | On the linked correction detail: Impact/evidence (expanded), Owner progress/recovery, Immutable history | Candidate radiogroup, decision bar, inline authorized correction entry, and persistent correction status remain visible together as the sole carve-out. In the durable S2 detail, the primary rationale/replacement-evidence form stays outside before commit; persistent canonical status stays outside after commit. It preserves and restores the originating S2 item, filters, selection, and focus. No correction dialog. |
| S2a — Inbound authenticity review | Provider/header/delegation evidence (expanded), Retention and immutable history | Mailbox scope, fresh evidence digest, expected revision, operation identity, distinct `mailbox-admin` initiator/`policy-admin` approver, safe reason, Accept/Reject decision, and successor-reprocess guidance; no Project candidate data |
| S3 — AI action approval | Authority/effects (expanded), Source evidence, Execution/audit | Proposal summary, expiry, and human approval controls |
| S4 — Correction surface | Impact/evidence (expanded), Owner progress/recovery, Immutable history | Before commit, the primary rationale/replacement-evidence form stays outside; after commit, persistent canonical status stays outside. M1 entry/status extend the S2-owned contract and return to the originating context. |
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

Every refusal, blocked, degraded, failed, denied, or waiting presentation resolves to the versioned safe-message catalog: stable code, user-safe headline of at most 80 characters, one non-leaking reason sentence, and one permitted next action from `retry|escalate|dismiss|request access`. Never display raw error text and never use “contact support” as next-step guidance.

## Component Patterns

This behavioral catalog has the same names and order as the paired [visual component catalog](DESIGN.md#components).

All read-only projection lists use generated FC-TBL `[Projection]` views by default. A direct `FluentDataGrid` is a reviewed FrontComposer Level-2/Level-3 customization only; it keeps the generated view key, stable row identity, keyboard semantics, responsive transformation, and every adjacent load/error/empty/filter/cap/slow-query surface. Semantic status outside grids uses visible-label `FcStatusBadge`. Generated `[ProjectionBadge]`/`FcStatusIcon` cells are the compact-grid exception: a shape-distinct glyph, contextual accessible name, keyboard/hover tooltip, and status text in responsive labeled records. Raw `FluentBadge` is limited to non-status categories, counts, filter chips, and optimistic summaries; a missing inherited status slot requires a reviewed FrontComposer extension rather than an ad hoc substitute.

| Component | Experience contract |
|---|---|
| Project context header | `{components.project-context-header}` keeps authorized Project, tenant where relevant, surface, increment, computed qualification, safe reason, owner, fallback, and recheck visible across reflow. Ordinary Project headers never expose raw gate-set identifiers; operator/admin/release-evidence variants may link to a safe gate-set reference. |
| Conversation shell | `{components.conversation-shell}` preserves context, stream, composer, and complementary evidence while distinguishing source, AI, and system items. |
| Conversation stream | `{components.conversation-stream}` is a uniquely named transcript region with semantic `list`, `feed`, `log`, or equivalent item/navigation semantics. Each item exposes stable ID, monotonic sequence, source/origin, canonical or advisory state, and programmatic relations to its originating request/evidence/proposal/outcome. A keyboard-reachable first-unseen/new-updates target enters the first unseen item without forcing scroll; partial streamed text never appears as committed. |
| Composer action entry | `{components.composer-action-entry}` separates governed chat from effectful AI requests, keeps Stop/Cancel stable and keyboard operable, and suppresses single-character or modifier-free application shortcuts while focus is in the composer without interfering with text entry or assistive-technology commands. |
| Actor badge | `{components.actor-badge}` names human, AI, service-client, mailbox, worker, or system origin without color alone. |
| Message classification | `{components.message-classification}` shows `informational` or `actionable`; actionable resolves to `request-information|request-action|request-decision`. It exposes detector version, evidence offsets, confidence, and review/capture/dismiss without implying an action-risk result or disguising a proposal/system event as a participant message. |
| Surface section group | `{components.surface-section-group}` owns the sibling section items; expansion never hides the primary task or warning. |
| Source evidence | `{components.source-evidence}` is default-visible and identifies source-evidence ID, immutable origin/trust label, redaction state, extraction boundary, reference freshness, and decision evidence. |
| AI summary | `{components.ai-summary}` starts collapsed, is labeled `AI summary`, is preceded by `Generated by <model+version> at <timestamp> from <source-evidence-IDs>`, and never outranks source evidence. |
| Why this project | `{components.why-this-project}` exposes originating signal class, matched value, confidence score, `auto|needs-review`, typed reason, visible-candidate rule, decision actor/timestamp, and superseding-correction links. |
| Evidence freshness | `{components.evidence-freshness}` shows per-reference snapshot time and `fresh`, `stale`, or `expired`. |
| Task intent review | `{components.task-intent-review}` shows tenant/Project/source message/requester, ≤280-character summary, `request-information|request-action|request-decision|informational`, evidence offsets, detector version, confidence, detected time/state, and source in full, or a detector-unavailable manual-review path. |
| Attachment row | `{components.attachment-row}` shows scan/storage/quarantine, folder, retention, retry, and AI-context eligibility; unsafe content never opens. |
| Association candidate group | `{components.association-candidate-group}` is one radiogroup; selection does not commit, and unsafe candidates/cardinality are omitted. |
| Association decision bar | `{components.association-decision-bar}` keeps selection and safe actions visible; from `Deferred`, Resume is the only settled action. |
| Association correction entry | `{components.association-correction-entry}` is visible inline only to an authorized Project owner with current source/destination authority. It shows predecessor/revision and persistent `Correcting|CorrectionDelayed|Corrected` status, opens the durable S2 detail, and never opens a dialog. |
| Association correction detail | `{components.association-correction-detail}` is S2-owned and preserves the origin route, queue item, filters, selection, and focus. Before commit its primary rationale/replacement-evidence form stays outside the accordion; after commit persistent canonical status stays outside. The accordion contains complementary impact/evidence, owner progress/recovery, and immutable history. S1 only deep-links; S4 extends it. |
| Action classification | `{components.action-classification}` shows determinate `low-risk|approval-required` with allowlist entry, effect surface, policy snapshot, requester authority, and Project/file/recipient/tool scopes; it separates pre-classification `denied|unsupported` and terminal `classifier-indeterminate`. |
| AI proposal panel | `{components.ai-proposal-panel}` links source request, Project, proposal revision, classification, and one human decision unit. |
| Approval authority and effects | `{components.approval-authority-and-effects}` shows current allowlist command, tappable file evidence/redaction, recipients, sender-authority class, classifier input tuple, policy snapshot, `approved_at`/`expires_at`, requester/current-human independence and presence, expected resource changes/side effects/audit events, freshness, and versions. |
| Approval controls | `{components.approval-controls}` keeps `approve|reject|request-revision|cancel` stable. Every action is `enabled`, `disabled-with-reason`, or `not-applicable-hidden`; disabled reasons come only from `insufficient-authority|state-not-permitted|dependency-degraded|awaiting-other-actor|policy-blocked` and are programmatically associated. Forbidden/leaking actions remain absent. |
| Correction progress | `{components.correction-progress}` distinguishes pre-commit blocking, committed correction, owner acknowledgments, delay, and irreversible effects. ChatBot owns immutable manifest membership/lifecycle and stable item identities; only each item's A13-mapped authenticated owner adapter/actor may acknowledge repair/rebuild or disposition it. `CorrectionDelayed` shows owner, next safe action, and P2 escalation. |
| Qualification status | `{components.qualification-status}` names capability/increment, exact `open|approved-current|expired|invalidated|superseded` state, immutable gate record, exact candidate/dependencies/environment/evidence/approvers/expiry/reopen binding, evidence age, owner, fallback, and recheck without treating prose or file presence as release proof. Gate-set reference is restricted to operator/admin/release-evidence views. |
| Bounded admin scope | `{components.bounded-admin-scope}` separates aggregate visibility/partition control from Project-authorized item access. |
| Service-client grant | `{components.service-client-grant}` shows class, exact scope, expiry, initiator, distinct approver, owner evidence, version, and successor status. |
| Two-person approval | `{components.two-person-approval}` requires distinct eligible people and shows changed values, scope, reason, version, expiry, and conflict. |
| Runtime control status | `{components.runtime-control-status}` shows subject, mode, scope, version, freshness, capability, owner, and fail-closed result. |
| Shared operation status | `{components.shared-operation-status}` returns operation identity/current typed status within five seconds p95. After 30 seconds it provides a retrievable detailed status with retry count, partial-output marker, terminal reason, next safe action, and correlation while separating coordinator, owner effect, delivery, projection, attempt, retry, and prior outcome. |
| Operation diagnostic | `{components.operation-diagnostic}` discloses only authorized runbook metadata: correlation ID, tenant ID, mailbox ID, workflow-item ID, current state, retry count, failure-reason code, next safe action, and last-transition timestamp, actor, and from-state, plus safe version and predecessor/successor links. |
| Queue row | `{components.queue-row}` shows queue/health name, current depth or exact `healthy|degraded|failed|unknown`, oldest item age, triage-owner role, authorized detail link, scope, categorical action-risk class where applicable, and confidence only for association/task-intent evidence. Numeric confidence never represents action risk. It also shows freshness, attempts, next action, and terminality without hidden detail. |
| Operational SLO dashboard | `{components.operational-slo-dashboard}` says “SLO qualification backlog” until target, source, window, budget, alert, owner, and A11 evidence qualify. It pairs each applicable metric with queue/health name, current depth or exact `healthy|degraded|failed|unknown`, oldest age, triage owner and authorized detail link, source/window, and freshness, and uses exactly `within-budget|approaching|exhausted|unsupported`; missing target, live signal, route, or burn test is `unsupported`. Approval volume, median/p95 queue age, reviewer load, rejection/revision, and rubber-stamp indicators are informational only. |
| Audit timeline | `{components.audit-timeline}` separates canonical records, rebuildable projection, replay tagged with `replay_run_id`, annotation, correction, and outcome; production audit views and completeness calculations exclude replay by default. |
| Inbound authenticity and sender authority | `{components.inbound-authenticity-and-sender-authority}` separates provider evidence from identity, delegation, and outbound sender authority. |
| Retention and export request | `{components.retention-and-export-request}` shows data classes, authorization, owner, hold/redaction limit, recipient expiry, and canonical state. |
| Redacted support bundle | `{components.redacted-support-bundle}` previews safe metadata/exclusions; external exposure requires explicit human confirmation. |
| Blocked state | `{components.blocked-state}` resolves to the versioned catalog and gives stable code, ≤80-character safe headline, one non-leaking reason sentence, affected safe scope/owner, and one action from `retry|escalate|dismiss|request access`. |
| Connectivity status | `{components.connectivity-status}` distinguishes before-submit disconnect, pending-unknown, reconciling, and restored without guessing outcomes. |
| Controlled update bar | `{components.controlled-update-bar}` holds list updates during review and applies them without moving the active row. |
| Status toast banner | `{components.status-toast-banner}` announces a transition once; persistent status stays inline. |
| Busy region | `{components.busy-region}` sets `aria-busy="true"` on the named owning region, emits one localized deduplicated start and terminal status, replaces in place, preserves layout/focus context, and keeps decorative skeletons out of the accessibility tree while honoring reduced motion. Success, empty, error, blocked, cancellation, and authorized-context replacement all clear the busy state. |
| Error summary | `{components.error-summary}` receives focus after invalid submit and links to each affected control. The control sets `aria-invalid="true"` and `aria-describedby` to stable localized error/help IDs; correction, successful validation, or replacement by a newer server state clears stale invalid state and obsolete associations while preserving valid input. |
| Review dialog | `{components.review-dialog}` renders `FluentDialogBody` through inherited `IDialogService`, the sole dialog-lifetime owner. A required FrontComposer shell arbitration service owns every review, settings, command-palette, and destructive-dialog open. While one modal is active, a second opener is rejected with one localized scoped status and focus stays on its trigger; it is never queued, nested, or allowed to close the active modal implicitly. Safe initial focus, Cancel/Escape, contained focus, and return to the invoking control or documented successor are required. Eligible destructive confirmation uses `FcDestructiveConfirmationDialog`; its French path remains qualification-blocked until FrontComposer localizes all framework/generated copy. Association correction is explicitly ineligible and remains inline plus durable S2 detail. |
| Queue filter bar | `{components.queue-filter-bar}` provides labeled server filters, explicit server-side sort with deterministic tie-break, active summary, omission-safe result count/order, ≤100 pagination, narrow-screen reflow, and focus/selection preservation. |

## Governed Action Boundary

### Increment-specific AI boundary

- M0 AI invocation has exactly one allowlisted command: `Project.AppendConversationMessage`. It is append-only in the current tenant/Project and is always `approval-required`; every AI-authored message receives current authorized human approval before it lands. M0 AI cannot send outbound communication, mutate files, create tasks, invoke external tools, impersonate participants, or execute a read-only low-risk assistance command.
- M1 adds only `ChatBot.ExecuteLowRiskAssistance` to the AI-invocable set. It may execute a product-declared, tenant-enabled, Project-authorized read-only/no-external-effect subtype; any boundary-crossing request returns `approval-required` and follows a separate proposal. Catalog membership alone never grants AI invocation.

The S1a admission presentation is exactly `accepted|needs-review|approval-required|denied|unsupported|typed failure`, and it appears before the UI implies that AI work started. These user-facing admission outcomes are distinct from the durable governed-chat attempt states below.

### Classifier and detector outcomes

| Evaluation | Stored proposal/state? | User-visible behavior |
|---|---:|---|
| Valid, qualified classifier returns eligible `low-risk`; request is authorized and has no boundary-crossing effect | No approval proposal | Execute through the governed low-risk path; show provenance, immutable attempt, and result. |
| Valid, qualified classifier returns `approval-required` with a fully declared effect and authority tuple | Yes | Explain classification and require an authorized, present human decision. |
| Classifier artifact is missing, invalid, unqualified, fails, or returns an unknown/undeclared effect or authority tag | No | Return `classifier-indeterminate`; create no proposal or durable domain/idempotency state, perform no effect, and expose no approval action. `classifier-unavailable` may appear only as a non-canonical availability reason. The separately typed redacted non-mutating auditable attempt records the original identity and can deny its reuse, but cannot authorize continuation or serve as domain idempotency. After remediation, the requester submits a fresh `operation_id` with an immutable predecessor link and classification must restart determinately. |
| Authorization, Project/tenant/actor scope, evidence safety, audit readiness, or supported-operation admission fails | No | Deny safely before classification where applicable; approval cannot override it. |
| Operation is outside the supported catalog | No | Show `unsupported`; perform no mutation or external effect. |

Task intent keeps detector availability separate from risk classification. A valid detector artifact may produce `Detected` or `NeedsReview`. A missing, invalid, unqualified, or failed artifact produces `detector-unavailable`, creates no detector-derived domain state, and offers authorized manual review or retry with a separate auditable attempt.

### Untrusted instruction handling

Email bodies, quoted threads, attachments, filenames, extracted text, retrieved Project context, model output, and tool results remain untrusted data with immutable origin, source-evidence ID, trust class, redaction state, and extraction boundary. If suspicious instruction-like content is detected, show `untrusted-instruction-detected`, prevent model/tool invocation, and link the redacted security-sensitive attempt. An authorized Project actor may inspect the source, exclude or quarantine the fragment through its source-owner workflow, and submit a new request. MVP exposes no trust-promotion action.

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

### Qualification, health, and dashboard vocabularies

| Presentation family | Exact values and authority |
|---|---|
| Gate record | `open`, `approved-current`, `expired`, `invalidated`, `superseded`; computed from one immutable record and never from prose, file presence, or a local Boolean. |
| Gate set — operator/admin/release evidence only | A safe reference resolves to the published `gate_set_id`, which references every required record for one exact candidate. Incomplete, mixed-candidate, expired, invalidated, or superseded sets fail closed; CI, release, and runtime consume the same set. Ordinary Project surfaces never expose the raw identifier. |
| Queue/health | Current depth when the surface is a queue, or exactly `healthy`, `degraded`, `failed`, `unknown` when it is health. Counts never derive the status enum. |
| SLO/error budget | `within-budget`, `approaching`, `exhausted`, `unsupported`; absent numeric target, live signal, route, or burn test is `unsupported` and blocks the associated M2 claim. |

### Collection loading and windowing

Generated FC-TBL `[Projection]` is the default for every read-only projection grid. The required behavior selects generated `Items` below `FcShellOptions.VirtualizationServerSideThreshold` or `ItemsProvider` at or above it on first mount, then changes lane only through explicit invalidation/remount. The configured threshold remains strictly below `MaxUnfilteredItems`; every rendered row uses a stable domain/projection key. The pinned generator currently re-evaluates item count on every render, so affected collection surfaces remain qualification-blocked until FrontComposer implements the latch or proves an equivalently safe explicit rebind that preserves focus, scroll anchor, expanded detail, and fetch semantics. ChatBot must not fork the generator. Direct `FluentDataGrid` composition still requires a reviewed Level-2/Level-3 FrontComposer exception and preserves the same view key and contract.

Loading, query error, unfiltered-empty, filter summary/filter-empty, `FcMaxItemsCapNotice`, and `FcSlowQueryNotice` remain adjacent to their grid, never synthesized as data rows. `FcExpandInRowDetail` stays outside the virtualized body and retains a stable controlled region. Conversation transcript and audit history are bounded server-windowed chronological collections with explicit older/newer navigation, stable item keys, preserved anchor/focus, and no unbounded client fetch or infinite DOM.

During any scoped fetch, the named owning region alone carries `aria-busy="true"`. One localized deduplicated status message announces meaningful loading start and terminal success, empty, error, blocked, or cancellation; polling ticks and decorative `FluentSkeleton` elements are excluded from the accessibility tree. The busy state also clears without a stale completion announcement when authorized context is replaced.

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

M0 correction starts and remains owned by S2. An authorized inline entry and persistent status summary open the durable S2 detail; no modal is permitted. S1 may deep-link without correction controls, and M1 S4 extends the same contract. Before commit, the primary rationale/replacement-evidence form stays outside the accordion; after commit, persistent canonical status stays outside. Complementary impact/evidence, owner progress/recovery, and immutable history share the one accordion. The detail preserves the originating S2 queue item, filters, candidate selection, and focus, and restores them when the user returns.

Before correction commits, collect rationale and replacement evidence, then validate source/destination authority, predecessor revision, complete frozen impact-manifest shape, owner commands, invalidation delivery, runtime dependencies, and canonical audit durability. The ChatBot correction aggregate solely owns immutable manifest membership and lifecycle. Every frozen item has one stable identity derived from correction, owner context, owner resource/effect, source version or effect digest, and required outcome. If the invalidation queue or atomic audit boundary is unavailable, no correction state commits; the predecessor remains authoritative and the UX shows a pre-commit block. Only an atomic start may show `Correcting`.

After commit, show each owner acknowledgment, repair/rebuild, compensation, or explicit irreversible-effect disposition. Only the A13 mapping's named authenticated owner adapter/actor may acknowledge or disposition its item; the coordinator, projection, and UI cannot self-acknowledge or change membership. Affected AI context remains blocked. The UX may show `Corrected` only after every item in the frozen complete manifest—including Conversations/Folders-owned records and irreversible effects—has an immutable acknowledgment or explicit disposition. A missed propagation SLO enters `CorrectionDelayed`, exposes owner and next safe action, and triggers P2 escalation.

An uncertain correction outcome remains pending while the detail reconciles by stable operation identity. Retry is hidden until reconciliation proves a typed retryable pre-effect failure; a permitted retry creates the defined linked successor and never starts a second correction blindly.

### Governed chat attempts

Streaming text is advisory and uncommitted. Operation identity and current typed status return within five seconds p95. If work lasts more than 30 seconds, the UI offers a retrievable detailed status with retry count, partial-output marker, terminal reason, next safe action, and correlation. Metadata such as phase, elapsed time, attempt ID, stop availability, and safe reason may update; the UI re-queries typed attempt status and never treats tokens or percentages as effect evidence. One uniquely named polite status region is scoped to the current attempt and announces deduplicated `Generating`, `Completed`, `Response stopped`, or failure transitions; streamed chunks remain outside live announcement. Stop/Cancel stays in a stable Tab position and returns focus to the composer or the linked proposal/status. A winning stop discards uncommitted output. `Completed` presents one committed response; `Stopped` and `Failed` preserve no partial committed message. Retry creates a new immutable linked attempt and never resumes or overwrites its predecessor.

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
- Every review action is visibly `enabled`, `disabled-with-reason`, or `not-applicable-hidden`. The only disabled reason codes are `insufficient-authority`, `state-not-permitted`, `dependency-degraded`, `awaiting-other-actor`, and `policy-blocked`; guidance names the responsible role or an available action, never “contact support.”
- Refresh/conflict preserves valid input. Invalid submit or attempted activation of an unavailable action focuses `{components.error-summary}`; prior-outcome/retry results focus `{components.shared-operation-status}`. Success, delayed, blocked, retryable, and terminal outcomes move focus to the owning status or error-summary heading, are announced once, and retain valid fields, selections, and drafts. Correction navigation additionally restores the originating S2 context.
- An invalid control exposes `aria-invalid="true"` and a stable `aria-describedby` chain to localized field error and help text; the error summary links back to it. Corrected input, successful validation, or a newer authoritative state removes stale error text, invalid state, and obsolete description references.
- Aggregate admin actions operate only on opaque partitions. Per-item claim, assign, retry, reprocess, quarantine, dismiss, or domain decision requires current Project authority and the owner command.

### Session expiry and reauthentication

The configured identity provider owns session lifetime, expiry detection, warning/extension policy, and whether any warning is permitted. FrontComposer owns only the authentication challenge and sanitized relative return bridge, including the mapped `/authentication/challenge` endpoint; ChatBot does not invent an expiry timer, extension control, or login UI. A security policy may require an immediate challenge with no warning, and the UX must not delay or override it.

Before challenge, ChatBot preserves only eligible non-sensitive draft and selection state; restricted content, expired evidence, approval decisions, and authority claims are never cached as resumable authority. On return, ChatBot revalidates authenticated authority, tenant, Project access, surface/operation membership, evidence freshness, and current revision before restoring safe context. If still authorized, restore the sanitized route, eligible draft/selection, and focus target; otherwise discard ineligible state, route to an existence-neutral safe context, focus its heading/status, and explain the safe next action without revealing the former resource.

### Operation membership is separate from outcome parity

An operation belongs to a surface only when every membership gate passes. Outcome parity is evaluated only after membership; it cannot make an unexposed or unauthorized operation invocable.

| Membership gate | Required answer |
|---|---|
| Product operation catalog | This exact normalized operation is supported in this increment. |
| Surface exposure | It is explicitly exposed on UI, CLI, and/or MCP for this actor class. |
| MCP tool tag | The typed tool declares the exact operation and effect profile. |
| AI allowlist | AI-originated proposal eligibility and all applicable six-effect classes are explicit. |
| Owner executable/qualification | The target owner contract is accepted, qualified, current, and callable. |

For valid members, UI/CLI/MCP preserve normalized input, authorization/redaction, state transition, stable identity, origin attribution, audit envelope, retry/conflict, and long-running outcome. Presentation may differ. Human-delegated MCP approval is candidate M1 behavior only when current user presence and `actorType=human` are recorded; it remains unavailable until the implementation proves the aligned shared-pipeline decision contract, delegated user presence, principal type, parity, and applicable qualification evidence. AI, tool, and service principals are structurally denied and cannot approve or co-sign.

### Omission parity

Authorization failure yields the same existence-neutral omission across candidates, counts, filters, typeahead, detail links, exports, notifications, diagnostics, CLI, and MCP. Do not return placeholders, rank gaps, total counts, timing distinctions, or errors that reveal a hidden item. Authorized-empty, filtered-empty, and denied results differ only where that distinction is safe.

The authorization/redaction transform runs before content enters the rendered Document Object Model (DOM) or accessibility tree. It covers accessible names and descriptions, live/status regions, hidden or collapsed content, table/grid metadata, clipboard, transcript/download, export, and read-aloud output. Announce that content is redacted only when that fact is safe; never retain the removed value, candidate count/order, or identifying description in accessibility metadata.

When historical identity has no accepted current successor, show `unresolved-current-identity`, preserve the original identifier, reject mutation, and route to authorized reconciliation. History is never rewritten; immutable migration links appear only after the producing context accepts the versioned evolution contract.

### Retry and replay

Equivalent replay returns the recorded outcome. Changed input under the same identity returns a typed conflict and performs no new effect. Retry is offered only for the exact retryable pre-effect/failed state and current revision, creating the defined linked successor.

M2 simulation/replay is visibly isolated from production: every replay event shows `replay_run_id`, production audit queries exclude replay by default, audit-completeness numerator and denominator exclude replay, and credential/egress/production-store invariance failure is a stop-ship state rather than a warning.

### Outbound reconciliation

Outbound `SendOutcomeUnknown`, `Reconciling`, and `Unresolved` never offer resend. Reconciliation shows its evidence status and four-hour default deadline; exhaustion enters terminal `Unresolved` with owner and P2 escalation. Only an audited `NotSent` permits a new draft and fresh human approval, never a retry of the uncertain send. Pre-send revalidation checks frozen content, recipients, authority tuple, approval lifetime, and revision.

### Service-client administration

M0 bootstrap exposes only the four enumerated M0 service-client classes and minimum grants. The first grant/policy snapshot uses two distinct current TenantOwner principals plus required Security approval. M1 editors show exact client/role scope, expiry, predecessor, initiator, distinct approver, owner evidence, schema/policy version, and immutable audit link. Revoked/expired grants never reactivate.

### Audit and diagnostics

Security-sensitive denials, restricted reads, and service-client failures use the separate auditable-attempt path. They never appear as a canonical mutation envelope or repair a missing one. Diagnostics are metadata-only and authorization-filtered; support bundles require redaction and explicit human confirmation before external exposure.

Across S5, S8, and S10, every successful admin dashboard read, committed mutation, and rejected attempt records admin identity, scope used, affected opaque items, outcome, and server time. Successful mutations use canonical audit; successful reads, sensitive denials, and restricted reads use the separate attempt path. Service-client and AI principals cannot initiate or approve role or policy mutation.

## Accessibility Floor

- Meet WCAG 2.2 AA: 4.5:1 normal text and 3:1 non-text controls/focus. Status never depends on color alone.
- Give every control/group a programmatic name; use semantic grids, status, errors, dialogs, accordions, and progress. Render semantic workflow, health, freshness, risk, qualification, and SLO state through visible-label `FcStatusBadge` outside grids. For generated `[ProjectionBadge]`/`FcStatusIcon` cells, accessibility-tree and keyboard tests verify the contextual localized accessible name, focusable tooltip, distinct non-color glyph, and status text in responsive labeled records. Raw `FluentBadge` remains limited to non-status uses.
- Expose the conversation transcript as a uniquely named region with semantic `list`, `feed`, `log`, or equivalent item/navigation semantics. Test stable item IDs and sequence, source/state labels, request/evidence/proposal/outcome relations, first-unseen targeting, older/newer window navigation, and no forced scroll with keyboard and screen reader.
- For each loading state, accessibility-tree tests assert `aria-busy` only on the owning named region, decorative skeleton exclusion, one localized deduplicated start/terminal announcement, and deterministic clearing on success, empty, error, blocked, cancellation, or context replacement.
- For each invalid-submit state, tests assert the control label/required state, `aria-invalid`, its stable localized error/help description, summary-to-control focus, preservation of valid input, and removal of stale errors and associations after correction or authoritative state replacement without duplicate announcements.
- Keep keyboard order aligned with visible order. Contain focus only in the active `{components.review-dialog}`, then return it. Provide skip links to main content, filters, results, and active review. Keep every keyboard or programmatic focus target at least partially visible above persistent chrome and outside overlays; use scroll margins and reveal only enough context after errors, refresh, Project switch, proposal surfacing, dialog close, or safety interruption.
- Move focus to the documented new-context heading and announce once after a Project switch updates authorized context. Link each surfaced proposal programmatically to its originating request and expose a predictable “Review proposal” focus target.
- After any success, delayed, blocked, retryable, or terminal workflow outcome, move focus to the owning status/error-summary heading and announce the outcome once. Returning from correction restores the originating S2 item and focus when it remains authorized; otherwise focus the queue summary and explain removal.
- Announce meaningful transitions once through a uniquely named, scoped status region. Do not announce streaming chunks, polling ticks, queued-count ticks, background rows, or historical content. Assertive announcements are reserved for a block caused by the current action.
- Remove skeleton shimmer, streaming cursor/typing animation, animated row movement/reordering, and nonessential dialog/toast transitions under reduced motion. Keep state text and real determinate values; textual indeterminate status must remain understandable without animation.
- Reflow actions into labeled full-width rows/steps at 320 CSS pixels and 400% zoom; wrap facts and preserve control order. Convert wide data to labeled records; allow bounded scrolling only for intrinsically two-dimensional content. Support WCAG text-spacing overrides without loss, clipping, overlap, obscured focus, or unavailable actions. Rows may grow or become labeled details; truncation never hides authority, state, reason, freshness, qualification, or the safe next action.
- Make every pointer target at least 24×24 CSS pixels or satisfy the applicable spacing, inline, or essential exception. Make touch-primary actions 44×44 CSS pixels; an essential inline control may instead use the AA minimum or spacing exception. Apply this rule to interactive badges, grid and pagination actions, disclosures, Stop/Cancel, approval controls, and narrow-screen overflow actions. Never make hover the only access path.
- Label source evidence, AI summary, canonical record, investigation projection, partial output, qualification, and restricted content explicitly.

### Localization and language

English and French have feature, state, action, unavailable-reason, and screen-reader parity. Root/page `lang` follows the selected locale, which persists across navigation and authenticated sessions. Known-language messages, quotations, attachment extracts, and AI summaries carry language-of-parts metadata. Dates, numbers, durations, and plurals are locale-aware; stable identifiers and canonical codes remain untranslated and are not tagged as prose. Accessible strings are complete localized messages, never concatenated fragments. French expansion is accepted at 320 CSS pixels, 400% zoom, and the text-spacing settings above. A destructive-dialog flow cannot qualify for French while the pinned FrontComposer component or generated renderer emits English framework copy; the platform must provide localized Cancel/default Confirm/default body plus host/domain title/body/label metadata before that flow is available in French.

### Per-surface acceptance

Every delivered surface has a real live route exercised through the running application. It independently passes server-verified primary-path success, direct assertions of functional requirements, automated checks, keyboard-only review, and screen-reader review for every state that its contract exposes: cold-load/loading, empty, validation, unauthorized/redacted, degraded, offline or pending-unknown, retryable, and terminal. Static fixtures, source scans, snapshots, and handler-only tests do not substitute. Projection-collection tests exercise the required first-mount `Items`/`ItemsProvider` selection on both sides of the inherited `FcShellOptions.VirtualizationServerSideThreshold`, explicit invalidation/remount, threshold-crossing stability, stable keys, adjacent notices, external row detail, and the reviewed exception record where a Level-2/Level-3 customization exists; the current pinned generator fails this gate until FrontComposer resolves its render-time re-evaluation. Transcript and audit tests exercise bounded server windows and older/newer navigation. Each row also proves focus order and visibility, scoped announcements, 320 CSS pixel/400% reflow, text spacing, target size, forced colors, reduced motion, and English/French behavior. Dialog acceptance opens through `IDialogService` and the required shared FrontComposer opener arbiter; a second opener from settings, command palette, review, or destructive confirmation is rejected with localized scoped status and retained trigger focus, while the active modal keeps safe initial focus, dismissal, containment, and deterministic return. Eligible destructive cases use `FcDestructiveConfirmationDialog` and remain French-qualification-blocked until its framework/generated copy is localized. When an upstream transition is blocked, acceptance tests the accessible blocked/unavailable posture and does not invent the missing action.

| Surface | Required surface-specific acceptance |
|---|---|
| S1 — Project conversation view | `/` shows an existence-neutral authorized Project picker/recents. No-project-selected and empty-project-conversation are distinct; the latter offers the composer only when S1a is qualified, otherwise an accessible M1 availability reason. Tests assert the named transcript region, item/navigation semantics, stable IDs/sequence/source/state, origin relations, bounded older/newer windows, first-unseen/new-updates focus, and no forced scroll. Wrong-association UI is a deep-link to S2 only. |
| S1a — Governed chat composer | Project switch, eligible draft preservation, exact `accepted|needs-review|approval-required|denied|unsupported|typed failure` admission presentation, operation identity/current typed status within five seconds p95, retrievable detailed status after 30 seconds with retry count/partial-output marker/terminal reason/next safe action/correlation, shortcut suppression during text entry, current-attempt live region, Stop/Cancel focus return, proposal-link focus, and session-expiry/reauth safe return are deterministic. Reauth tests cover warning when the identity provider permits it, immediate no-warning challenge when security policy requires it, authority/tenant/Project revalidation, ineligible-state discard, and safe focus recovery. |
| S2 — Ambiguous association review | One radiogroup, safe empty candidates, evidence expiry, exact review-action states/reasons, controlled refresh, and omission parity are operable nonvisually. Inline correction entry/status opens the durable S2 detail without a dialog; rationale, evidence, manifest, commit, owner progress/recovery, history, uncertain-outcome reconciliation, and origin-context restoration are keyboard/screen-reader complete. |
| S2a — Inbound authenticity review | Mailbox evidence, frozen digest/revision/operation identity, distinct `mailbox-admin` initiator/`policy-admin` approver, accept/reject, terminal block, retention guidance, and changed-evidence successor-only reprocess are operable without Project disclosure. |
| S3 — AI action approval | Authority/effects, expiry, unavailable reasons, revision conflict, and decision focus are explicit before any human action. |
| S4 — Correction surface | The M1 extension preserves S2 correction state/anatomy and context return; pre-commit block, owner progress, delay, irreversible disposition, and completion remain distinguishable without color or motion. |
| S5 — Tenant admin configuration | Two-person review, scope, version conflict, bootstrap-vs-editor availability, and audited read/mutation outcomes are explicit. |
| S6 — Outbound approval | Frozen content/recipients, sender authority, classifier reason, send uncertainty, reconciliation evidence/deadline, terminal P2 escalation, and audited-`NotSent`-only new draft remain reachable and announced once; any approval-load link is informational. |
| S7 — Cross-surface attribution view | Membership, parity, unsupported/denied states, origin, and omitted results are understandable in labeled responsive records. |
| S8 — Operational dashboards | Queue/health name, current depth or exact `healthy|degraded|failed|unknown`, oldest age, triage owner, authorized detail link, metric source/window/freshness, exact `within-budget|approaching|exhausted|unsupported`, qualification backlog, immutable gate provenance and safe gate-set reference, approval-volume/quality observations, controlled updates, aggregation-only scope, and audited reads work without auto-reorder, hidden detail, or approval bypass. |
| S9 — Compliance investigation | Canonical/projection distinction, redaction, identity reconciliation, chronology, expiring data-right references, visible `replay_run_id`, and default replay exclusion from production audit/completeness are nonvisually explicit. |
| S10 — Admin queue operations | Partition/item authority, queue name, current depth or exact health status, oldest-item age, owner, authorized detail link, filters, deterministic server sort, pagination, assignment, approval-load observations, diagnostics, and retry/terminal states retain active context after updates. |
| O1 — Data-rights operations API/provisioning interface | A6 block, independent approval, owner-by-owner progress, hold precedence, retry/appeal, redaction, and 24-hour recipient-bound expiry are explicit. |

## Responsive & Platform

Desktop uses the FrontComposer shell, persistent Project context, dense grids, and side-by-side regions after the active FrontComposer breakpoint provides each region its specified minimum width. Tablet stacks complementary regions after the primary task. Phone and 400% zoom use one reading column, convert grids to labeled row/details, keep decision summary before actions, and move filters into normal flow without hiding active criteria.

Review dialogs render `FluentDialogBody` through inherited `IDialogService` as sole lifetime owner. The required FrontComposer shell opener arbiter rejects any second review, settings, command-palette, or destructive-dialog request with localized scoped status and retained trigger focus; dialogs do not queue or nest. Eligible destructive confirmation uses `FcDestructiveConfirmationDialog` and remains French-qualification-blocked until FrontComposer localizes its framework/generated copy. Queue filters use `FcPageToolbar` with `FluentField`, `FluentTextInput`, and `FluentSelect<TOption,TValue>`. Progress uses `FluentProgressBar`. A larger-screen continuation may preserve route, filters, item, and draft, but is optional.

## Inspiration & Anti-patterns

The experience combines professional conversation chronology, evidence-first audit discipline, and explicit authority/effect previews. It avoids consumer-chat novelty, oversized cards, raw event dumps, hidden hover actions, and optimistic completion language.

## Key Flows

### Journey 1: Business Contributor Requests AI Help From a Project Conversation

**Source mapping:** UJ1 · FR21–FR28f, FR33, FR35–FR46 · S1, S1a, S3.

1. Amira opens the authorized Project conversation and sees source evidence, attachment eligibility, classification, freshness, and qualification before AI interpretation. Switching Project rebinds authorized context, focuses the new-context heading, and announces it once.
2. In the M0 vertical loop, an AI result can invoke only `Project.AppendConversationMessage`, and every append is approval-required before it lands. At M1 she may submit through S1a; `accepted|needs-review|approval-required|denied|unsupported|typed failure` and an immutable attempt identity appear before the UI implies work started.
3. Only at M1 may product-declared, tenant-enabled, Project-authorized `ChatBot.ExecuteLowRiskAssistance` stream a read-only/no-external-effect response as visibly partial; a six-effect request produces a human-review proposal.
4. Amira reviews the proposal's authority, files, recipients, effects, versions, expected owner revision, and expiry.
5. **Climax:** A permitted result returns to the conversation with its canonical attempt/effect and audit links, distinct from participant messages and laggable projections.

**Failure/recovery:** Missing context asks for clarification. Connectivity reconciles by identity before retry. Detector, classifier, runtime-control, or audit unavailability blocks without implying that work started.

### Journey 2: Business Contributor Resolves an Ambiguous Project Association

**Source mapping:** UJ2 · FR3–FR12, FR64–FR69, FR76–FR80 · S2, S4.

1. Marc opens an authorized `NeedsReview` item without learning whether hidden Projects exist.
2. Only authorized, conflict-free deterministic evidence with `score >= T_high` is auto-eligible. No candidate, any lower score, conflict, non-finite/scorer error, stale evidence, or unauthorized evidence enters `NeedsReview`; `T_low` may organize presentation only.
3. He moves through one radiogroup, opens current evidence, and selects without committing.
4. **Climax:** Confirm commits one decision slot after authority, revision, and evidence checks; controlled refresh preserves context.
5. He may reject all or defer with owner and revisit condition. A deferred item later offers Resume to `NeedsReview`. If an existing association is wrong, S2 shows the authorized inline correction entry and persistent status and opens its durable detail without a dialog.

**Failure/recovery:** Expired evidence blocks confirmation and offers refresh. Conflict returns the recorded winner. Direct confirm/reject from `Deferred` is not exposed; Resume first returns the item to `NeedsReview`. An uncertain correction reconciles by operation identity before retry, and returning from detail restores the originating S2 context.

### Journey 3: External Party Sends Project Context Into Hexalith

**Source mapping:** UJ3 · FR1–FR4, FR13–FR20, FR29–FR34 · S1, S2a, S2.

1. Elena sends an email and attachment through a controlled mailbox pattern without needing portal access.
2. Intake preserves mailbox-scoped provider/header/delegation evidence and resolves participant authority separately. Accepted evidence enters association; strict anomalies enter `AuthenticityReviewRequired`, and paranoid anomalies enter terminal `AuthenticityBlocked` without exposing Project candidates.
3. A current `mailbox-admin` initiates a decision over the frozen evidence digest, expected revision, and operation identity; a distinct current `policy-admin` approves before S2a accepts or terminally rejects it. A blocked/rejected record remains retained under A6; changed policy/provider evidence and the same separation of duty create a new audit-linked intake instance rather than reopening it.
4. Association uses deterministic evidence; ambiguous outcomes enter S2. Attachments remain unavailable until scan, Project authority, retention, and owner storage succeed.
5. **Climax:** Elena continues ordinary email collaboration while the authorized team receives attributable Project context and governed file status only after authenticity acceptance.

**Failure/recovery:** Authenticity block/rejection, unresolved identity, scanner failure, or audit/owner unavailability fails closed. S2a always shows a safe reason, retention guidance, and its successor-only reprocess posture; reprocessing or retry never exposes candidate Projects, unsafe bytes, or duplicate storage.

### Journey 4: Project Owner Corrects a Wrong Association

**Source mapping:** UJ4 · FR7–FR8, FR23–FR28, FR60–FR63, FR87–FR96 · S2, S1, S4, S9.

1. In M0, Priya enters from S2's inline authorized correction control/status or follows S1's deep-link into the durable S2-owned detail; S1 owns no correction action and no modal opens. M1 S4 extends the same flow.
2. She sees the authoritative predecessor/revision, supplies rationale and replacement evidence, and reviews downstream consumers/effects. Pre-commit checks validate source/destination authority, the frozen complete impact-manifest shape, invalidation path, owner commands, and canonical audit.
3. **Climax:** Correction commits atomically to `Correcting`; affected source and destination context remain blocked while every owner acknowledgment or irreversible-effect disposition arrives.
4. The detail separates owner effect, repair/compensation, delay, projection, and immutable audit evidence; returning restores the originating S2 item, filters, selection, and focus.

**Failure/recovery:** A pre-commit failure leaves the predecessor authoritative. `Corrected` remains unavailable until the complete manifest contract is accepted and every item is dispositioned; delay becomes `CorrectionDelayed` with owner and P2 escalation. An unknown outcome reconciles by stable operation identity before retry; a retryable pre-effect failure creates one linked successor.

### Journey 5: Tenant Admin Configures Governed Email Collaboration

**Source mapping:** UJ5 · FR9, FR18–FR20, FR51–FR53, FR67–FR75g · S5, S8, S10.

1. Nora sees her bounded TenantOwner/admin authority, exact gate records, a safe published gate-set reference, and the difference between M0 bootstrap automation and the M1 editor.
2. For a service-client, role, mailbox, policy, limit, or safety-control change, she sees the exact scope, expiry, owner evidence, version, and required independent approver.
3. A distinct eligible person reviews the same frozen values; service and AI principals cannot initiate or approve admin-role/policy authority.
4. At M2, operational updates wait behind an accessible apply action; aggregate partition controls remain separate from Project-authorized item actions. Queue/health rows show name, depth or exact health enum, oldest age, owner, authorized detail, and freshness; SLO disposition is exactly `within-budget|approaching|exhausted|unsupported`.
5. Successful dashboard reads, mutations, and rejected attempts show their appropriate audit/attempt reference with identity, scope, affected opaque items, outcome, and server time.
6. **Climax:** An accepted successor version appears with qualification, approvers, scope, and canonical audit without widening Project authority.

**Failure/recovery:** Stale authority, self-approval, invalid scope, unavailable controls, or audit failure writes no version. Revoked/expired grants require successors; unqualified SLOs stay in the qualification backlog.

### Journey 6: Developer Uses CLI To Inspect and Resolve Project Email Workflow

**Source mapping:** UJ6 · FR80–FR86, FR90–FR95a · S7, S10.

1. Leo selects a normalized operation in the Cross-surface attribution view.
2. He verifies catalog membership, surface exposure, MCP tagging, AI allowlist eligibility, owner qualification, and increment before invocation.
3. He confirms an association through an exposed surface and receives stable identity, origin, canonical transition, and audit outcome.
4. **Climax:** An authorized lookup returns the equivalent outcome through another surface while omitted resources remain omitted from data, counts, diagnostics, and timing.

**Failure/recovery:** Membership or authority failure blocks invocation; parity cannot grant access. Replay returns the prior outcome, changed input conflicts, AI/tool/service MCP approval is denied, and candidate human-delegated MCP approval remains unavailable until shared-pipeline decision enforcement, delegated user presence/`actorType=human`, parity, and applicable qualification evidence are implemented and proven.

### Journey 7: Compliance or Support Reviewer Investigates a Risky Action

**Source mapping:** UJ7 · FR54–FR63, FR85–FR86, FR90–FR91a · S8, S9, O1.

1. Sofia searches authorized audit by source, actor, origin, policy, decision, correlation, and time.
2. The view distinguishes canonical record references from projection freshness/lag, AI interpretation, replay tagged with `replay_run_id`, appended non-authoritative annotations, corrections, and outcomes; production audit and completeness exclude replay by default.
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
2. Untrusted email, quoted threads, attachments, filenames, extracted text, retrieved context, model output, and tool results remain origin-labeled data, never authority or instructions that can change governance. `untrusted-instruction-detected` prevents model/tool invocation and lets an authorized Project actor inspect, exclude/quarantine through the source-owner workflow, then submit a new request; no MVP action promotes trust.
3. Beginning in M1 only, eligible read-only assistance may enter the low-risk attempt path; any six-effect operation requires a human-reviewed proposal. M0 AI execution remains limited to approved `Project.AppendConversationMessage`.
4. **Climax:** Human approval is revalidated before the shared command atomically records the owner effect and canonical audit.
5. Advisory progress, delivery, and projections update independently and never become effect authority.

**Failure/recovery:** Unresolved association, missing context, authorization, classifier/runtime/audit unavailability, denial, or unsupported work blocks safely. A retriable terminal attempt creates one immutable linked successor and never resumes partial output or duplicates an effect.
