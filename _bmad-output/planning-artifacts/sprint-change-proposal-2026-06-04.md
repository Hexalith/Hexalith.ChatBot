---
title: Sprint Change Proposal - Epic 1 Retrospective Documentation Reconciliation
project: Chatbot
date: 2026-06-04
status: approved
mode: Batch
source_report: _bmad-output/implementation-artifacts/epic-1-retro-2026-05-31.md
related_audit: _bmad-output/implementation-artifacts/epic-1-documentation-update-audit-2026-05-31.md
scope_classification: Minor
recommended_approach: Direct Adjustment
implementation_state: already-applied-and-verified
owner: Jerome
approved_by: Jerome
approved_at: 2026-06-04T19:31:41+02:00
---

# Sprint Change Proposal - Epic 1 Retrospective Documentation Reconciliation

## 1. Issue Summary

The Epic 1 retrospective identified a documentation and planning-drift issue after the command spine was implemented. The code and tests established runtime and lifecycle facts that older planning text did not fully reflect:

- The live Aspire/DAPR topology uses canonical EventStore state component `statestore`, ChatBot read-model/idempotency state component `chatbot-statestore`, and Redis pub/sub component `chatbot-pubsub`.
- Local self-hosted Aspire runs with mTLS disabled and therefore needs `accesscontrol.local.yaml`; production keeps deny-by-default `accesscontrol.yaml`.
- The local validation path in this sandbox uses compiled xUnit v3 binaries because VSTest-backed `dotnet test` can fail while opening a denied local socket.
- `Skipped` is part of the M0 lifecycle contract, not only later M1 lifecycle expansion.
- FR81a and related pipeline wording need the implemented split between pre-commit fail-closed audit and post-commit audit emission.

Issue type: implementation-discovered documentation drift. No product direction, epic sequencing, or code rollback issue was found.

Supporting evidence:

- The retrospective explicitly lists the topology, local ACL, xUnit validation, and `Skipped` lifecycle drift as action items.
- The follow-up documentation audit records that README, architecture, PRD, and addendum updates were applied and that OpenAPI plus DAPR ACL files already matched implementation.
- Current planning and implementation artifacts now confirm the corrected text.

## 2. Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] Done | Trigger is Epic 1 retrospective, not a single failed story. Most concrete source is Story 1.1 topology plus Stories 1.4, 1.6, and 1.9 implementation evidence. |
| 1.2 Core problem | [x] Done | Documentation lagged behind implementation reality: runtime topology names, local ACL split, sandbox test path, lifecycle scope, and two-phase audit wording. |
| 1.3 Evidence | [x] Done | Retrospective action items, documentation audit, README, architecture, PRD, addendum, OpenAPI, and sprint-status evidence were inspected. |
| 2.1 Current epic impact | [x] Done | Epic 1 remains complete. The issue does not invalidate completed work. |
| 2.2 Epic-level changes | [x] Done | No new epic, removal, resequencing, or scope change required. Epic 1 documentation was reconciled. |
| 2.3 Future epic impact | [x] Done | Epic 2 depended on the command spine and reused the harnesses and validation pattern; later artifacts confirm this was carried forward. |
| 2.4 Future epic validity | [x] Done | No planned epic became obsolete. |
| 2.5 Priority/order | [x] Done | No epic order change required. |
| 3.1 PRD conflicts | [x] Done | PRD needed lifecycle and FR81a wording reconciliation. Current PRD now treats `Skipped` as M0. |
| 3.2 Architecture conflicts | [x] Done | Architecture needed concrete DAPR component names and local/production ACL distinction. Current architecture now includes them. |
| 3.3 UI/UX conflicts | [N/A] Skip | No UI/UX spec conflict was introduced by this change. |
| 3.4 Other artifacts | [x] Done | README needed runtime/test guidance; OpenAPI and DAPR ACL files required verification only. |
| 4.1 Direct Adjustment | [x] Viable | Best path. Effort low, risk low, no scope or story-count impact. |
| 4.2 Rollback | [N/A] Not viable | Reverting implemented Epic 1 work would not simplify anything and would remove the safety floor. |
| 4.3 MVP Review | [N/A] Not viable | MVP remains achievable; no product-scope reduction needed. |
| 4.4 Recommendation | [x] Done | Direct Adjustment: keep implemented behavior, reconcile docs, preserve audit trail. |
| 5.1-5.5 Proposal components | [x] Done | This proposal records issue, impact, approach, detailed changes, and handoff. |
| 6.1-6.2 Final review | [x] Done | Proposal is internally consistent with current artifacts. |
| 6.3 User approval | [!] Action-needed | Pending Jerome approval of this proposal record. |
| 6.4 Sprint-status update | [N/A] Skip | No epic/story additions, removals, or renumbering are required. Current sprint status already marks Epic 1 and later epics done. |

