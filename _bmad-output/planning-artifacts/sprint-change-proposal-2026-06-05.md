---
title: Sprint Change Proposal - Implementation Readiness Blocker Cleanup
project: Chatbot
date: 2026-06-05
status: approved
mode: Batch
source_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-04.md
scope_classification: Moderate
recommended_approach: Direct Adjustment
owner: Jerome
prepared_at: 2026-06-05T11:54:51+02:00
approved_by: Jerome
approved_at: 2026-06-05T12:12:05+02:00
implementation_state: planning-artifacts-applied
---

# Sprint Change Proposal - Implementation Readiness Blocker Cleanup

## 1. Issue Summary

The June 4 implementation readiness assessment remains `NEEDS WORK` after the PRD/UX discovery and traceability blocker was corrected. The confirmed PRD now exposes 111 FR identifiers and 77 NFR identifiers, and Epic FR coverage is 100.0%. The remaining readiness blockers are not missing product requirements; they are backlog structure, production binding ownership, and M1/M2 UX elaboration before sprint assignment.

Triggering artifact:

- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-04.md`

Blocking issues:

1. Story 1.1 is too large for a single implementation story.
2. Story 2.8 references hosted Dapr Workflow production binding, but ownership is not explicit in the epic/story plan.
3. Story 7.27 combines command allowlist v1 governance and full lifecycle state matrix completion.
4. M1/M2 UX surfaces need explicit PRD-surface-to-UX-surface mapping before those increment stories are assigned.

This is a planning/backlog correction, not a code rollback. Current implementation artifacts show Story 1.1, Story 2.8, and Story 7.27 as done, so approved edits should preserve historical traceability and avoid destructive renumbering of completed implementation evidence.

## 2. Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] Done | Trigger is the June 4 readiness report, not a single failed implementation story. The affected story records are 1.1, 2.8, 7.27, plus M1/M2 surface stories. |
| 1.2 Core problem | [x] Done | Issue type: planning/backlog structure and ownership gap discovered during readiness assessment. |
| 1.3 Evidence | [x] Done | Evidence comes from the readiness report, `epics.md`, architecture Dapr Workflow notes, UX `DESIGN.md`/`EXPERIENCE.md`, and implementation artifacts. |
| 2.1 Current epic impact | [x] Done | Epic 1 and Epic 7 need story splitting. Epic 2 needs an explicit production binding owner or scope downgrade. |
| 2.2 Epic-level changes | [x] Done | No new product epic is required. Add child stories / ownership story and pre-increment UX gate. |
| 2.3 Remaining epics | [x] Done | Epic 6, 8, and 9 are impacted by M1/M2 UX elaboration; Epic 8 is the recommended home for production Dapr Workflow binding validation. |
| 2.4 Future epic validity | [x] Done | No planned epic is obsolete. |
| 2.5 Priority/order | [x] Done | Do not resequence M0. Add gates before M1/M2 sprint assignment and before production saga claims. |
| 3.1 PRD conflicts | [x] Done | No PRD scope change required; PRD is complete enough. |
| 3.2 Architecture conflicts | [x] Done | Architecture already says hosted Dapr Workflow binding is pending before production saga claims; backlog must name the owner. |
| 3.3 UI/UX conflicts | [x] Done | UX spine is sufficient for M0 but insufficiently mapped for S4, S6, S7, S8/S10, and S9 assignment. |
| 3.4 Other artifacts | [x] Done | `sprint-status.yaml` must be updated only after approval if child stories are added or status aliases are introduced. |
| 4.1 Direct Adjustment | [x] Viable | Best path: split/annotate stories, add binding ownership, and add UX elaboration gates. Effort medium; risk low to medium. |
| 4.2 Rollback | [N/A] Skip | No implementation rollback is useful; the issue is planning readiness. |
| 4.3 MVP Review | [N/A] Skip | MVP scope remains achievable; no scope reduction recommended. |
| 4.4 Recommendation | [x] Done | Direct Adjustment with moderate backlog reorganization. |
| 5.1-5.5 Proposal components | [x] Done | This document contains issue summary, impact analysis, proposed edits, and handoff. |
| 6.1-6.2 Final review | [x] Done | Proposal is internally consistent with the readiness report and current artifacts. |
| 6.3 User approval | [!] Action-needed | Pending Jerome review/approval. |
| 6.4 Sprint-status update | [!] Action-needed | Apply only after approval. |
| 6.5 Handoff plan | [x] Done | Product Owner / Developer for backlog edits; UX designer for surface elaboration; Architect/Developer for Dapr Workflow binding ownership. |

## 3. Impact Analysis

### Epic Impact

| Epic | Impact | Required change |
| --- | --- | --- |
| Epic 1 - First Safe Governed Action & Command Spine | Story 1.1 is oversized and already contains a split hint. | Replace the monolithic implementation story with 1.1a-1.1d child stories or add approved planning aliases if preserving completed story IDs. |
| Epic 2 - Email Intake & Project Association | Story 2.8 owns correction propagation but references hosted Dapr Workflow production readiness without visible owner. | Add a production binding owner story or downgrade 2.8 wording to coordinator seam only until the owner story lands. |
| Epic 6 - Outbound Communication & Inbound Authenticity | S6 outbound approval requires M1 UX elaboration. | Add PRD-to-UX mapping and story-specific acceptance context before assignment. |
| Epic 7 - Tenant Administration & Governance Policy | Story 7.27 combines allowlist v1 and lifecycle matrix completion. Epic 7 also consumes S5/S8/S10 UX detail. | Split 7.27 into 7.27a/7.27b and add UX elaboration gate for admin/queue surfaces. |
| Epic 8 - Operational Dashboards & Observability | Good home for production Dapr Workflow binding and dashboard UX mapping. | Add or identify a story for hosted Dapr Workflow production binding and saga validation. |
| Epic 9 - Tamper-Evident Audit, Compliance Investigation & Recovery | S9 compliance investigation needs UX elaboration and addendum validation before assignment. | Add PRD-to-UX mapping and acceptance context before sprinting S9 stories. |

### Story Impact

| Story | Current issue | Proposed result |
| --- | --- | --- |
| 1.1 | Too broad: scaffold, root config, submodule policy, Aspire/Dapr topology, CI, tests, and build verification. | Split into 1.1a-1.1d or record child-story aliases while retaining historical 1.1 implementation evidence. |
| 2.8 | Production hosted Dapr Workflow binding is required before production saga claims, but no owner story is visible. | Add Story 8.6 for production binding and validation, and update 2.8 to explicitly defer production saga claims until 8.6 is complete. |
| 7.27 | Two independently testable responsibilities in one story. | Split into 7.27a command allowlist v1 governance and 7.27b lifecycle matrix / cross-actor isolation completion. |
| M1/M2 UX surface stories | UX spine exists, but mapping and addendum validation are not assignment-ready. | Add an M1/M2 UX surface elaboration addendum and gate affected stories until it exists. |

### Technical Impact

- No code rollback is recommended.
- No PRD scope change is recommended.
- Production Dapr Workflow binding must be owned by ChatBot platform/operations implementation, not by EventStore or sibling bounded contexts.
- Story 2.8 may keep the coordinator/activity seam and aggregate lifecycle behavior, but it must not claim production saga readiness until the hosted runtime binding and production validation story is complete.
- If the backlog has already been implemented, apply these changes as planning corrections / aliases rather than rewriting completed evidence.

## 4. Recommended Approach

Selected approach: Direct Adjustment.

Scope classification: Moderate.

Rationale:

- The product requirements and epic-level FR coverage are intact.
- The fixes are backlog, ownership, and UX acceptance-context changes.
- Renumbering completed implementation artifacts would create avoidable traceability churn, so suffix child IDs are recommended.
- The Dapr Workflow production binding should be explicit because it gates production saga claims and failure-mode evidence.
- M1/M2 UX elaboration should be a pre-increment gate, not an M0 blocker.

Effort estimate: Medium.

Risk level: Low to medium. Risk is mainly traceability drift if completed story artifacts are renumbered destructively.

Timeline impact: No M0 scope change. M1/M2 planning must include the UX elaboration gate before story assignment.

## 5. Detailed Change Proposals

### 5.1 Split Story 1.1

Artifact: `_bmad-output/planning-artifacts/epics.md`

Current section: `### Story 1.1: Scaffold the buildable Hexalith.ChatBot module`

