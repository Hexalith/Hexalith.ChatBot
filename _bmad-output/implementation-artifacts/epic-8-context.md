# Epic 8 Context: Review Operations, Notifications & Escalation

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Reviewers can find and take responsibility for work requiring human attention, see why it is waiting, and follow it to an attributable outcome. Operators can monitor queue health and direct work to the responsible role within the scope they hold. Review items remain available through a service restart or a failed notification delivery.

## Stories

- Story 8.1: Build Persistent Tenant-Safe Review Queues
- Story 8.2: Filter, Sort, and Prioritize Review Queues Server-Side
- Story 8.3: Resolve Safe Review Actions and Disabled Reasons
- Story 8.4: Claim and Assign Review Items Without Granting Authority
- Story 8.5: Show the Next Human Action for Each Review Item
- Story 8.6: Detect Stale, Waiting, Blocked, and Escalation-Needed Work
- Story 8.7: Configure Bounded Notification and Escalation Routing
- Story 8.8: Dispatch Actionable Notifications Without Approval Fatigue
- Story 8.9: Operate Review Queues on the Live S10 Surface
- Story 8.10: Qualify Review Operations and Notification Safety End to End

## Requirements & Constraints

- Track ambiguous associations, unresolved participants, pending approvals, failed ingestion, failed attachments, and retryable operations. Keep a stable subject and item identity through retries, corrections, and closure. Record the terminal outcome and time so the history can be reconstructed.
- Derive the tenant from trusted server context. Require current Project and resource authority for detail, including on a continuation or refresh. Apply authorization and redaction before computing counts, facets, priorities, or cursors. An administrator with aggregate scope can see safe counts above the privacy threshold, age, freshness, and owner role. Project names, item references, links, evidence, and precise failure reasons require additional authority.
- Each item identifies its permitted next action and responsible role. Claiming and assignment coordinate responsibility. Decisions and retries require separate authority at submission. For concurrent claims, the first committed transition wins and the other caller receives a safe conflict.
- Keep notifications bounded per reviewer. Limit pushes to eight per hour and 30 per day, move overflow into a digest, and flag overload above 25 open applicable items. Verify recipients' current permissions at dispatch. Every approval retains its own decision and audit history, including approvals displayed together.

## Technical Decisions

- Build durable read models from committed events. Apply updates in source-version order and make redelivery safe. An older event cannot reopen a terminal item, revoke an assignment, or replace newer guidance. Keep item evolution and notification attempts attributable through operation and correlation identifiers. Record server times in UTC.
- Derive stale, waiting, blocked, and escalation indicators against versioned operating thresholds. Surface the affected scope, freshness, owner, and safe next step when the projection is unavailable or behind. Emit metadata-only depth, age, retry, failure, and lag observations. A healthy empty queue requires affirmative evidence from its source.
- Evaluate allowed filters and stable ordering on the server. Bind opaque cursors to the tenant and the authorized query, including its sort and version. Limit pages to 100 rows. Representative reads target p95 of two seconds with healthy dependencies. Return a retrievable degraded or pending state when the result is unavailable.
- Route mutations through the governed command path with current authority, revision, policy, and audit checks. Keep backlog indicators and escalation independent of workflow decisions. Configure notification channels, recipients, digests, and escalation rules through versioned tenant policy and retain the policy version associated with each attempt.

## UX & Interaction Patterns

The S10 queue workspace uses labelled filters and compact rows with accessible status, owner, age, and next action. Preserve focus, filters, and selection across refresh and navigation to the workflow that owns a decision. Explain applicable disabled actions in reachable text. Suppress actions whose existence would disclose protected details. Distinguish empty, filtered, stale, unavailable, and redacted views. Cover keyboard and screen-reader use, mobile reflow, forced colors, reduced motion, and English and French on the live surface.

## Cross-Story Dependencies

The event-producing workflows supply committed transitions and source versions. Tenant administration supplies role and partition scope, while Project authority controls access to item detail. Durable queues supply the later ordering, action, assignment, escalation, and notification stories. The S10 surface consumes those contracts through the typed client. Operations dashboards consume safe observations. The end-to-end qualification follows an item from source through closure and audit and exercises authority changes, concurrent reviewers, delivery failure, restart, and native-store isolation. Runtime controls shown on the queue require the owning governance capability and its evidence.
