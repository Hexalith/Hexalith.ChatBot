---
title: Sprint Change Proposal - Chatbot Readiness Corrective Pass
project: Chatbot
date: 2026-05-30
status: applied
source_report: _bmad-output/planning-artifacts/implementation-readiness-report-2026-05-30.md
scope_classification: Moderate
recommended_approach: Direct Adjustment
owner: Jerome
approved_by: Jerome
approved_at: 2026-05-30
applied_at: 2026-05-30
---

# Sprint Change Proposal - Chatbot Readiness Corrective Pass

## 1. Issue Summary

The 2026-05-30 implementation-readiness assessment concluded that the Chatbot planning package is **NEEDS WORK before full implementation**. The PRD, UX, architecture, and epic coverage are aligned, but `epics.md` contains story-quality defects that make several stories too large or dependent on later stories.

The triggering evidence is the latest readiness report:

- 2 critical story-sizing violations: Story 3.2 and Story 7.7.
- 8 major epic/story quality issues.
- 4 minor concerns.
- 1 UX traceability watch item.

The issue type is planning/backlog correction, not implementation failure. No code rollback is required because no implementation artifacts were found under `_bmad-output/implementation-artifacts`.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Required change |
| --- | --- | --- |
| Epic 1: Walking Skeleton & Governed Command Spine | Major story-sizing and value-framing issue. | Reframe the epic around the first governed action and split Stories 1.10-1.12 before estimation. |
| Epic 2: Email Intake & Project Association | Major forward-coupling issue in Story 2.8. | Keep M0 correction propagation focused on current derived stores. Move AI proposal invalidation and vector reindexing into later epics. |
| Epic 3: Project Conversation Context, Files & Attachments | Critical sizing issue in Story 3.2; minor future-coupling issue in Story 3.8. | Split Story 3.2 into seven independent rendering stories. Rephrase Story 3.8 as an inspectable context-package contract, not AI consumption. |
| Epic 4: Governed AI Action Mediation | Needs extension for correction invalidating AI proposals and actual AI-context consumption. | Add a story or acceptance criteria covering AI proposal invalidation and consumption of authorized context packages. |
| Epic 6: Outbound Communication & Inbound Authenticity | Story order defect. | Move sender-authority classification before outbound draft creation. |
| Epic 7: Tenant Administration & Governance Policy | Critical sizing issue in Story 7.7. | Split Story 7.7 into the 15 FR74 subject/action stories and renumber Story 7.8. |
| Epic 8: Operational Dashboards & Observability | Minor watch item for Story 8.2 breadth. | Keep as-is until estimation; split into metrics, SLO publication, and alerting if it exceeds sprint size. |
| Epic 9: Tamper-Evident Audit, Compliance Investigation & Recovery | Major sizing in Stories 9.6 and 9.7; needs M2 vector-reindex extension. | Split 9.6 and 9.7, and add explicit correction-driven vector reindexing. |

No epic is invalidated. No new top-level epic is required.

### Story Impact

Stories requiring immediate changes before sprint assignment:

- Story 3.2: split into 7 stories.
- Story 7.7: split into 15 stories.
- Story 6.1 / 6.2: reorder.

Stories requiring pre-estimation decomposition:

- Story 1.10: split into architecture tests, differential-conformance harness, cross-tenant isolation harness, and fixture/evaluation scaffold.
- Story 1.11: split into visual/token foundation, shared component primitives, interaction guardrails, and responsive/touch foundation.
- Story 1.12: split into accessibility/focus floor, live-region/reduced-motion behavior, localization infrastructure, and redaction-safe off-surface affordances.
- Story 2.8: remove future-coupled acceptance from M0 and add extension stories in Epic 4 / Epic 9.
- Story 9.6: split into data-class inventory/retention policy, export workflow, deletion/erasure workflow, and consent/lawful-basis metadata.
- Story 9.7: split into continuity drill, projection rebuild validation, and scoped outage degradation.

Watch items:

- Story 3.8: keep as an enabling context package and inspectability story in Epic 3; actual AI consumption belongs in Epic 4.
- Story 8.2: split if estimation shows it exceeds one sprint-sized story.
- PRD S4/S6/S7 traceability: add explicit tags in affected stories.

### Artifact Conflicts

