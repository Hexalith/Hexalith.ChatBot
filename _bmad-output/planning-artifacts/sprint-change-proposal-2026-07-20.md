---
title: Sprint Change Proposal - Standalone Consumer Validation for Story 1.1e
project: Chatbot
date: 2026-07-20
status: approved
mode: Batch
trigger: "Resolve Story 1.1e's independent-validation blocker through standalone checkouts"
scope_classification: Moderate
recommended_approach: Direct Adjustment to the validation evidence method
owner: Jerome
prepared_by: Correct Course workflow
approved_by: Jerome
approved_at: 2026-07-20
implementation_state: planning-and-validation-instructions-updated
handoff_status: routed-to-developer-and-test-architect
---

# Sprint Change Proposal - Standalone Consumer Validation for Story 1.1e

## 1. Issue Summary

Story 1.1e requires independent canonical restore/build/test evidence for every package consumer before the final ChatBot umbrella rerun. The 2026-07-20 evidence pass proved ChatBot itself green but could not complete six consumer `.slnx` builds because those solutions intentionally include projects held in the consumer repository's own submodules. Inside the ChatBot umbrella, those dependency checkouts are nested and must remain uninitialized. Timesheets similarly needs its own tracked `Hexalith.Works` source checkout, which ChatBot does not and should not declare at its root.

The blocker came from treating two different roots as one:

- In the ChatBot umbrella, only ChatBot's root `.gitmodules` entries may be initialized, non-recursively. Nothing owned beneath `references/` may be initialized there.
- In an isolated standalone consumer checkout, that consumer is the repository root. Its own root-declared dependencies may be initialized with explicit non-recursive pathspecs, while dependencies declared below those direct checkouts remain uninitialized.

The previous umbrella-nested evidence is useful diagnostic history but is not canonical independent validation.

## 2. Correct Course Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | Done | Story 1.1e, final independent-consumer completion task. |
| 1.2 Core problem | Done | The validation location—not the consumer solution shape or dependency manifest—conflicted with the no-nested-initialization invariant. |
| 1.3 Evidence | Done | ChatBot `82af7d3` passed the full umbrella sweep; six consumer solutions required their own declared dependency projects, and Timesheets required its own Works gitlink. |
| 2.1 Current epic | Done | Epic 1 remains viable and in progress. |
| 2.2 Epic-level change | Done | Clarify Story 1.1e acceptance only; no new story or epic. |
| 2.3 Remaining epics | Done | No product or future-epic impact. Story 1.1f remains gated by Story 1.1e. |
| 2.4 Future validity | Done | No story becomes obsolete. |
| 2.5 Priority/order | Done | Independent standalone rows must be green before the final ChatBot umbrella row. |
| 3.1 PRD conflict | N/A | Product requirements and MVP scope do not change. |
| 3.2 Architecture conflict | Done | “Root-declared” must be made relative to the checkout under validation. |
| 3.3 UX conflict | N/A | No surface, behavior, accessibility, responsive, or localization impact. |
| 3.4 Other artifacts | Done | Canonical epics wording, Story 1.1e, sprint tracking note, Timesheets validation instructions, and this proposal require updates. |
| 4.1 Direct Adjustment | Viable | Low planning effort; medium cross-repository validation effort and low architectural risk. |
| 4.2 Rollback | Not viable | Reverting package migration or consumer solutions does not solve evidence isolation. |
| 4.3 MVP Review | Not viable | No product-scope pressure exists. |
| 4.4 Recommended path | Done | Standalone-checkout validation at ChatBot-pinned commits. |
| 5.1-5.5 Proposal components | Done | Sections 1-6 provide issue, impact, approach, edits, evidence gate, and handoff. |
| 6.1 Checklist review | Done | All applicable items are resolved. |
| 6.2 Accuracy review | Done | The target matrix is derived from ChatBot `bd652e3`; execution must freeze and verify the then-current matrix. |
| 6.3 Explicit approval | Done | Jerome's 2026-07-20 directive explicitly selects this method and constraints. |
| 6.4 Sprint update | Done | Story remains `in-progress`; the ordered standalone-before-umbrella gate is recorded. |
| 6.5 Handoff | Done | Developer/Test Architect own evidence capture; Superproject maintainer owns the final umbrella rerun. |