## 3. Impact Analysis

### Epic Impact

| Epic | Impact | Result |
| --- | --- | --- |
| Epic 1 - First Safe Governed Action & Command Spine | Documentation needed to catch up to implemented runtime topology, validation path, lifecycle scope, and audit split. | Applied via documentation audit. Epic remains done. |
| Epic 2 - Email Intake & Project Association | Depended on Epic 1 command spine, lifecycle states, local topology, and regression harnesses. | No replan needed. Later Epic 2 retro confirms reuse of spine, conformance/isolation harnesses, local ACL distinction, compiled xUnit path, and `Skipped`/correction states. |
| Epics 3-9 | No direct scope impact from the Epic 1 retro trigger. | No changes required by this proposal. |

### Story Impact

No story needs to be added, split, removed, or renumbered.

Story records most relevant to the trigger:

- Story 1.1: Aspire/DAPR topology and root-level submodule policy.
- Story 1.4: pre-commit fail-closed audit and post-commit audit emission.
- Story 1.6: canonical lifecycle states including `Skipped`.
- Story 1.9: first governed command through the full command spine with state-store end-state validation.
- Story 2.9: later confirmation that `Skipped` terminal-state semantics are reused for duplicate suppression and retry/failure handling.

### Artifact Conflicts

| Artifact | Required action | Current state |
| --- | --- | --- |
| `README.md` | Add concrete runtime topology and local validation guidance. | Applied. |
| `_bmad-output/planning-artifacts/architecture.md` | Replace shorthand/obsolete topology wording with actual DAPR component names and ACL split. | Applied. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` | Treat `Skipped` as M0 and align FR81a pipeline wording with implemented audit split. | Applied. |
| `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` | Align Shared Command Pipeline wording with pre-commit audit gate plus post-commit audit emission. | Applied. |
| `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml` | Verify lifecycle/surface-origin contract. | No update required; audit says it already matched implementation. |
| DAPR ACL files | Verify local vs production posture. | No update required; audit says files already documented the distinction. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Update only if story/epic structure changed. | No update required. |

### Technical Impact

No code or infrastructure change is proposed here. The implementation is the source of truth already achieved by Epic 1. The technical impact is documentation correctness and implementation handoff clarity:

- Future stories must use `statestore`, `chatbot-statestore`, and `chatbot-pubsub`.
- Local live topology must keep `accesscontrol.local.yaml` visibly bounded to mTLS-off development only.
- Production references must remain deny-by-default via `accesscontrol.yaml`.
- Validation evidence in this sandbox may use compiled xUnit v3 binaries instead of VSTest-backed `dotnet test`.
- `Skipped` must remain an M0 lifecycle state in contracts, OpenAPI, state-model tests, stories, and PRD text.

## 4. Recommended Approach

Selected path: Direct Adjustment.

Rationale:

- Effort: Low. The affected work is documentation and planning text, not product scope or code behavior.
- Risk: Low. The proposed/current wording is grounded in implementation evidence and already verified by the documentation audit.
- Timeline impact: None. No epic resequencing, backlog restructuring, or rollback is required.
- MVP impact: None. M0 safety floor becomes clearer; the product thesis is unchanged.

Alternatives considered:

- Rollback: rejected because implementation is correct and rollback would weaken the safety floor.
- MVP review: rejected because the retrospective explicitly states no Epic 2 scope rewrite is required.
- New epic/story: rejected because this is not new capability work.

## 5. Detailed Change Proposals

### 5.1 README Runtime and Test Guidance

Artifact: `README.md`

OLD:

- README did not carry the verified Epic 1 runtime topology and local validation guidance.

NEW:

```text
Runtime component names are intentional:

- `statestore` is the canonical EventStore actor/status/archive/checkpoint store.
- `chatbot-statestore` is ChatBot's derived read-model and coarse-idempotency store.
- `chatbot-pubsub` is the Redis pub/sub component carrying governed events.