| Artifact | Conflict | Required action |
| --- | --- | --- |
| PRD | None. The PRD already contains decomposition guidance for FR22 and FR74. | No PRD scope change. |
| Architecture | None. The architecture already supports the command gateway, correction propagation, UX surfaces, and safety-floor testing. | No architecture change required. Keep architecture consulted for Epic 1 split details. |
| UX | No blocking conflict. UX covers S4/S6/S7 behavior under broader surfaces. | Add explicit S4/S6/S7 trace tags in `epics.md`. |
| Epics | Blocking story-sizing and ordering defects. | Apply the detailed story changes below. |
| Sprint status | No sprint-status artifact was found. | If a sprint-status file is introduced later, update it after approval. |

### Technical Impact

No code or infrastructure implementation changes are required by this proposal. The direct technical impact is on planning artifacts and future implementation sequencing. The change reduces implementation risk by making acceptance criteria independently testable and preventing future-epic dependencies inside current-increment stories.

## 3. Recommended Approach

Use **Direct Adjustment**.

Rationale:

- The PRD, architecture, and UX are aligned and do not require an MVP reset.
- The defects are isolated to epic/story structure.
- There is no completed implementation to roll back.
- The corrections preserve M0 -> M1 -> M2 ordering while increasing story count and improving sprint readiness.

Effort estimate: 1-2 planning days to update `epics.md`, adjust story numbering/references, and rerun the epic-quality portion of readiness validation.

Risk: Low to medium. The main risk is accidental traceability loss while renumbering stories. Mitigation: keep FR/NFR IDs on every new story and rerun readiness validation after the edit.

Timeline impact: no MVP scope expansion. Sprint planning should wait for the corrected `epics.md` before assigning Stories 3.2, 7.7, 1.10-1.12, 2.8, 6.1, 9.6, or 9.7.

## 4. Detailed Change Proposals

### Stories - Epic 1

#### Story: Epic 1 framing

Section: Epic title and description.

OLD:

```markdown
## Epic 1: Walking Skeleton & Governed Command Spine
```

NEW:

```markdown
## Epic 1: First Safe Governed Action & Command Spine

Deliver the architecture-mandated safety floor through one user-observable governed UI command. Each foundation story must either unblock that first governed action or add a mechanical guardrail required to prove it is safe.
```

Rationale: Preserves the required technical foundation while tying the epic to a user-observable outcome.

#### Story: 1.10 Mechanical parity and isolation enforcement harnesses

Section: Story split.

OLD:

```markdown
### Story 1.10: Mechanical parity and isolation enforcement harnesses

As an architecture owner,
I want NetArchTest, a differential-conformance harness, and a cross-tenant isolation harness wired from day one,
So that parity-by-construction and zero cross-tenant leakage are enforced mechanically, not by review.
```

NEW:

```markdown
### Story 1.10: Architecture dependency fitness tests

As an architecture owner,
I want dependency-direction and adapter-boundary fitness tests,
So that FR81a pipeline-stage replication is mechanically blocked.

### Story 1.11: Differential-conformance harness

As a platform tester,
I want equivalent semantic intents submitted through UI and thin CLI/MCP shims,
So that parity failures are detected before real CLI/MCP surfaces ship.

### Story 1.12: Cross-tenant isolation harness

As a security owner,
I want negative tests across the nine actor types,
So that every actor fails closed with zero leakage.

### Story 1.13: Tenant-scoped fixture and evaluation scaffold

As a QA owner,
I want tenant-scoped fixtures, sandbox data, and evaluation-dataset partitions,
So that later calibration and conformance tests are safe and repeatable.
```

Rationale: Separates four independent test harness deliverables while preserving FR86, NFR11, FR92, and FR93 coverage.

#### Story: 1.11 UX foundation

Section: Story split.

OLD:

```markdown
### Story 1.11: UX foundation -- design system, shared components, and interaction primitives
```

NEW:

```markdown
### Story 1.14: Visual inheritance and semantic token foundation
### Story 1.15: Shared governed component primitives
### Story 1.16: Interaction guardrails and streaming stop/cancel behavior
### Story 1.17: Responsive and touch foundation
```

Rationale: Separates visual tokens, component primitives, interaction rules, and responsive behavior into independently testable UI foundation work.

#### Story: 1.12 Accessibility floor and English/French localization

Section: Story split.

OLD:

```markdown
### Story 1.12: Accessibility floor and English/French localization
```

NEW:

```markdown
### Story 1.18: Accessibility and focus-management floor
### Story 1.19: Live-region and reduced-motion behavior
### Story 1.20: English/French localization infrastructure
### Story 1.21: Redaction-safe off-surface affordances and recovery patterns
```

Rationale: Separates accessibility behavior, feedback behavior, localization, and redaction/recovery guarantees.

### Stories - Epic 2