OLD:

```text
### Story 1.1: Scaffold the buildable Hexalith.ChatBot module

Planning note: if estimation exceeds one sprint-sized story, split into (1.1a) solution scaffold + root config + EventStore root submodule + build-green, and (1.1b) Aspire/DAPR topology + CI workflows.
```

NEW:

```text
### Story 1.1: Scaffold the buildable Hexalith.ChatBot module

Planning status: parent planning container only. Do not assign this parent story directly to a sprint. Assign the child stories below. Existing implementation evidence that references Story 1.1 remains historical evidence for the combined scaffold slice.

#### Story 1.1a: Solution scaffold, root config, and build-green baseline

As a platform engineer,
I want the root ChatBot solution scaffold and build policy established,
So that the module has a convention-correct, buildable foundation before runtime topology work begins.

Acceptance Criteria:
- `.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, and `nuget.config` exist at the repository root.
- Required `Contracts`, `Client`, `Server`, `Aspire`, `AppHost`, `ServiceDefaults`, and `Testing` projects exist with strict dependency direction.
- Matching test projects exist for the scaffold, architecture, conformance, and testing baselines.
- `dotnet restore Hexalith.ChatBot.slnx` and `dotnet build Hexalith.ChatBot.slnx --no-restore` pass with warnings as errors.

