---
title: Sprint Change Proposal - Reusable Domain-Module CI/CD Alignment
project: chatbot
date: 2026-07-18
status: approved
mode: Incremental
trigger: "Align ChatBot CI/CD with the Hexalith domain-module pattern, using Tenants or Parties as implementation evidence, and verify best-practice and reusable-workflow-policy compliance"
scope_classification: Moderate
recommended_approach: Direct Adjustment through Epic 1 Story 1.1f and shared Hexalith.Builds corrections
owner: Jerome
prepared_by: Correct Course workflow
approved_by: Jerome
approved_at: 2026-07-18
implementation_state: planning-updates-applied-implementation-not-started
handoff_status: routed-to-product-owner-developer-builds-maintainer-devops-and-domain-owners
incremental_approvals:
  - "Proposal 1 - Epic 1 Story 1.1f"
  - "Proposal 2 - Architecture invariant"
  - "Proposal 3 - Shared reusable workflows and policy"
  - "Proposal 4 - Reusable browser-test tier"
  - "Proposal 5 - ChatBot workflow callers"
  - "Proposal 6 - Supporting build, release, and test contract"
  - "Proposal 7 - Sprint tracking and implementation handoff"
---

# Sprint Change Proposal - Reusable Domain-Module CI/CD Alignment

## 1. Issue Summary

### Trigger

Jerome requested that Hexalith.ChatBot CI/CD align with the established Hexalith domain-module CI/CD pattern,
using Hexalith.Tenants or Hexalith.Parties as implementation evidence. The resulting pipeline must comply with
CI/CD best practices and the reusable-workflow policy, and the common pipeline skeleton must remain consistent
across Hexalith domain modules.

### Issue type

This is a stakeholder-directed build-governance correction and a failed-standardization correction discovered
after the original CI/release scaffold was completed. It does not change product behavior or MVP scope.

### Problem statement

ChatBot still owns bespoke build, topology, and release mechanics. Its workflows duplicate capabilities now
owned by the reusable `Hexalith.Builds` domain workflows, omit current supply-chain gates, run tests at solution
level, and allow required runtime/browser evidence to become vacuous. The release workflow also runs directly on
push and is not bound to successful CI.

The current `Hexalith.Builds` release pattern contains one shared correctness gap: `workflow_run` releases check
out without an explicit `ref`. GitHub documents the `workflow_run` event's `GITHUB_SHA` as the latest commit on
the default branch. The release must therefore select and assert `github.event.workflow_run.head_sha` to prove it
publishes the commit whose CI succeeded.

### Evidence collected

| Evidence | Finding |
| --- | --- |
| ChatBot `.github/workflows/ci.yml` | Direct checkout/setup/build workflow; floating third-party action tags; solution-level `dotnet test`; duplicated setup in the topology job. |
| ChatBot `.github/workflows/release.yml` | Push-triggered direct semantic-release; duplicates topology acceptance; broad workflow-level write permissions; no CI-success dependency. |
| ChatBot build properties | `NuGetAudit=false`; cross-repository dependencies are unconditional project references. |
| ChatBot release tooling | `.releaserc.json` creates GitHub releases only; `package.json`, `package-lock.json`, and consumer-package validation scripts are absent. |
| ChatBot security automation | CodeQL, dependency review, commitlint, and Dependabot are absent. |
| ChatBot runtime evidence | The required Aspire test self-skips without bespoke environment variables. UI E2E tests contain browser-unavailable skip paths. |
| Hexalith.Tenants | Uses `domain-ci.yml@main`; release waits for successful push CI; non-cancelling release concurrency; job-scoped write permissions; explicit release secrets. This is the canonical caller. |
| Hexalith.Parties | CI uses the reusable workflow, but release remains push-triggered, duplicates tests, grants top-level write permissions, and uses `secrets: inherit`. Its release is migration evidence, not the target pattern. |
| `Hexalith.Builds` policy | Requires reusable workflows, project-level tests, Release builds, non-recursive root submodules, explicit release secrets, security scanning, commitlint, enabled NuGet audit, and the standard `workflow_run` release gate. |
| Shared `domain-release.yml` | Checkout has no explicit `ref`, so its implementation does not yet enforce the policy's tested-head claim. |
| Shared `domain-ci.yml` | Aspire TRX is uploaded, but missing/zero/all-skipped execution is not rejected; no optional browser tier exists. |
| Workspace preservation | The parent records `Hexalith.Builds` at `8a3c5ba`, while the clean checked-out submodule is at `a8933ae`. This proposal does not reset or update that user-owned submodule state. |