#### Story: 2.8 Correction propagation contract

Section: Acceptance Criteria.

OLD:

```markdown
Then every derived store referencing the original association (candidate ranking, evidence snapshot, AI-action proposals that consumed the misassigned context, queue projections; vector entries in M2) is invalidated and rebuilt...

Given an item in Correcting
When an AI action is requested against it
Then the action is blocked until all derived stores acknowledge invalidation...
```

NEW:

```markdown
Then every M0 derived store referencing the original association (candidate ranking, evidence snapshot, queue projections) is invalidated and rebuilt...

Given an item in Correcting
When any project context read or command preparation references the corrected association
Then it returns the correcting state with progress, estimated completion, and safe next action until all M0 stores acknowledge invalidation.
```

Add extension stories:

```markdown
### Story 4.9: Correction invalidates AI action proposals

As a project owner,
I want AI action proposals that consumed misassociated context invalidated by correction,
So that AI mediation never acts on stale project evidence.

### Story 9.6: Correction-driven vector reindexing

As a security owner,
I want vector entries derived from corrected associations invalidated and reindexed in M2,
So that derived AI stores remain correct and tenant-isolated.
```

Rationale: Keeps Epic 2 independently completable in M0 and moves Epic 4/M2-specific behavior to the epics that own it.

### Stories - Epic 3

#### Story: 3.2 Conversation item rendering across the seven concerns

Section: Story split.

OLD:

```markdown
### Story 3.2: Conversation item rendering across the seven concerns

Then it represents (1) associated email, (2) participants, (3) attachments, (4) decisions, (5) approvals, (6) failures, and (7) AI outcomes...
```

NEW:

```markdown
### Story 3.2: Associated-email rendering in the conversation stream
### Story 3.3: Participant rendering in the conversation stream
### Story 3.4: Attachment rendering in the conversation stream
### Story 3.5: Association and correction decision rendering
### Story 3.6: Approval event rendering
### Story 3.7: Failure, retry, and blocked-state rendering
### Story 3.8: AI outcome rendering
```

Each new story inherits this acceptance floor:

```markdown
Given the concern-specific conversation item
When it renders on S1
Then it shows actor attribution, actor-type label in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status, reduced-motion behavior, and WCAG 2.2 AA compliance.
```

Renumber current Story 3.3 and later stories after the split.

Rationale: Follows PRD FR22 decomposition guidance and makes each rendering concern independently estimable and testable.

#### Story: 3.8 Scoped AI-context packaging from authorized files

Section: Acceptance Criteria.

OLD:

```markdown
Given an AI action needing file context
When the context package is built
Then files are included only through explicit authorization, policy checks, and auditable context packaging.
```

NEW:

```markdown
Given an authorized project file set
When an AI-context eligibility package is produced in Epic 3
Then the package manifest can be inspected without invoking a model or tool, and includes tenant ID, project ID, source evidence references, policy snapshot ID, redaction decision, retention class, provider-reuse setting, and excluded-file reasons.

Given Epic 4 AI mediation
When an AI action needs file context
Then it consumes only an authorized, current context package produced by this contract.
```

Rationale: Keeps Epic 3 as an enabling context-package story and moves actual AI action consumption into Epic 4.

### Stories - Epic 4

#### Story: Add Story 4.9

Section: New story.

NEW:

```markdown
### Story 4.9: Correction invalidates AI action proposals

As a project owner,
I want AI proposals that consumed corrected project context invalidated or blocked,
So that approval and execution never use stale evidence.

Acceptance Criteria:

Given an AI action proposal was built from association evidence
When that association is corrected
Then the proposal is marked invalidated with the correction ID, cannot be approved or executed, and links to the corrected evidence state.

Given a new AI proposal is requested after correction
When all M0/M1 invalidation acknowledgements are complete
Then the proposal uses the corrected evidence snapshot and records the correction lineage in audit.
```

Rationale: Moves AI-specific correction behavior out of Epic 2 and into AI mediation ownership.

### Stories - Epic 6

#### Stories: 6.1 and 6.2 ordering

Section: Story order.

OLD:

```markdown
### Story 6.1: Outbound draft creation within authority
### Story 6.2: Sender-authority classes and M365 mapping
```

NEW:

```markdown
### Story 6.1: Sender-authority classes and M365 mapping
### Story 6.2: Outbound draft creation within authority
```

Update the draft story to depend on the authority classifier:

```markdown
Given the sender-authority classifier from Story 6.1 has resolved `draft-only`
When an authorized contributor creates an outbound draft
Then the draft is created within the approved project and sender authority and does not leave ChatBot.
```