#### Story 1.1b: Root-level EventStore submodule and sibling dependency resolution

As a platform engineer,
I want EventStore and sibling references resolved through root-level submodules only,
So that the ChatBot module builds against the Hexalith ecosystem without nested submodule drift.

Acceptance Criteria:
- EventStore is declared only in the repository-root `.gitmodules`.
- Setup and CI use non-recursive root-level initialization, for example `git submodule update --init`.
- No script, workflow, or setup doc introduces recursive submodule initialization.
- MSBuild root-detection resolves required sibling references without hardcoded nested paths.

#### Story 1.1c: Aspire/DAPR topology and local run verification

As a platform engineer,
I want the ChatBot AppHost and DAPR topology wired with production-safe component names,
So that the module can run locally and preserve production deny-by-default assumptions.

Acceptance Criteria:
- AppId `chatbot`, state stores `statestore` and `chatbot-statestore`, pub/sub component `chatbot-pubsub`, topic `chatbot.events`, and deadletter `deadletter.chatbot.events` are declared consistently.
- Local mTLS-off development uses `accesscontrol.local.yaml`; production references `accesscontrol.yaml` with deny-by-default access control.
- Required sibling and Keycloak resources are represented with `WaitFor` health where applicable.
- `aspire run` is verified or blocked prerequisites are recorded exactly.

#### Story 1.1d: CI/release skeleton and scaffold quality gates

As a platform engineer,
I want CI, release skeleton, and scaffold guardrails in place,
So that future story work cannot silently weaken build, dependency, or submodule policy.

Acceptance Criteria:
- CI and release workflow skeletons exist and do not initialize nested submodules.
- Tests reject inline package versions in project files.
- Architecture/conformance tests verify dependency direction, DAPR naming, access-control posture, and adapter boundary rules.
- Validation evidence records the exact build/test lane and any sandbox-specific limitations.
```

Rationale: This preserves Story 1.1 as a historical parent while making future sprint assignment implementable.

Sprint-status impact after approval:

- Add `1-1a-solution-scaffold-root-config-and-build-green-baseline`, `1-1b-root-level-eventstore-submodule-and-sibling-dependency-resolution`, `1-1c-aspire-dapr-topology-and-local-run-verification`, and `1-1d-ci-release-skeleton-and-scaffold-quality-gates` as child/planning aliases.
- If preserving completed evidence, mark aliases `done` with `superseded_by_parent: 1-1...` or equivalent project-local convention rather than deleting `1-1`.

### 5.2 Assign Story 2.8 Hosted Dapr Workflow Production Binding Ownership

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/architecture.md`

Current Story 2.8 wording:

```text
... correction-propagation coordinator/activity seam coordinates invalidation, with hosted Dapr Workflow runtime binding required before production saga claims (FR91, FR91a).
```

NEW in Story 2.8:

```text
Ownership note: Story 2.8 owns the correction-propagation contract, aggregate lifecycle, coordinator/activity seam, per-store acknowledgements, user-visible correcting/delayed states, and fail-closed dependency readiness. It does not by itself claim production saga readiness. Hosted Dapr Workflow runtime binding, production AppHost/container wiring, retry/backoff policy, workflow status management, production observability, and saga-failure validation are owned by Story 8.6 before any production saga orchestration claim is made.
```

Add under Epic 8:

```text
### Story 8.6: Hosted Dapr Workflow production binding and saga readiness validation

As a platform/operations engineer,
I want ChatBot's correction-propagation coordinator bound to the hosted Dapr Workflow runtime with production validation,
So that production saga orchestration claims are backed by runtime wiring, observability, retry behavior, and failure-mode evidence.

Acceptance Criteria:

Given the Story 2.8 correction-propagation coordinator/activity seam
When the production AppHost/container topology is configured
Then the hosted Dapr Workflow runtime is registered, health-checked, and bound to ChatBot through explicit DI and DAPR component configuration.

Given a correction propagation workflow instance
When it starts, retries, completes, delays, or fails in the hosted runtime
Then workflow instance id, tenant id, correction id, source version, status, retry count, last failure code, and correlation id are observable through metadata-only telemetry and operation status.

Given workflow runtime, state store, pub/sub, audit writer, or projection dependency outage
When correction propagation admission or execution depends on that dependency
Then the failure is scoped to the affected tenant/workflow item where possible, fails closed before false success, and emits the existing safe operator alert/P2 signal.

Given a production saga claim is made for correction propagation
When validation evidence is reviewed
Then it includes local AppHost smoke evidence, production-config lint/static checks, retry/idempotency evidence, delayed-state evidence, and no direct mutation of Projects, Conversations, Folders, Memories, or EventStore internals.
```

Rationale: Story 8.6 gives the production binding a clear ChatBot owner and prevents Story 2.8 from silently claiming more than the implemented seam can prove.

### 5.3 Split Story 7.27

Artifact: `_bmad-output/planning-artifacts/epics.md`

Current section: `### Story 7.27: Command allowlist v1 and full lifecycle completion`

OLD:

```text
### Story 7.27: Command allowlist v1 and full lifecycle completion

As a security engineer,
I want the versioned command allowlist v1 under change control and the full lifecycle state matrix completed,
So that M1 governance breadth lands without weakening the M0 safety floor.
```

NEW:

```text
### Story 7.27: Command allowlist v1 and full lifecycle completion

Planning status: parent planning container only. Do not assign this parent story directly to a sprint. Assign 7.27a and 7.27b separately. Existing implementation evidence that references Story 7.27 remains historical evidence for the combined governance/lifecycle slice.

#### Story 7.27a: Command allowlist v1 governance and change-control

As a security engineer,
I want the versioned AI-action command allowlist promoted from M0 to v1 under explicit change control,
So that AI-invocable governed commands can expand without weakening fail-closed allowlist enforcement.

Acceptance Criteria:
- v1 allowlist membership is versioned independently from the M0 set.
- Every v1 allowlisted command has effect surface, authority class, default risk, and idempotency-contract metadata.
- Adding/removing a command or changing default risk requires a version increment.
- Security-engineer sign-off is recorded in the PRD decision log.
- Enforcement remains fail-closed at dispatcher, aggregate, and DI seams.

#### Story 7.27b: Lifecycle state matrix completion and cross-actor isolation proof

As a security engineer,
I want the canonical workflow lifecycle state matrix completed and proven across service-client, CLI, and MCP actor classes,
So that every surface follows the same legal state transitions and isolation controls.

Acceptance Criteria:
- `Skipped` terminal triggers for duplicate suppression and out-of-scope mailbox handling are guard-mapped to valid terminal transitions.
- Every shipped command/guard mapping resolves to a valid transition in the canonical matrix.
- Invalid transitions are rejected before mutation and recorded with actor, reason, and correlation context.
- Disabled, quarantined, and rate-limited service-client, CLI-class, and MCP-class actors are denied with stable reason codes.
- UI/CLI/MCP parity holds for representative isolation and lifecycle outcomes.
```

Rationale: The allowlist and lifecycle work have different reviewers, code seams, and regression risks. Splitting keeps each acceptance set independently testable.

Sprint-status impact after approval:

- Add `7-27a-command-allowlist-v1-governance-and-change-control`.
- Add `7-27b-lifecycle-state-matrix-completion-and-cross-actor-isolation-proof`.
- Preserve `7-27-command-allowlist-v1-and-full-lifecycle-completion` as the historical parent if already implemented.

