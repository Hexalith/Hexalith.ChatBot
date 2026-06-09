---
name: Hexalith.ChatBot M1/M2 Surface Elaboration
status: approved-gate
created: 2026-06-05
updated: 2026-06-05T12:12:05+02:00
source_proposal: ../../sprint-change-proposal-2026-06-05.md
sources:
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/prd.md
  - ../../prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md
  - EXPERIENCE.md
  - DESIGN.md
---

# M1/M2 Surface Elaboration Gate

This artifact closes the approved readiness gate for M1/M2 UX assignment. It does not add mockups or wireframes. It maps later surfaces to PRD/addendum anchors and defines the acceptance context each implementation story must import before sprint assignment.

## Gate Rule

Before any M1/M2 story that implements S4, S6, S7, S8, S9, or S10 is assigned to a sprint, the story must include:

- PRD and addendum anchors.
- UX surface and flow anchors.
- Required states and disabled-action behavior.
- Accessibility and focus-management expectations.
- Responsive behavior expectations.
- English/French localization impact.
- Redaction-safe failure and escalation behavior.

## Surface Map

| Surface | Primary PRD/addendum anchors | UX anchor | Affected epics/stories | Required elaboration |
| --- | --- | --- | --- | --- |
| S4 Correction | FR7, FR23, FR63, FR76, FR79, FR91a, NFR17a | Flow 4, Association Review, Conversation Detail, status/progress patterns | Epic 2, Epic 3 | Predecessor/supersession display, correcting/delayed progress, disabled AI-action reasons, safe retry/escalation, focus after correction. |
| S6 Outbound approval | FR41-FR42, FR45, FR47-FR50, FR48a-FR48d | AI Action Review, approval controls, evidence/provenance patterns | Epic 6 | Sender-authority class display, recipient preview, authenticity posture, approval/revision/cancel states, fail-closed denied send. |
| S7 Cross-surface attribution | FR81a-FR86, FR85 | Command Surface Reference, audit timeline, actor badges | Epic 5, cross-cutting Epic 1 | UI/CLI/MCP/source-origin display, equivalent error/outcome mapping, audit attribution, no bypass affordance. |
| S8 Operational dashboards | FR67, FR69-FR75, FR78-FR79, NFR42a, NFR48 | Operational Queues / dashboard states | Epic 8, Epic 7 | Queue depth/age/status/freshness, stable filters, degraded dependency state, role-owned triage actions, no infinite scroll. |
| S9 Compliance investigation | FR54-FR56, FR57, FR60, NFR49a, NFR50a | Audit Investigation | Epic 9 | Search axes, decision reconstruction, redacted rows with escalation path, audit completeness evidence, replay exclusion. |
| S10 Admin queue operations | FR67, FR69-FR75g, FR78, NFR15a | Operational Queues, Tenant Configuration | Epic 7, Epic 8 | Claim/assign/filter/sort, two-person-rule surfaces, policy conflict resolution, safe operation feedback, admin read vs per-project authority boundary. |

## Addendum Validation Notes

M1/M2 UX stories must explicitly revalidate against these addendum details when they are in scope:

- Sender-authority mapping and inbound authenticity posture for S6.
- Tenant Policy Schema, security-sensitive knobs, and two-person-rule confirmation for S10 and S5-adjacent configuration.
- Idempotency windows and stable operation status for retry, duplicate, command, and workflow surfaces.
- Replay isolation and `replay_run_id` treatment for S9.
- Risk classifier reason display and approval fatigue observables for S6, S8, and S10.
- Operating baselines, SLO/error-budget language, and freshness timestamps for S8/S10.

## Story Acceptance Context

Implementation stories that consume this gate should not paste this artifact wholesale. They should cite the relevant row and include the concrete surface states needed for that story.

Minimum acceptance context per story:

- The surface shows stable status enums, not inferred status from counts or colors alone.
- Disabled actions are either focusable with `aria-disabled="true"` and an announced reason, or paired with an adjacent focusable explanation.
- Redacted rows explain restriction and offer safe escalation without revealing hidden resources.
- Success, delayed, blocked, retryable, and terminal outcomes move focus to the appropriate status or error summary.
- English and French text exists for user-visible states, next actions, and disabled-action reasons.