### External best-practice verification

- GitHub identifies full-length commit SHA pinning as the only immutable action reference and recommends
  least-privilege token permissions:
  <https://docs.github.com/en/actions/reference/security/secure-use>.
- Reusable workflows accept explicit inputs and named secrets; inherited secrets expose the caller's complete
  available secret set:
  <https://docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows>.
- GitHub documents `workflow_run` `GITHUB_SHA`/`GITHUB_REF` as the latest default-branch commit/default branch,
  not the triggering workflow's tested head:
  <https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows>.
- GitHub recommends granting `GITHUB_TOKEN` only the minimum required permissions:
  <https://docs.github.com/en/actions/tutorials/authenticate-with-github_token>.

## 2. Correct Course Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | Done | Completed Story 1.1d owns the historical CI/release scaffold. The stakeholder correction reveals that the scaffold no longer satisfies the shared domain-module contract. |
| 1.2 Core problem | Done | New stakeholder requirement plus failed standardization: bespoke ChatBot mechanics diverge from the centrally owned pipeline. |
| 1.3 Evidence | Done | ChatBot, Tenants, Parties, Hexalith.Builds policy/workflows, planning artifacts, build files, test guards, and official GitHub guidance were inspected. |
| 2.1 Current epic | Done | Epic 1 remains viable. Preserve completed Story 1.1d and add corrective Story 1.1f. |
| 2.2 Epic-level change | Done | Add Story 1.1f under the scaffold work package; no new product epic is required. |
| 2.3 Remaining epics | Done | Product stories remain valid. Their release evidence will consume the stronger pipeline. |
| 2.4 Future epic validity | Done | No product epic becomes obsolete and no product capability is added or removed. |
| 2.5 Priority/order | Done | Complete approved Story 1.1e first, then Story 1.1f. Shared Builds prerequisites precede consumer rollout. |
| 3.1 PRD conflict | N/A | Actors, journeys, functional requirements, NFR outcomes, and MVP scope are unchanged. |
| 3.2 Architecture conflict | Done | Added the approved binding reusable-pipeline, package-mode, exact-tested-SHA, security, secret, and evidence contract. |
| 3.3 UI/UX conflict | N/A | No user surface, interaction, accessibility, responsive, redaction, or localization contract changes. Browser CI only verifies the existing UX contract. |
| 3.4 Other artifacts | Done | Decision log and sprint tracking are updated; shared workflows/policy and implementation artifacts are routed through Story 1.1f. |
| 4.1 Direct Adjustment | Viable | Medium effort, medium-high release risk. Existing architecture and product scope remain intact. |
| 4.2 Potential rollback | Not viable | Reverting delivered product work does not create a reusable or secure pipeline. |
| 4.3 PRD MVP review | Not viable | MVP remains achievable and unchanged. |
| 4.4 Recommended path | Done | Direct Adjustment through shared Builds corrections followed by Story 1.1f. |
| 5.1 Issue summary | Done | Section 1. |
| 5.2 Epic/artifact impact | Done | Sections 3 and 5. |
| 5.3 Recommended path | Done | Section 4. |
| 5.4 MVP impact/action plan | Done | No MVP impact; Sections 6 and 7 define execution and validation. |
| 5.5 Handoff | Done | Section 8. |
| 6.1 Checklist completion | Done | All applicable analysis items are resolved or represented as implementation work. |
| 6.2 Proposal accuracy | Done | Findings are grounded in the checked-out files and current shared policy. |
| 6.3 Explicit user approval | Done | Jerome approved the assembled proposal and implementation handoff on 2026-07-18. |
| 6.4 Sprint-status update | Done | Story 1.1f is tracked as backlog after the in-progress Story 1.1e prerequisite. |
| 6.5 Next steps/handoff | Done | Ownership, sequencing, risks, and success criteria are explicit. |

## 3. Impact Analysis

### 3.1 Product and MVP impact