## 3. Impact Analysis

### Epic and story impact

- Epic 1 remains `in-progress`; no story is added, removed, renumbered, or reprioritized.
- Story 1.1e keeps its package-governance scope. Acceptance Criterion 5 and the final evidence task now define where independent validation occurs and what qualifies.
- Story 1.1f remains `backlog` and depends on Story 1.1e.

### Architecture impact

The submodule invariant now distinguishes the active checkout root. This is a validation-boundary clarification, not permission to deepen the ChatBot dependency graph.

### Technical and repository impact

- No ChatBot `.gitmodules` change.
- No consumer `.gitmodules` change.
- No umbrella-level `Hexalith.Works` dependency.
- No dependency project removal from any consumer `.slnx`.
- No package, source, generated artifact, gitlink, CI, or runtime change is authorized by this proposal.
- Standalone checkout creation and validation outputs are disposable evidence operations outside the ChatBot worktree.

### PRD and UX impact

None. The PRD and complete binding UX package remain unchanged.

## 4. Recommended Approach

Use a **Moderate Direct Adjustment**:

1. Freeze one ChatBot root commit and record the exact Builds and consumer gitlinks it pins.
2. Validate `Hexalith.Builds` and each root-declared .NET consumer in an isolated standalone checkout at its pinned commit.
3. In each standalone checkout, initialize only dependencies declared by that standalone repository's root `.gitmodules`, using explicit pathspecs without `--recursive` or `--remote`.
4. Permit standalone Timesheets to initialize its direct `Hexalith.Works` dependency; do not initialize dependencies owned by Works or any other direct dependency.
5. Require every standalone row to pass before rerunning the unchanged ChatBot umbrella at the same recorded pin matrix.

This preserves consumer-owned solution navigation and release-support contracts while satisfying the ChatBot umbrella's strict no-nested-initialization invariant.

## 5. Detailed Change Proposals

### Architecture

**Old:** “Root-declared submodules under `references/` only; never recursive init.”

**New:** Root is relative to the checkout under validation. ChatBot may initialize only ChatBot root entries. An isolated standalone consumer may initialize only its own root entries, non-recursively. Timesheets may initialize its root-declared Works only in standalone Timesheets. No manifest, gitlink, or solution-membership change is implied.

**Rationale:** The old shorthand correctly protected ChatBot but was ambiguous when used for independent consumer validation.

### Canonical Story 1.1e and implementation story

**Old:** Each owning repository passes independently before the complete superproject, without defining checkout isolation or direct dependency initialization.

**New:** Each pinned consumer passes from an isolated standalone checkout at the exact ChatBot gitlink; only that checkout's direct root dependencies may be initialized non-recursively. Evidence must prove identity, isolation, direct dependency SHAs, canonical commands/results, package governance, and a clean tree. The final ChatBot row waits for all standalone rows.

**Rationale:** Completion evidence becomes reproducible and cannot rely on prohibited umbrella-nested state.

### Sprint status

**Old:** Story 1.1e was `in-progress` with only the original proposal comment.

**New:** Story remains `in-progress`; the sprint comment states that standalone consumer rows precede the final umbrella row and that ChatBot nested initialization remains forbidden.

**Rationale:** No supported `blocked` status exists, and implementation is not complete. The comment makes the active gate explicit without inventing a status.

### Timesheets validation instructions

**Old:** Initialize `Hexalith.Works` from `references/Hexalith.Timesheets` inside the ChatBot umbrella and treat that run as completion evidence.

**New:** Retain the existing file path for trace stability but replace its content with standalone Timesheets instructions at the ChatBot-pinned Timesheets SHA. Initialize explicit Timesheets root paths only; Works is allowed as a direct Timesheets dependency, while Works' dependencies remain uninitialized. The umbrella run is historical only.

**Rationale:** Timesheets' manifest is correct; the validation location was wrong.

## 6. Standalone-Checkout Evidence Contract

### Observed pin matrix

The matrix observed at ChatBot `bd652e3c61ebfa0202f6a1fdb696759637a21bca` is:

| Role | Repository | Pinned SHA |
| --- | --- | --- |
| Authority | Builds | `e4ae82df6cfcc6511a32fc2ce100070d7924f119` |
| Consumer | EventStore | `afcc167e0c539b09ecad978a58da2f756123f34e` |
| Consumer | Tenants | `f03b474d836a3465e311c48e90c75ae1e755ef45` |
| Consumer | FrontComposer | `550cb0602d506d9fd008a8c09f2cca6b328ec1e3` |
| Consumer | Folders | `cfe830b410bce6e04308ea67c3492eca6bc8bdfd` |
| Consumer | Conversations | `6e8cf8a6605142808d1afede3f2d0e29541f0e08` |
| Consumer | Projects | `fca2bfc050f7a581a467aaa3921e2aa61f249e72` |
| Consumer | Parties | `b316dab5cf27d9f80a662b5b3cd6c5e2569adfd7` |
| Consumer | Memories | `f474db156c372aad4ab243c13a669c35d78b49e6` |
| Consumer | Commons | `ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c` |
| Consumer | Timesheets | `441f02509cfd43c888e2d4317a167b41657208b4` |
| Consumer | PolymorphicSerializations | `a5dd24f5e66324d18241de7d5521ee124eab4877` |

Execution must first record the then-current ChatBot `HEAD` and verify the complete matrix. A documentation-only ChatBot commit may supersede the observed root when every gitlink is unchanged. If a gitlink changes, that row must be rerun and every reused row must be verified against the identical pinned SHA.

### Evidence required before the final Story 1.1e umbrella stage resumes

For Builds and all eleven standalone consumer rows:

1. Repository URL, ChatBot path and baseline SHA, expected gitlink SHA, actual standalone `HEAD`, empty superproject-working-tree output, and clean pre-run status.
2. The root `.gitmodules` direct path/SHA inventory, exact explicit non-recursive initialization command, and proof that only direct paths were initialized while dependency-owned submodules remained uninitialized.
3. Exact canonical restore/build and focused-test commands, exit codes, warning/error totals, and passed/failed/skipped totals. Every required lane must run and pass.
4. Applicable catalog, exclusive-consumer-authority, exception, family-alignment, package/pack, and resolved-graph results with no local version escape hatch or NU1109.
5. Clean post-run status and `git diff --check`, with no `.gitmodules`, `.slnx`, gitlink, source, or generated workaround.
6. One PASS table bound to the recorded ChatBot pin matrix.

ChatBot is the twelfth .NET consumer and is the final unchanged-umbrella row. Its restore, serialized warning-as-error Release build, package-authority validators, Architecture/UI suites, all required test projects, and relevant integration lanes must be rerun after the standalone PASS table is complete. A blocker or unrun required lane is not completion evidence.

## 7. Implementation Handoff

| Recipient | Responsibility |
| --- | --- |
| Developer / validation runner | Create disposable standalone checkouts, initialize only direct root dependencies, run owning-repository commands, and capture the evidence dossier. |
| Test Architect | Verify that every required lane is non-vacuous, green, commit-bound, and free of umbrella-nested or solution-shape workarounds. |
| Superproject maintainer | Verify the pin matrix and run the final unchanged ChatBot umbrella sweep only after all standalone rows pass. |

**Success criteria:** the evidence dossier is complete for Builds plus all eleven standalone consumers at the ChatBot-pinned commits, the final ChatBot row is green, and Story 1.1e can advance without changing manifests, solution membership, dependencies, or package authority.

## 8. Approval and Workflow Log

Jerome explicitly approved the standalone-checkout method and its constraints in the 2026-07-20 correction directive. The proposal is therefore approved and applied in batch mode.

| Event | Result |
| --- | --- |
| Change analysis | Completed against PRD, canonical epics, architecture, binding UX package, Story 1.1e evidence, sprint status, and validation instructions. |
| Selected approach | Direct Adjustment; standalone consumer validation. |
| Scope | Moderate cross-repository validation correction; no product or implementation mutation. |
| Artifact updates | Architecture, canonical story, implementation story, sprint note, Timesheets validation instructions, and this proposal. |
| Handoff | Developer / Test Architect / Superproject maintainer. |