Rationale: Draft creation cannot be validated until authority classes and mapping exist.

### Stories - Epic 7

#### Story: 7.7 Source disable / quarantine / rate-limit controls

Section: Story split.

OLD:

```markdown
### Story 7.7: Source disable / quarantine / rate-limit controls

Given a mailbox source / service client / AI actor / command capability / outbound channel producing unsafe/invalid/excessive/policy-violating activity
When an admin acts
Then they can disable, quarantine, or rate-limit it...
```

NEW:

```markdown
### Story 7.7: Disable mailbox source
### Story 7.8: Quarantine mailbox source
### Story 7.9: Rate-limit mailbox source
### Story 7.10: Disable service client
### Story 7.11: Quarantine service client
### Story 7.12: Rate-limit service client
### Story 7.13: Disable AI actor
### Story 7.14: Quarantine AI actor
### Story 7.15: Rate-limit AI actor
### Story 7.16: Disable command capability
### Story 7.17: Quarantine command capability
### Story 7.18: Rate-limit command capability
### Story 7.19: Disable outbound channel
### Story 7.20: Quarantine outbound channel
### Story 7.21: Rate-limit outbound channel
```

Shared acceptance floor:

```markdown
Given an authorized administrator with the required scope
When the subject/action control is changed
Then the operation records actor, scope, subject, reason, old state, new state, policy snapshot, and timestamp.

Given the action is disable or quarantine
When requested
Then it follows the FR75d two-person rule and cannot be performed by service clients or AI actors.

Given the action is rate-limit
When configured
Then it is a standard policy mutation bounded by the Tenant Policy Schema and protects unrelated tenants/sources from degradation where isolation is possible.
```

Renumber current Story 7.8 to Story 7.22.

Rationale: FR74 explicitly decomposes into five subject classes times three actions. This makes each control independently implementable and auditable.

### Stories - Epic 8

#### Story: 8.2 SLOs, metrics, and alerting

Section: Watch item.

OLD:

```markdown
### Story 8.2: SLOs, metrics, and alerting
```

NEW, if estimation exceeds one sprint-sized story:

```markdown
### Story 8.2: Operational telemetry emission
### Story 8.3: SLO publication and error-budget view
### Story 8.4: Tenant-safe alert wiring
```

Rationale: Not a blocking correction now, but this should be split before sprint assignment if the work is too large.

### Stories - Epic 9

#### Story: Add 9.6 Correction-driven vector reindexing

Section: New story.

NEW:

```markdown
### Story 9.6: Correction-driven vector reindexing

As a security owner,
I want vector, embedding, and prompt-context entries invalidated and rebuilt after correction,
So that M2 derived stores do not preserve stale or misassociated material.

Acceptance Criteria:

Given a correction affects material already present in a vector index, embedding store, prompt-context cache, or candidate-ranking cache
When M2 correction propagation runs
Then `ReindexVectors(tenantId, correctionId, sourceVersion)` invalidates and rebuilds the affected entries with idempotent, version-guarded behavior.

Given reindexing exceeds the M2 SLO
When the corrected item is inspected
Then it shows `correction-delayed`, owner role, next safe action, and P2 incident linkage per NFR17a.
```

Rationale: Moves vector reindexing out of M0 Story 2.8 and into the M2 derived-store ownership area.

#### Story: 9.6 Data retention, export, deletion, and consent

Section: Story split.

OLD:

```markdown
### Story 9.6: Data retention, export, deletion, and consent
```

NEW:

```markdown
### Story 9.7: Data-class inventory and retention policy
### Story 9.8: Tenant export workflow
### Story 9.9: Deletion and erasure workflow
### Story 9.10: Consent and lawful-basis metadata
```

Rationale: Separates policy definition, export behavior, deletion/erasure mechanics, and consent metadata.

#### Story: 9.7 Recovery and continuity

Section: Story split.

OLD:

```markdown
### Story 9.7: Recovery and continuity
```

NEW:

```markdown
### Story 9.11: Continuity drill and RPO/RTO validation
### Story 9.12: Projection rebuild validation
### Story 9.13: Scoped outage degradation validation
```

Rationale: Separates disaster-recovery drill, projection rebuild, and dependency-outage isolation testing.

### UX Traceability Tags

Section: Story metadata / acceptance criteria tags.

OLD:

```markdown
PRD S4/S6/S7 are present in the PRD but not explicitly tagged in affected stories.
```

NEW:

```markdown
Add explicit trace tags:

- `Surface trace: S4 Correction` on Story 2.7, Story 2.8, and any correction UI story created later.
- `Surface trace: S6 Outbound Approval` on Epic 6 outbound approval story.
- `Surface trace: S7 Cross-surface Attribution` on Story 1.9 and Epic 5 parity/attribution stories.
```

Rationale: UX already covers these behaviors under broader surfaces, but story authors need explicit PRD-surface traceability.

## 5. Implementation Handoff

Scope classification: **Moderate**.

This is a backlog reorganization and artifact-correction change. It does not require a PRD rewrite, architecture replan, UX redesign, code implementation, or rollback.

Recommended recipients:

- Product Owner / PM: approve story splitting, story renumbering, and Epic 1 framing.
- Developer agent: apply approved edits to `epics.md`, preserving FR/NFR IDs and M0 -> M1 -> M2 order.
- Architect: review Epic 1 split and Story 2.8 / 4.9 / 9.6 placement for FR81a, correction propagation, and derived-store consistency.
- UX designer: verify S4/S6/S7 trace tags after the epics edit.

Success criteria:

- Story 3.2 is no longer a single story and the seven FR22 concerns are independently testable.
- Story 7.7 is no longer a single story and the 15 FR74 subject/action combinations are independently testable.
- Story 6.2 precedes draft creation or Story 6.1 contains a complete minimal authority classifier.
- Story 2.8 no longer depends on Epic 4 AI proposals or M2 vector entries.
- Stories 1.10, 1.11, 1.12, 9.6, and 9.7 are decomposed before estimation.
- S4/S6/S7 trace tags are visible in `epics.md`.
- Re-run readiness validation reports no critical story-sizing violations and no forward dependency between 6.1 and 6.2.

## 6. Checklist Status

| Checklist item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story / artifact identified | Done | Trigger is the 2026-05-30 readiness report, with Stories 3.2 and 7.7 as critical examples. |
| 1.2 Core problem defined | Done | Planning artifact story sizing and dependency defects. |
| 1.3 Evidence gathered | Done | Evidence from readiness report, PRD decomposition guidance, `epics.md`, architecture, and UX. |
| 2.1 Current epic assessed | Done | Multiple epics affected; none invalidated. |
| 2.2 Epic-level changes identified | Done | Modify existing epics and stories; no new top-level epic. |
| 2.3 Remaining epics reviewed | Done | M1/M2 impacts captured. |
| 2.4 Future epic invalidation/new epic need checked | Done | No future epic invalidation. |
| 2.5 Order/priority checked | Done | Epic 6 story order must change. |
| 3.1 PRD conflicts checked | Done | No PRD change required. |
| 3.2 Architecture conflicts checked | Done | No architecture change required. |
| 3.3 UI/UX conflicts checked | Done | Add trace tags; no UX redesign. |
| 3.4 Other artifacts checked | Done | No sprint-status artifact found. |
| 4.1 Direct Adjustment evaluated | Viable | Recommended path. |
| 4.2 Rollback evaluated | Not viable | No implementation to roll back. |
| 4.3 MVP review evaluated | Not needed | MVP scope remains intact. |
| 4.4 Path selected | Done | Direct Adjustment. |
| 5.1 Issue summary created | Done | See Section 1. |
| 5.2 Impact documented | Done | See Section 2. |
| 5.3 Path forward documented | Done | See Section 3. |
| 5.4 MVP impact/action plan defined | Done | No scope change; backlog correction only. |
| 5.5 Handoff plan established | Done | See Section 5. |
| 6.1 Checklist reviewed | Done | Remaining action is user approval. |
| 6.2 Proposal accuracy reviewed | Done | Consistent with source artifacts. |
| 6.3 User approval obtained | Done | Approved by Jerome on 2026-05-30. |
| 6.4 Sprint status updated | N/A | No sprint-status file found. |
| 6.5 Next steps confirmed | Done | Routed as a Moderate backlog reorganization for PO/PM, Developer, Architect, and UX follow-up. |

## 7. Approval and Handoff Log

Approval decision: approved by Jerome on 2026-05-30.

Final scope classification: **Moderate**.

Routed to:

- Product Owner / PM for backlog reorganization approval and story-numbering ownership.
- Developer agent applied approved edits to `epics.md`.
- Architect for review of Epic 1, Story 2.8, Story 4.9, and Story 9.6 placement.
- UX designer for S4/S6/S7 trace verification.

Applied artifact: `_bmad-output/planning-artifacts/epics.md`.

Next implementation step: rerun readiness validation focused on epic quality and forward dependencies.
