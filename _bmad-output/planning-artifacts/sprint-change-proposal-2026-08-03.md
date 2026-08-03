# Sprint Change Proposal — Mechanical Story-Evidence Integrity Gate

- **Date:** 2026-08-03
- **Author:** Administrator (via Correct Course workflow)
- **Trigger:** Open Epic 13 retrospective action: “Add a mechanical story-evidence integrity gate that reconciles File Lists, scoped diffs, machine test results, primary-path execution, and checked tasks before a story moves to done.”
- **Evidence:** `_bmad-output/implementation-artifacts/epic-10-retro-2026-07-17.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, representative story/review records, current CI/release workflows, the canonical PRD/addendum, `architecture.md`, `epics.md`, the indexed UX package, and the technical-enabler ledger.
- **Review mode:** Batch
- **Scope classification:** **Moderate delivery-governance correction.** Product scope, product epic/story counts, runtime behavior, and UX remain unchanged. A separately tracked technical enabler adds a repository-owned evidence contract, validator, and CI enforcement before any subsequent story can become `done`.
- **Approval state:** **Approved by Administrator on 2026-08-03.** The proposal is finalized and routed for implementation. No planning, sprint-status, source, test, or CI change described below has yet been applied.

---

## 1. Issue Summary

The repository has strong domain-specific quality gates, but story completion itself is still a narrative/manual decision. A story record can therefore disagree with the repository state that is supposed to prove it. The missing control is not another test suite; it is a fail-closed reconciliation step over the evidence already produced.

The correction is justified by concrete repository evidence:

1. The 2026-07-17 retrospective records stale test counts, incomplete File Lists, inaccurate scope claims, checked tasks contradicted by the diff, and verification claims that did not match the current tree. It names Story 10.6a's false empty-diff claim and missing expected-tests handoff, and Story 10.6b's checked transport task while no producer or receiver existed.
2. The same retrospective makes “Story integrity needs a mechanical gate” a key learning and carries the exact requested action into `sprint-status.yaml`, owned by Amelia with Murat and required before any subsequent story moves to `done`.
3. A repository search finds **64 currently `done` story records** containing explicit File List omission/drift or stale test-count language. Some references are preventive or retrospective, but the breadth confirms that manual reconciliation is recurrent rather than exceptional.
4. Story 13.9's first real-render pass produced 42 screenshots and passed coarse assertions even though the captures showed a broken layout. The story was corrected only after the primary render was inspected and the load-bearing `.fluent-layout { display: grid; }` invariant was asserted directly.
5. Story 12.15 withdrew earlier measurements because the local bundle predated the current evidence-manifest contract and could not be replayed through the shipped gate. It remains `in-progress`, demonstrating why test prose, artifact provenance, primary-path execution, and status must agree mechanically.

### Problem statement

The current completion path can accept internally inconsistent story evidence, so a story may be proposed as `done` even when its declared File List, scoped repository diff, machine test outcomes, required primary paths, and checked tasks do not reconcile.

### Change objective

Before a new transition to `done` is accepted, one repository-owned command must produce a pass for the exact story, exact source scope, exact machine results, and exact primary-path obligations. Missing, stale, ambiguous, zero-test, skipped-primary, or contradictory evidence must fail with stable reason codes and leave the story in `review` or `in-progress`.

### Scope boundaries

- This is a delivery-integrity control, not a product feature.
- It does not reopen historical `done` stories solely to retrofit evidence.
- It does not replace adversarial code review, product acceptance, visual judgment, or test-quality review.
- It does not infer semantic correctness from Markdown. It proves that declared completion evidence is present, current, internally consistent, and bound to executable results.
- It does not initialize, update, or recurse into submodules. Root-declared repository and gitlink scopes are read only.

## 2. Correct Course Checklist Status

### Section 1 — Trigger and context

- [x] Trigger identified in the Epic 10 retrospective and the current sprint action ledger.
- [x] Problem classified as a process/quality and delivery-evidence integrity gap.
- [x] Concrete evidence verified; the workflow does not rely on speculation.
- [x] Ownership identified: Amelia with Murat, with architecture and CI support.

### Section 2 — Epic and story impact

- [x] No product epic goal or user outcome becomes obsolete.
- [x] No product story is added, removed, split, or renumbered.
- [x] Current `review`/`in-progress` stories become prospective consumers of the gate.
- [x] Historical `done` stories remain historical evidence; no mass reopening is proposed.
- [N/A] Epic rollback or MVP de-scope is not required.

### Section 3 — Artifact and delivery impact

- [x] `technical-enablers.md`, `epics.md`, `architecture.md`, planning index, sprint workflow notes, CI, solution/tooling, and developer documentation impacts are defined.
- [x] PRD/addendum reviewed: existing validation NFRs support the control; no requirement-content change is needed.
- [x] UX package reviewed in full: its primary-path evidence hierarchy remains binding; no UX change is needed.
- [x] Test/CI implications, primary-path retention, root-submodule handling, and branch-protection dependency are defined.
- [N/A] No `docs/index.md` exists, so Document Project routing is unavailable and unnecessary.

### Section 4 — Path evaluation

- [x] Direct adjustment evaluated and recommended.
- [x] Rollback evaluated and rejected.
- [x] MVP review/de-scope evaluated and rejected.

### Section 5 — Proposal completeness

- [x] Issue, impact, approach, exact artifact edits, acceptance contract, risks, validation, and handoff are included.
- [x] The enabler has a non-circular bootstrap completion path.
- [x] Product counts and existing status history are preserved.

### Section 6 — Final review

- [x] Proposal is internally consistent and implementation-ready.
- [x] Explicit approval received from Administrator on 2026-08-03.
- [x] Proposal finalized and routed; downstream planning, sprint, source, test, and CI changes remain implementation work.

## 3. Impact Analysis

### 3.1 Product, MVP, and UX impact

- No FR, NFR, actor, journey, supported surface, runtime behavior, or release milestone changes.
- The product plan remains **13 product epics and 112 assignable product stories**.
- M0 → M1 → M2 ordering remains unchanged.
- The indexed UX evidence hierarchy remains unchanged: primary live execution is required for runtime/layout/browser/asset/hosting/SignalR claims; fallbacks remain supporting diagnostics only.
- No new UI, localization, accessibility, or responsive behavior is introduced.

### 3.2 Epic and story impact

- The action remains attributed to the Epic 13 retrospective because that delivery chain exposed the failure, but the implementation is **Technical Enabler TE-2**, not a product story. It has no standalone user outcome and does not inflate product counts.
- TE-2 is a prerequisite for every subsequent `review → done` proposal across all epics and technical enablers.
- As of this proposal, the prospective hold covers the stories currently in `review` or `in-progress`, including canonical 2.9, 9.1–9.6, 12.15, and 13.2–13.8; Story 12.16 remains `backlog` and will inherit the rule when it advances.
- A story may remain in `review` while a required environment or retained primary-path artifact is unavailable. A diagnostic fallback does not waive the hold.
- Existing `done` stories are not retroactively failed. They may be used as positive/negative parser fixtures only after removing project payloads and making the fixture intent explicit.

### 3.3 Artifact impact

| Artifact | Required change after approval |
| --- | --- |
| `technical-enablers.md` | Add TE-2 with its contract, tasks, owners, bootstrap rule, and completion evidence. |
| `epics.md` | Increase `technicalEnablerCount` from 1 to 2, update the overview, and add a binding cross-cutting story-completion rule. Product epic/story counts stay unchanged. |
| `architecture.md` | Add the evidence-integrity process pattern and CI/CD invariant; add the approved proposal to inputs. |
| `index.md` | Replace the stale 11-epic/129-story readiness note with the canonical 13/112 inventory and TE-1/TE-2 discovery links. |
| `sprint-status.yaml` | Add the temporary no-new-`done` hold and the permanent gate rule. Keep the action open until TE-2 is implemented and self-validates; then mark only that action `done`. |
| `story-evidence-policy.json` | Add the versioned repository policy for story grammar, allowed evidence formats, source-digest rules, primary-path triggers, result freshness, and root-declared submodule scopes. |
| `_bmad-output/implementation-artifacts/evidence/<story-key>.json` | Add one machine-readable evidence contract for each prospective `done` transition. It declares scope, result inputs, primary-path obligations, and task/AC mappings; it does not contain secrets or product payloads. |
| `tools/Hexalith.ChatBot.StoryEvidenceGate/` | Add a dependency-free .NET 10 console validator using framework libraries and Git read-only commands. |
| `tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/` | Add parser, reconciliation, policy, and mutation/negative fixtures; add the tool and tests to `Hexalith.ChatBot.slnx`. |
| `.github/workflows/ci.yml` | Emit machine-readable test results, retain them on pass/failure, detect proposed `done` transitions, and run the gate after required evidence is available. |
| `.github/workflows/release.yml` | No duplicate default gate. Preserve its existing retained primary-path artifacts so a story contract can cite an approved exact-SHA artifact when the required lane is release-only. |
| `docs/story-evidence-integrity.md` and `README.md` | Document authoring, local preflight, CI behavior, reason codes, exception policy, and the exact command. |
| `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md` | No change. Repository-specific gate guidance belongs in repository docs/config, not the synchronized universal baseline. |
| PRD/addendum and UX package | No change. |

### 3.4 Technical and CI impact

- Add one small .NET tool and test project; no new NuGet dependency is required.
- Existing test jobs must emit TRX or another policy-approved machine format. Narrative Markdown counts are never a result source.
- The gate consumes existing test outputs; it does not rerun the whole suite merely to reconcile it.
- CI must fetch enough Git history to resolve the event base and head commits. It must not use recursive or remote submodule updates.
- A protected-branch rule must require the `story-evidence-integrity` check. Repository code can report failure, but branch protection is the external control that prevents merge/direct completion bypass.
- Primary-path evidence may come from the current workflow or a retained scheduled/release artifact only when the artifact is bound to the exact tested implementation digest and satisfies the configured age policy.

### 3.5 Effort, risk, and compatibility

- **Estimated implementation:** 3–5 engineer/test-architect days, including CI wiring and mutation fixtures.
- **Risk:** medium. Markdown grammar variation, self-referential evidence files, multi-job result collection, and submodule scopes can create false positives if the policy is underspecified.
- **Compatibility:** prospective enforcement avoids rewriting 112 historical records. Existing story files need an evidence contract only when they next propose `done`.
- **Operational dependency:** protected-branch configuration must require the new check before the action can be closed.

## 4. Recommended Approach

### Option A — Direct adjustment through Technical Enabler TE-2 (recommended)

Add one repository-owned evidence contract and validator, wire it to proposed `done` transitions, and require a passing CI check. Keep product scope and historical story status unchanged.

**Benefits**

- Directly closes the recorded action without creating a process-shaped product story.
- Reuses existing tests and primary-path lanes while making their provenance and applicability checkable.
- Fails on the recurring bookkeeping defects before they become accepted status.
- Produces stable reason codes and a machine report that reviewers can inspect.
- Supports root and root-declared submodule scopes without recursive Git behavior.

**Costs and risks**

- Requires a canonical story/evidence grammar for prospective transitions.
- Requires CI artifact collation and branch-protection configuration.
- Does not prove semantic correctness; review remains mandatory.

### Option B — Keep manual review and strengthen the checklist

Add more reviewer instructions but no executable gate.

**Rejected.** The same manual reconciliation instruction already recurs across retrospectives and story reviews. The 64 matching `done` records show that reminders do not provide a durable control.

### Option C — Roll back or reopen all completed stories

Re-audit every historical story before continuing.

**Rejected.** That would be expensive, would rewrite historical acceptance context, and is unnecessary to prevent the next bad transition. Historical defects remain useful fixtures; only a separately identified product risk should reopen a completed story.

### Option D — Reduce MVP scope or delay current product outcomes

Remove or defer product scope to reduce verification load.

**Rejected.** The defect is evidence integrity, not an unachievable product requirement. Removing outcomes would not make remaining completion claims more trustworthy.

### Recommendation

Approve **Option A** as a moderate correction. Apply the planning hold immediately after approval, implement TE-2 before any subsequent `done` transition, then close the open sprint action only after the gate validates its own evidence and the protected check is active.

## 5. Detailed Change Proposals

### 5.1 `technical-enablers.md` — add TE-2

**OLD**

> The ledger contains only TE-1, DomainService SDK Host Adoption, and ends with TE-1's completion evidence rule.

**NEW**

Append this separately tracked enabler:

> ## TE-2 — Mechanical Story-Evidence Integrity Gate
>
> - **Status:** planned; blocks subsequent `done` transitions until complete
> - **Source:** `sprint-change-proposal-2026-08-03.md`
> - **Owners:** Amelia (implementation) with Murat (evidence/primary-path policy); Winston reviews architecture and CI boundaries.
> - **Product impact:** none; delivery-integrity control only.
> - **Invariant:** a story or technical-enabler item may move to `done` only when the repository-owned gate passes for the proposed status, exact scoped diff, exact machine results, required primary paths, File List, and checked-task/acceptance mappings.
>
> | Task | Outcome | Status |
> | --- | --- | --- |
> | TE-2.1 | Define the versioned JSON policy and per-story evidence-contract schema, including stable reason codes and metadata-only output. | planned |
> | TE-2.2 | Reconcile story/sprint status, File List, explicit out-of-scope disclosures, root and root-declared-submodule diffs/gitlinks, and mandatory checkbox state. | planned |
> | TE-2.3 | Parse policy-approved machine results, bind them to the tested implementation digest, and enforce required primary-path execution with zero-test/all-skipped/fallback-only failure. | planned |
> | TE-2.4 | Require every checked task and acceptance criterion to map to current diff and/or passing machine assertions; detect proposed `done` transitions in CI and emit a fail-closed report. | planned |
> | TE-2.5 | Prove the gate with positive, per-reason negative, mutation, multi-repository, and bootstrap self-validation fixtures; publish the developer runbook and activate the protected check. | planned |
>
> ### Completion evidence rule
>
> TE-2 cannot waive its own gate. While its record is still `review`, CI evaluates it with `targetStatus=done`; after all negative mutations are proven to fail and the positive exact-scope run passes, its status may change to `complete`. The Epic 13 action stays open until that self-validation and required branch check are both active.

**Rationale:** This work has no standalone user outcome and mirrors the approved TE-1 separation principle. It should not become Epic 14 or inflate the product story count.

### 5.2 `epics.md` — make the rule cross-cutting without adding a product story

**OLD frontmatter and overview**

```yaml
technicalEnablerCount: 1
```

> Technical Enabler TE-1 is tracked separately and is excluded from the product epic/story counts.

**NEW**

```yaml
technicalEnablerCount: 2
storyEvidenceIntegrityAlignedAt: "2026-08-03"
```

> Technical Enablers TE-1 and TE-2 are tracked separately and are excluded from the product epic/story counts. TE-2 governs prospective story-completion evidence across every product epic.

Add this binding bullet under **Cross-cutting acceptance & planning guidance**:

> - **Mechanical story-evidence completion gate.** A story may be proposed as `done` only through the repository-owned TE-2 gate. The gate must reconcile the story File List with the explicit repository/submodule-scoped diff; parse current policy-approved machine test results; prove every required primary browser/runtime/topology path executed with no required skip or diagnostic substitution; and bind every checked task and acceptance criterion to current diff and/or passing assertions. Missing, stale, zero-test, all-skipped, fallback-only, scope-mismatched, status-mismatched, or contradictory evidence leaves the story in `review`/`in-progress`. Historical `done` records are not retroactively reopened solely by this rule.

**Rationale:** The rule applies to every product story but remains acceptance guidance rather than a new FR, NFR, or product story.

### 5.3 `architecture.md` — add the delivery evidence invariant

**OLD — Process Patterns / Enforcement Guidelines**

> Tests are required in the same change, with Tier 2/3 state-store end-state inspection. Mechanical enforcement currently covers architectural boundaries, conformance, and isolation.

**NEW — add Story completion evidence integrity**

> **[ChatBot delivery] Story-evidence integrity:** `done` is a gated state transition. One repository-owned validator reads the story, sprint ledger, explicit evidence contract, exact root/root-submodule diff, and policy-approved machine results. It fails closed unless File List and scoped change sets reconcile, result provenance matches the tested implementation digest, mandatory tests are non-vacuous, required primary paths executed, and every checked task/acceptance item has current evidence. The report is metadata-only and uses stable reason codes. This validates evidence integrity; it does not replace semantic or adversarial review.

**OLD — Domain-Module CI/CD Invariant / Non-vacuous gates**

> Required Aspire/Dapr topology and browser tiers must execute their named tests. Missing, zero-test, self-skipped, or all-skipped evidence fails the lane; uploaded results do not substitute for passing execution.

**NEW — retain that paragraph and add**

> - **Story status transition gate:** CI detects each story or sprint-ledger transition to `done` and requires a matching TE-2 evidence contract and passing `story-evidence-integrity` report. Results come from machine files produced by the current exact implementation digest or from an approved retained exact-digest artifact within policy age. Narrative summaries, screenshots without direct invariant assertions, diagnostic fallbacks, and unrelated aggregate suites cannot satisfy a missing local primary-path obligation. Root-declared submodule changes require both the submodule diff and superproject gitlink to reconcile; nested submodules are never initialized.

Add one Implementation Handoff bullet requiring the local TE-2 preflight before proposing `done`.

**Rationale:** Architecture already says gates must be mechanical and non-vacuous. This closes the lifecycle gap between those gates and story status.

### 5.4 Evidence contract and policy — define one mechanical source of reconciliation

Add `story-evidence-policy.json` and one `_bmad-output/implementation-artifacts/evidence/<story-key>.json` per prospective transition.

The policy must define:

- schema version and minimum supported version;
- canonical story headings/sections and the checkbox sections that are mandatory;
- accepted result formats (`.trx` initially; additional formats require an explicit parser and tests);
- deterministic implementation-scope digest rules that avoid self-reference by excluding only the generated gate report and declared lifecycle bookkeeping fields, never production/test/config changes;
- event base/head resolution for pull requests and pushes;
- root repository and root-declared submodule scopes, with no nested/recursive discovery;
- maximum result/evidence age and exact-SHA/digest binding;
- changed-path/claim-class triggers for primary browser, SignalR, hosting/assets, Dapr/Aspire topology, and retained recovery evidence;
- stable fail reason codes;
- metadata-only/redaction requirements;
- explicit, versioned exceptions. A missing environment may keep a story in `review`; it is not a passing exception for a required primary path.

Each story evidence contract must identify:

- story key/path and proposed status;
- sprint-status key;
- explicit repository scope(s), base commit(s), and head/digest resolution;
- required test lanes and machine-result selectors;
- required primary-path classes and approved artifact source;
- normalized task/AC evidence mappings;
- any changed file intentionally owned by another story, with owner and reason. Such a disclosure prevents silent omission but does not make a mixed-scope commit acceptable unless policy explicitly permits it.

### 5.5 Reconciliation rules — fail closed on each contradiction

The tool must return non-zero and emit a stable reason when any of these conditions holds:

| Area | Required reconciliation | Example failure reason |
| --- | --- | --- |
| Status | Story header, sprint key, proposed target, and evidence contract identify the same story and transition. | `status_mismatch` |
| File List | Normalized File List equals the scoped changed-path set after explicit, policy-valid disclosures; listed files exist at head; no silent glob or blanket `_bmad-output` exclusion. | `file_list_diff_mismatch` |
| Submodules | Root gitlink change and submodule base/head diff are both explicit; nested scopes are rejected. | `gitlink_scope_mismatch` |
| Scope | Base/head resolve, head matches CI, and no changed implementation/test/config file is hidden as bookkeeping. | `scope_digest_mismatch` |
| Results | Every required lane has parseable machine output, successful exit/outcome, non-zero tests, no failures, and no forbidden skips. | `machine_results_invalid` |
| Freshness/provenance | Results bind to the exact implementation digest and satisfy the age/retention policy. | `evidence_stale_or_unbound` |
| Primary path | Every triggered/declared primary-path class has a successful recognized lane; fallback/supporting evidence cannot substitute. | `primary_path_not_executed` |
| Tasks/ACs | Every mandatory checkbox is complete, and every checked task/AC maps to at least one current diff path or passing machine assertion required by its evidence row. | `checked_item_evidence_mismatch` |
| Evidence safety | Contract/report contains only allowed metadata and locators; secrets or product payload fields are rejected. | `evidence_payload_forbidden` |

Unchecked mandatory work blocks `done`. A genuine deferral must move to an owned story/action with a rationale and cannot be used to claim the current acceptance criterion complete.

The gate's success output must include story key, base/head, repository scopes, normalized File List/diff counts, test totals by lane, required primary-path verdicts, checked-item coverage, policy version, timestamp, and artifact locators/checksums. It must not copy logs, messages, prompts, credentials, screenshots, or tenant payloads into the report.

### 5.6 Tool and test implementation

Add:

```text
tools/Hexalith.ChatBot.StoryEvidenceGate/
tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/
story-evidence-policy.json
```

The tool should use .NET 10 framework APIs (`System.Text.Json`, XML parsing, process execution) and add no package unless implementation proves a framework gap. It may execute only read-only Git commands. It must never mutate status automatically; a pass authorizes the proposed transition, while the status edit remains reviewable in the change.

Required test matrix:

- one valid single-repository story;
- File List missing, extra, nonexistent, duplicate, renamed, and disclosed-out-of-scope cases;
- dirty/untracked local preflight and immutable CI base/head cases;
- root gitlink plus root-declared submodule diff; nested submodule rejection;
- malformed, missing, failed, zero-test, all-skipped, mixed-skip, stale, wrong-SHA, and wrong-digest TRX/result cases;
- primary browser/runtime path pass and fallback-only rejection;
- checked task with no mapping, stale path, failed assertion, or unchecked mandatory child;
- story/sprint-status mismatch;
- payload/secret-field rejection;
- mutation tests proving each fail reason can be triggered;
- TE-2 prospective self-validation with `targetStatus=done`.

### 5.7 CI and branch protection

**OLD**

```yaml
- name: Test
  run: dotnet test Hexalith.ChatBot.slnx --no-build --configuration Release