- No PRD requirement changes.
- No user journey, actor, UI surface, command/event contract, or data-model change.
- No MVP capability is added, removed, or deferred.
- Existing safety, audit, authorization, isolation, and accessibility gates become more credible because their CI
  evidence must execute non-vacuously.

### 3.2 Epic and story impact

- Epic 1 remains `in-progress`.
- Completed Story 1.1d remains unchanged historical evidence.
- Backlog Story 1.1e remains the prerequisite for sole shared package-version authority.
- New Story 1.1f owns reusable pipeline adoption and supporting release/test corrections.
- Other product epics require no edits.

### 3.3 Architecture impact

Architecture must state that all domain modules call the same reusable domain workflows, while module-specific
differences remain inputs. It must also define:

1. Debug/project-reference local development versus Release/NuGet-reference CI and release.
2. Project-level test execution and non-vacuous required lanes.
3. Successful-CI and exact-tested-SHA release binding.
4. Job-scoped permissions, explicit secrets, queued release concurrency, and declared artifacts.
5. Required supply-chain gates and the policy-owned `Hexalith.Builds@main` exception.
6. Shared behavior ownership in `Hexalith.Builds`, with documented module-specific operational exceptions only.

### 3.4 Technical and operational impact

| Area | Impact |
| --- | --- |
| Shared CI | Add generic Aspire execution validation and optional Playwright browser tier. |
| Shared release | Select/assert the successful CI run's head SHA before publishing. |
| ChatBot CI | Replace direct steps with `domain-ci.yml@main` and declared test-tier inputs. |
| ChatBot release | Replace direct semantic-release with successful-CI-triggered `domain-release.yml@main`. |
| Dependencies | Use NuGet references for cross-repository libraries in CI/release; preserve project-reference local development. |
| Packages | Pack/validate/publish Contracts, Client, and Testing. |
| Containers | Publish Server and UI through .NET SDK container support. |
| Supply chain | Enable NuGet audit; add CodeQL, dependency review, commitlint, Dependabot, locked npm dependencies, and signature verification. |
| Runtime evidence | Required Aspire and browser proofs cannot pass through missing, zero, or skipped execution. |
| Secrets | Repository/organization administrators must provision NuGet and Zot credentials explicitly. |
| Other modules | Tenants remains the canonical caller; Parties release and other domain modules enter a conformance rollout. |

### 3.5 Risk assessment

| Risk | Level | Mitigation |
| --- | --- | --- |
| Publishing a commit not validated by CI | High before correction | Bind checkout and pre-publication assertion to `workflow_run.head_sha`. |
| First real NuGet/container publication exposes packaging defects | Medium-high | Validate packages and container mappings in CI before enabling publication. |
| Cross-repository package mode reveals missing packages or incompatibilities | Medium-high | Complete Story 1.1e first; validate consumer packages and package-mode builds. |
| Required tests previously skipped | Medium | Install deterministic dependencies and reject vacuous evidence. |
| Shared workflow regression affects multiple modules | Medium-high | Fixture-test shared changes; validate Tenants, Parties, and ChatBot callers before rollout. |
| Missing repository secrets blocks release | Medium | DevOps preflight with explicit secret inventory; fail before publication. |
| Mutable `Hexalith.Builds@main` changes consumer behavior | Accepted policy exception | Restrict exception to Hexalith-owned shared workflows, review shared changes, and keep third-party actions full-SHA pinned. |

## 4. Recommended Approach

### Option 1 - Direct Adjustment (recommended)

Correct the shared workflows first, complete package-authority Story 1.1e, then implement Story 1.1f in ChatBot
and roll the caller pattern across domain modules.

- **Effort:** Medium.
- **Risk:** Medium-high during first publication, then lower ongoing operational risk.
- **Timeline impact:** One shared-workflow change, one ChatBot corrective story, DevOps credential setup, and a
  domain-module caller conformance sweep. No product epic resequencing.
- **Benefits:** Central ownership, less duplication, exact tested-artifact provenance, reproducible browser/runtime
  evidence, and uniform supply-chain controls.

### Option 2 - Roll back recent work

**Rejected.** No delivered product work caused the divergence. Rollback would not produce shared workflows,
package-mode CI, or secured release behavior.

### Option 3 - Reduce or redefine the MVP

**Rejected.** The problem is delivery governance, not product feasibility. Reducing product scope would not fix
pipeline drift.