### 5.4 Add M1/M2 UX Surface Elaboration Gate

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- Optional new artifact: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md`

Current planning guidance:

```text
Before assigning their increments, elaborate: S4 correction (Epics 2/3), S6 outbound approval (Epic 6), S7 cross-surface attribution (Epics 1/5), S8/S10 operations (Epic 8), and S9 compliance investigation (Epic 9).
```

NEW:

```text
M1/M2 UX surface elaboration gate:
Before any M1/M2 story that implements S4, S6, S7, S8, S9, or S10 is assigned to a sprint, the UX package must contain a PRD-surface-to-UX-surface map, addendum validation notes, and surface-specific acceptance context for information architecture, states, interactions, accessibility, responsive behavior, localization, and redaction-safe failure handling.
```

Required mapping table:

| Surface | Primary PRD / addendum anchors | UX anchor | Affected epics/stories | Required elaboration |
| --- | --- | --- | --- | --- |
| S4 Correction | FR7, FR23, FR63, FR76, FR79, FR91a, NFR17a | Flow 4, Association Review, Conversation Detail, status/progress patterns | Epic 2, Epic 3 | Predecessor/supersession display, correcting/delayed progress, disabled AI-action reasons, safe retry/escalation, focus after correction. |
| S6 Outbound approval | FR41-FR42, FR45, FR47-FR50, FR48a-FR48d | AI Action Review, approval controls, evidence/provenance patterns | Epic 6 | Sender-authority class display, recipient preview, authenticity posture, approval/revision/cancel states, fail-closed denied send. |
| S7 Cross-surface attribution | FR81a-FR86, FR85 | Command Surface Reference, audit timeline, actor badges | Epic 5, cross-cutting Epic 1 | UI/CLI/MCP/source-origin display, equivalent error/outcome mapping, audit attribution, no bypass affordance. |
| S8 Operational dashboards | FR67, FR69-FR75, FR78-FR79, NFR42a, NFR48 | Operational Queues / dashboard states | Epic 8, Epic 7 | Queue depth/age/status/freshness, stable filters, degraded dependency state, role-owned triage actions, no infinite scroll. |
| S9 Compliance investigation | FR54-FR56, FR57, FR60, NFR49a, NFR50a | Audit Investigation | Epic 9 | Search axes, decision reconstruction, redacted rows with escalation path, audit completeness evidence, replay exclusion. |
| S10 Admin queue operations | FR67, FR69-FR75g, FR78, NFR15a | Operational Queues, Tenant Configuration | Epic 7, Epic 8 | Claim/assign/filter/sort, two-person-rule surfaces, policy conflict resolution, safe operation feedback, admin read vs per-project authority boundary. |

UX metadata update:

```text
Add `prd addendum: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` to the UX source inventory and revalidate M1/M2 UX against sender-authority mapping, Tenant Policy Schema, idempotency windows, replay isolation, risk classifier, and operating baselines before assignment.
```

Rationale: The current UX bundle is intentionally non-wireframe-based and good enough for M0. M1/M2 assignment needs explicit mapping and addendum-aware acceptance detail.

## 6. Implementation Handoff

Scope classification: Moderate.

Recommended routing:

| Recipient | Responsibility |
| --- | --- |
| Product Owner / Developer agent | Apply approved `epics.md` child-story edits, add Story 8.6, and update sprint-status bookkeeping without deleting historical completed evidence. |
| UX Designer / UX workflow | Create the M1/M2 UX surface elaboration artifact, update UX source metadata to include `addendum.md`, and revalidate S4/S6/S7/S8/S9/S10. |
| Architect / Developer agent | Confirm Story 8.6 production Dapr Workflow binding acceptance criteria and ensure it does not shift ownership into EventStore or sibling bounded contexts. |
| Product Manager | Decide whether Epic 7 remains one epic with sub-epic milestones or is split later; this proposal does not require that broader split. |

Success criteria:

- `epics.md` no longer assigns parent Story 1.1 or parent Story 7.27 directly as implementation-sized sprint work.
- Story 2.8 explicitly states its production saga claim is blocked until the hosted Dapr Workflow binding owner story is complete.
- A visible owner story exists for hosted Dapr Workflow production binding and saga readiness validation.
- M1/M2 UX surfaces have PRD/addendum-to-UX mapping before sprint assignment.
- `sprint-status.yaml` reflects approved story aliases/additions without erasing completed historical artifacts.
- A rerun of readiness assessment should move from `NEEDS WORK` to at least `M0-ready` if no new blockers are found.

## 7. Approval and Routing

Approval status: approved by Jerome on 2026-06-05T12:12:05+02:00.

Applied artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/m1-m2-surface-elaboration.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Handoff completion:

- Product Owner / Developer handoff: complete for planning edits and sprint-status bookkeeping.
- UX handoff: complete for the approved M1/M2 surface elaboration gate artifact.
- Architect / Developer handoff: Story 8.6 now owns hosted Dapr Workflow production binding and saga readiness validation.

Recommended next action after approval:

1. Rerun implementation readiness assessment.
2. Create a Story 8.6 implementation artifact when the production binding is scheduled.
3. Keep parent Story 1.1 and 7.27 artifacts as historical evidence; use the child aliases for future reporting.