The local self-hosted Aspire topology loads `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml` because mTLS is disabled. Production must use the deny-by-default `accesscontrol.yaml` with mTLS/Sentry enabled.
```

Additional current guidance:

```text
In this sandbox, `dotnet test` can fail because the VSTest runner opens a denied socket. Story validation uses the compiled xUnit v3 binaries directly.
```

Rationale: Epic 2 and later developers need accurate local setup guidance and must not infer that VSTest-backed `dotnet test` is the only accepted validation path in this sandbox.

### 5.2 Architecture Topology

Artifact: `_bmad-output/planning-artifacts/architecture.md`

OLD:

- Earlier shorthand/obsolete topology wording described a `chatbot-eventstore` style state store and did not consistently distinguish EventStore actor state from ChatBot derived state.

NEW:

```text
Wire Aspire AppHost + DAPR components: canonical EventStore actor/status store `statestore`, ChatBot derived state store `chatbot-statestore`, Redis pub/sub `chatbot-pubsub`, production deny-by-default `accesscontrol.yaml`, and local mTLS-off `accesscontrol.local.yaml`; verify `aspire run` brings up the topology.
```

Rationale: DAPR component names are runtime contracts. Future AppHost, test, and story work must not build against stale component names.

### 5.3 PRD Lifecycle Scope

Artifact: `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`

OLD:

- Parts of the PRD treated `Skipped` as later M1 lifecycle scope.

NEW:

```text
M0 lifecycle states: Received, Proposed, Associated, NeedsReview, Deferred, Rejected, Failed, Skipped, Corrected. The full state-transition matrix expands in M1, but `Skipped` is part of the M0 command-spine contract because duplicate suppression and out-of-scope mailbox rules need a terminal safe state.
```

Rationale: `Skipped` is implemented and tested in Epic 1 and reused by Epic 2 duplicate suppression. Planning docs must not contradict the contract enum, OpenAPI schema, or state-model tests.

### 5.4 Shared Command Pipeline and Audit Split

Artifacts:

- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`

OLD:

- Pipeline wording compressed audit into a simplified single stage.

NEW:

```text
The ChatBot admission layer applies, in order: authentication, tenant-scope binding, authorization, risk classification, approval gate, coarse idempotency check, pre-commit audit gate, EventStore command execution (including fine idempotency), event publication, projection update, and post-commit audit emission.
```

Rationale: Story 1.4 implemented the two-phase audit model. Pre-commit audit is the fail-closed gate; post-commit audit emission records the committed outcome.

### 5.5 OpenAPI and DAPR ACL Verification

Artifacts:

- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml`
- `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml`

OLD:

- These files were candidates for review because they could have drifted.

NEW:

- No edit required. The documentation audit verified that OpenAPI already includes the implemented lifecycle and surface-origin contract, and that DAPR ACL files already document local mTLS-off versus production deny-by-default posture.

Rationale: Avoid churn where source artifacts already match implementation.

## 6. Implementation Handoff

Scope classification: Minor.

Handoff recipients:

| Role | Responsibility |
| --- | --- |
| Architect / Tech Writer | Keep runtime topology and DAPR ACL wording aligned with implementation whenever AppHost or DAPR component names change. |
| Developer agent | For future stories, use existing conformance/isolation harnesses, exact lifecycle tokens, and compiled xUnit validation path when VSTest is blocked. |
| Product Manager / Architect | Treat `Skipped` as M0 in planning language and avoid reclassifying it as later scope. |

Success criteria:

- Architecture and README name `statestore`, `chatbot-statestore`, and `chatbot-pubsub`.
- Local `accesscontrol.local.yaml` is described as mTLS-off development only.
- Production `accesscontrol.yaml` remains deny-by-default.
- PRD and epics treat `Skipped` as part of the M0 lifecycle contract.
- Future story validation records do not imply VSTest-backed `dotnet test` is the only valid local test path in this sandbox.
- No new story, epic, sprint-status, or implementation change is required by this proposal.

## 7. Approval and Routing

Recommended routing: Developer agent / documentation owner for record closure only.

No implementation task remains from the Epic 1 trigger because the documentation audit already applied and verified the required changes. This proposal should be approved as a historical Sprint Change Proposal record, not as a request to change code.

Approval status: approved by Jerome on 2026-06-04T19:31:41+02:00.

No further sprint-status update is needed.