```

**NEW behavior**

1. Emit per-project/lane TRX under collision-safe directories and upload results on success or failure.
2. Detect proposed `done` transitions between the event base and head.
3. If none exist, run the gate's own test project and report “no transition” successfully.
4. If transitions exist, collect each story's declared current-run or retained exact-digest evidence and run the validator once per story.
5. Publish one JSON report per story and a concise job summary; fail the job on any mismatch.
6. Require the named `story-evidence-integrity` check in branch protection for `main` and other protected release branches.

CI checkout may fetch the required base history but must retain the existing root-only, non-recursive submodule policy.

### 5.8 `sprint-status.yaml` — hold and lifecycle rule

**OLD workflow note**

> Dev moves story to `review`, then runs code-review (fresh context, different LLM recommended).

**NEW — retain it and add**

> - A story moves from `review` to `done` only when the TE-2 `story-evidence-integrity` check passes for that exact proposed transition.
> - Until TE-2 self-validates and the check is required, no subsequent story may move to `done`; stories remain in `review`/`in-progress` without implying product failure.

The existing action item stays `open` through implementation. After TE-2 self-validation and protected-check activation, change only its status to `done`; do not rewrite its wording or original ownership.

### 5.9 Planning index and developer documentation

Update the planning index's stale inventory:

**OLD**

> `epics.md` is aligned to 11 epics and 129 assignable stories...

**NEW**

> `epics.md` contains 13 canonical product epics and 112 assignable product stories. TE-1 and TE-2 are tracked in `technical-enablers.md` and excluded from product counts; TE-2 governs prospective `done` transitions.

Add `docs/story-evidence-integrity.md` with the evidence grammar, lifecycle, examples, reason-code catalog, local preflight, CI artifact behavior, retained-evidence rule, root-submodule rule, and troubleshooting. Add a concise README link and command. Do not copy this repository-specific process into the synchronized universal assistant entry points.

## 6. Implementation Handoff

### 6.1 Sequencing

1. **Product Owner / delivery lead:** approve this correction and apply the planning/index/sprint hold without changing product story counts or historical `done` statuses.
2. **Winston (architecture):** review the policy boundary, implementation digest, self-reference exclusions, multi-repository model, and CI/release evidence provenance.
3. **Amelia (development):** implement the tool, JSON contracts, exact-diff/File List/status/task reconciliation, and solution integration.
4. **Murat (test architecture):** define primary-path triggers, machine-result rules, freshness/retention, required-skip policy, negative fixtures, and mutation tests.
5. **CI owner:** emit/upload collision-safe results, add transition detection and the named check, then configure protected branches to require it.
6. **Amelia + Murat:** run TE-2's prospective self-validation, record the exact command/result, mark TE-2 complete, and close the sprint action.
7. **Story owners:** only then propose subsequent `done` transitions through the gate.

### 6.2 Acceptance conditions for TE-2

TE-2 is complete only when all of the following are true:

- the tool and its tests build in the solution with warnings as errors;
- every negative/mutation fixture fails for its intended stable reason and the positive fixtures pass;
- a root-declared submodule fixture reconciles without recursive initialization;
- current-run TRX and retained exact-digest artifact paths are both proven;
- required primary-path zero-test, skipped, fallback-only, stale, and wrong-SHA cases fail;
- story header and sprint-status mismatches fail;
- the gate validates TE-2 itself prospectively for `targetStatus=done`;
- the named CI check is required by branch protection;
- the runbook and README command match the shipped CLI;
- `git diff --check` is clean.

### 6.3 Suggested verification commands

The implementation story may refine project paths, but it must retain equivalent machine evidence:

```bash
dotnet build Hexalith.ChatBot.slnx --configuration Release
dotnet test tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/Hexalith.ChatBot.StoryEvidenceGate.Tests.csproj --configuration Release --logger "trx;LogFileName=story-evidence-gate.trx" --results-directory TestResults/story-evidence-gate
dotnet run --project tools/Hexalith.ChatBot.StoryEvidenceGate/Hexalith.ChatBot.StoryEvidenceGate.csproj -- validate --story <story-path> --contract <evidence-contract> --target-status done --base <base-sha> --head <head-sha> --results <results-root>
git diff --check
```

No passing count is prescribed in planning. The implementation record must report the counts parsed from the actual machine output.

### 6.4 Success and rollback criteria

**Success:** no protected-branch change can introduce a new story/sprint `done` transition without a passing exact-scope evidence report; every enumerated negative case demonstrably fails.

**Rollback trigger:** if the gate blocks unrelated changes because its story grammar or diff scope is ambiguous, leave affected stories in `review`, disable only the faulty transition-detection rule through an explicit time-bounded change, and repair TE-2. Do not convert a primary-path requirement into a warning and do not mark stories `done` manually.

### 6.5 Approval and routing record

- **Decision:** approved by Administrator on 2026-08-03.
- **Final scope:** moderate delivery-governance correction.
- **Primary handoff:** Product Owner and Amelia for the planning hold, TE-2 backlog organization, and implementation.
- **Supporting handoff:** Murat for evidence/primary-path policy and negative validation; Winston for architecture/provenance review; CI owner for the required check and protected-branch activation.
- **Workflow artifact changed:** this finalized Sprint Change Proposal only.
- **Not yet changed:** `technical-enablers.md`, `epics.md`, `architecture.md`, planning index, `sprint-status.yaml`, developer docs, solution/tooling, tests, CI/release configuration, product code, and branch protection.
- **Completion boundary:** approval authorizes the planning changes and implementation handoff; it does not mark TE-2 or the open sprint action complete.