## 5. Detailed Change Proposals

### 5.1 Epics - add Story 1.1f

**OLD:** Completed Story 1.1d owns historical scaffold work; no current story owns reusable CI/CD alignment.

**NEW:** Add after Story 1.1e:

#### Story 1.1f: Standardize reusable domain-module CI/CD and release gates

As a platform engineer,
I want ChatBot CI/CD to use the shared Hexalith domain-module workflows,
So that every domain module follows the same secure, maintainable build and release contract.

**Acceptance Criteria:**

- CI calls `Hexalith.Builds/.github/workflows/domain-ci.yml@main`, with only module-specific solution,
  test-tier, coverage, and operational inputs.
- Release calls `domain-release.yml@main` only after a successful push-triggered CI run on `main`.
- Release checks out and publishes the exact `workflow_run.head_sha` validated by CI.
- CI builds Release with warnings as errors, uses NuGet dependencies for cross-repository libraries, and tests
  projects individually.
- Required Aspire/Dapr topology and browser tests execute and cannot pass through self-skip, zero-test, or
  all-skipped results.
- Release does not duplicate CI tests, uses non-cancelling concurrency, scopes write permissions to the release
  job, and maps secrets explicitly.
- NuGet packages and SDK-container images are validated and published from the declared ChatBot inventory.
- NuGet auditing remains enabled; individual advisories use targeted suppression.
- CodeQL, dependency review, commitlint, and Dependabot match the shared module pattern.
- Third-party actions are full-SHA pinned inside shared workflows; Hexalith.Builds reusable references follow the
  policy-mandated `@main` exception.
- Only root-declared submodules are initialized, non-recursively.
- Workflow validation proves triggers, permissions, concurrency, inputs, exact-SHA release binding, test
  execution, artifact retention, and secret boundaries.

### 5.2 Architecture - add the domain-module CI/CD invariant

**OLD:** Architecture lists `ci.yml`/`release.yml` and “semantic-release on merge to main” without defining the
reusable pipeline, dependency mode, release provenance, permissions, secrets, or supply-chain gates.

**NEW:** Add the binding invariant described in Section 3.3, including ChatBot's release inventory:

- NuGet: `Hexalith.ChatBot.Contracts`, `Hexalith.ChatBot.Client`, `Hexalith.ChatBot.Testing`.
- Containers: `hexalith-chatbot-server`, `hexalith-chatbot-ui`.

### 5.3 Hexalith.Builds - correct reusable workflow semantics

**OLD:** Release checkout is not bound to the triggering CI SHA; required Aspire evidence can be vacuous; no
reusable browser tier exists.

**NEW:**

1. In `domain-release.yml`, checkout `github.event.workflow_run.head_sha || github.sha`, disable persisted
   credentials, assert the selected SHA, and record it in release evidence.
2. In `domain-ci.yml`, disable persisted checkout credentials, validate Aspire TRX execution, and add an optional
   project-pinned Playwright Chromium tier with TRX/evidence guards.
3. In `ci-cd-standards.md`, require exact tested-head publication and non-vacuous blocking lanes; identify Tenants
   as canonical and Parties release as migration-required.

### 5.4 ChatBot workflow callers

**OLD:** Bespoke `ci.yml` and `release.yml`; no security workflows.

**NEW:**

- `ci.yml`: push/PR on `main`, cancelling concurrency, `contents: read`, reusable domain CI, individual Tier 1
  projects, required ChatBot Aspire test, browser project, consumer validation, and blocking runtime evidence.
- `release.yml`: successful push-CI `workflow_run` on `main`, non-cancelling concurrency, job-scoped writes,
  reusable domain release, no duplicated tests, two containers, and explicit secrets.
- Add reusable CodeQL, dependency-review, and commitlint callers.
- Add Dependabot for NuGet, npm, and GitHub Actions with `build(deps)`/`ci(deps)` prefixes.

### 5.5 Supporting build, release, and test artifacts

**OLD:** NuGet audit disabled; source dependencies unconditional; no locked npm toolchain or package scripts;
required runtime/browser tests may skip.

**NEW:**

- Enable NuGet audit and all-mode scanning with advisory-code handling defined by shared policy.
- Introduce local project-reference versus CI/release NuGet-reference dependency mode.
- Add locked semantic-release/commitlint dependencies.
- Configure semantic-release to build, pack, validate, publish, attach packages, and invoke shared container
  publication.
- Add ChatBot package inventory, consumer validation, and release-secret validation scripts.
- Treat GitHub Actions as a required Aspire environment and enable strict browser evidence.
- Add conformance tests for dependency mode, audit settings, release inventory, locked tooling, non-vacuous gates,
  and removal of bespoke mechanics.

### 5.6 Sprint tracking

After final approval, add:

```yaml
1-1f-standardize-reusable-domain-module-ci-cd-and-release-gates: backlog
```

Keep `epic-1: in-progress`, retain Story 1.1d as done, and leave Story 1.1e as the current in-progress
prerequisite.

### 5.7 PRD and UX

No changes. The complete PRD, architecture, epic set, and indexed UX handoff were assessed. The correction changes
delivery evidence only and does not modify product or user-experience contracts.

## 6. Implementation Sequence

1. Update and fixture-test `Hexalith.Builds` reusable workflows and standards.
2. Validate the shared changes against Tenants and a corrected Parties caller fixture.
3. Complete approved Story 1.1e central package-version migration.
4. Add ChatBot package-mode dependencies, enabled audit, locked release tooling, and validation scripts.
5. Make ChatBot Aspire/browser evidence strict and non-vacuous.
6. Replace ChatBot bespoke CI/release with reusable callers and add security workflows.
7. Provision repository secrets, variables, workflow access, branch protections, and required checks.
8. Run local/package validation, then actual GitHub CI and a controlled release dry run.
9. Migrate Parties release and run the caller conformance sweep across remaining domain modules.

## 7. Validation and Success Criteria

### Static and local validation

- Workflow syntax/action validation passes for all changed YAML.
- `npm ci`, `npm audit signatures`, and commitlint pass using the locked repository toolchain.
- Root-declared submodule initialization remains non-recursive.
- Release configuration resolves exactly the three NuGet packages and two container projects.
- Package-mode restore/build succeeds in Release with warnings as errors and enabled NuGet audit.
- Consumer package validation passes for Contracts, Client, and Testing.
- Each declared test project runs individually; results are uploaded per tier.

### GitHub Actions validation

- CI callers expose only module-specific inputs and least-privilege permissions.
- Required Aspire TRX proves the governed-command topology test executed and passed.
- Required browser TRX proves live Playwright conformance executed and passed.
- CodeQL, dependency review, and commitlint are required checks where applicable.
- Release starts only from successful push CI on `main`.
- Release checkout/assertion SHA equals `github.event.workflow_run.head_sha`.
- Release does not rerun CI test tiers.
- Release publishes exactly the declared packages/images and records their hashes and source SHA.
- No nested submodule initialization, broad secret inheritance, solution-level test, disabled NuGet audit, or
  floating third-party action reference remains.

## 8. Implementation Handoff

### Scope classification

**Moderate.** The change requires backlog tracking and coordination across Product Owner/Developer,
Hexalith.Builds maintainer, DevOps/repository administration, and domain-module owners. It does not require a
product or architecture replan.

### Recipients and responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner / Developer | Track Story 1.1f after 1.1e; preserve product scope and completed Story 1.1d history. |
| Hexalith.Builds maintainer | Implement exact-SHA release, non-vacuous evidence guards, browser tier, documentation, and shared fixtures. |
| ChatBot developer | Implement dependency mode, release tooling, validation scripts, test strictness, reusable callers, and conformance tests. |
| DevOps / repository administrator | Provision explicit secrets/variables, reusable-workflow access, required checks, and branch protection. |
| Parties maintainer | Replace the stale release caller with the canonical workflow-run pattern. |
| Domain-module owners | Run the common caller conformance sweep and remove remaining bespoke standard mechanics. |

### Handoff gate

Jerome approved the assembled proposal on 2026-07-18. Planning and sprint artifacts are updated, and
implementation is authorized to proceed through the routed owners in the sequence defined in Section 6.

## 9. Approval State

- Incremental edit proposals 1-7: **approved by Jerome**.
- Assembled Sprint Change Proposal: **approved by Jerome on 2026-07-18**.
- Planning updates: **applied**.
- Implementation handoff: **authorized and routed; implementation not started by this Correct Course workflow**.

