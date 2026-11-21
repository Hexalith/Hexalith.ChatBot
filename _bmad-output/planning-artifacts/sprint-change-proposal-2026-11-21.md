---
title: Sprint Change Proposal — Story Completion Without TE-2, Story 1.5 Definition of Done, and CiCd Adoption
project: Hexalith.ChatBot
created: "2026-11-21"
status: approved
decision: approved
approvedAt: "2026-11-21"
handoffStatus: routed
workflowMode: incremental
changeClassification: moderate
decisions:
  a: A1 — retire TE-2; reviewer-approved Story Completion policy
  b: B1 — Story 1.5 done = contracts landed and proven to fail closed; live acceptance moves to new Story 1.12
  c: C1 — one complete-perimeter Hexalith.CiCd adoption request
handoffRecipients:
  - Developer (Amelia)
  - Test Architect (Murat)
  - Hexalith.CiCd maintainers (request drafted, not filed)
---

# Sprint Change Proposal — Story Completion Without TE-2, Story 1.5 Definition of Done, and CiCd Adoption

## 1. Issue Summary

Story 1.5 (`spec-1-5-establish-stable-operation-identity-and-concurrency-contracts.md`) has no code findings left. Round 22 produced 0 patches, and the R22 build blockers are fixed and pushed (`d999082`, `8b2bea5`, `e51e608`, `bd61641`). The Release and source-mapped Debug solution builds have 0 errors. Release Server passes 3632/3632 and UI.Tests 613/613. Architecture passes 118/119, failing only on Folders wrapper drift. The story still cannot reach `done`, for three reasons:

1. **No completion-evidence producer (R21-D1).** `bf0f44f` deleted the TE-2 StoryEvidenceGate tool, `story-evidence-policy.json` and `.github/scripts/*` (59 files, about 13.9k lines). The Hexalith baseline (`references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, CI/CD section) now says that checks of story evidence do not belong in module CI. That contradicts `docs/story-evidence-integrity.md`, `architecture.md` §Delivery and Evidence Integrity and epics ARCH-37.
2. **No CI lane runs Server.Tests (R22-W2).** The legacy `ci.yml` and `release.yml` call the deleted scripts. The repository has no `.github/hexalith-ci.json`, so it has not adopted Hexalith.CiCd, and CiCd owns `.github/workflows/**`.
3. **A6, A13 and the retry profile are open.** AC5 and the Verification section require live canonical execution to fail closed, but the live recovery lane cannot pass while they are open (R17-W5).

Issue type: a failed approach (the module-CI evidence gate) plus a platform-policy change (CiCd ownership and the baseline's story-evidence exclusion).

### Evidence: CI on `bd61641`

Every red step runs a script that `bf0f44f` deleted, or a cleanup step that follows one. No product code fails.

| Run | Job | Failure |
|---|---|---|
| ci [44897519125](https://github.com/Hexalith/Hexalith.ChatBot/actions/runs/44897519125) | `build` | Restore and Build pass: 0 errors, 7 warnings, 4m12s. This is the first run since the migration to get past Build, so the BL0016 fix holds. The step "Test every xUnit project independently" (`ci.yml:119`) then fails: `run-merge-test-lanes.sh: No such file or directory`, exit 127. |
| | `story-evidence-integrity` | "Build Release configuration" (`ci.yml:173`): `build-release-configuration.sh` is missing, exit 127. "Run gate self-tests": `run-xunit-v4.sh` is missing, exit 127. |
| | topology, scheduled and PR jobs | skipped |
| release [44897518989](https://github.com/Hexalith/Hexalith.ChatBot/actions/runs/44897518989) | topology acceptance | "Build Release configuration" (`release.yml:60`) exits 127. "Stop DAPR runtime" then fails with `dapr: command not found`. |
| | live recovery sweep | `release.yml:168` exits 127. `project-recovery-results.sh` is missing (`:219`). The DAPR stop step fails the same way. |
| | evidence gate | `release.yml:269` exits 127. `semantic-release` is skipped. |

All 7 build warnings are CS0618 (`AddProject` is obsolete). Six are in `references/Hexalith.Platform` source, which the Release package-mode build compiles; the seventh is in `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoverySandboxTopologyComposer.cs:55`. ChatBot lists CS0618 in `WarningsNotAsErrors` (`Directory.Build.props:29`). The shared CI builds with `-warnaserror`, and whether that exemption survives the switch has not been verified.

## 2. Impact Analysis

### 2.1 Product and PRD

None. TE-2 never had product impact; it was a delivery-integrity control. The MVP scope is unchanged.

### 2.2 Epics and stories

- **Epic 1.**
  - Stories 1.5 and 1.6 are both parked at `review` on the same missing gate. Decision (a) applies to both. Story 1.6's own definition of done is out of scope here.
  - Decision (b) completes 1.5 against its frozen intent, which already says "validate with synthetic evidence while live owner/persistence acceptance remains gated".
  - A new Story 1.12 takes the live acceptance. It depends on 1.6 (A6) and 1.8 (A13).
- **Epic 13 action items.** Item 1 ("add a mechanical story-evidence integrity gate") is superseded. Item 2 ("primary-path evidence mandatory for runtime claims") is delivered as Story Completion criterion 4. Item 7 is already `done`.
- **Future epics.** No change in order or priority. Every future story completes under the Story Completion policy.

### 2.3 Artifact conflicts

| Artifact | Conflict |
|---|---|
| `docs/story-evidence-integrity.md` | The whole document is the TE-2 runbook for deleted tooling. |
| `architecture.md` §Delivery and Evidence Integrity | Five bullets bind `done` and the recovery evidence to TE-2: Shared ownership, Story status, Completion authority, Recovery diagnostics and Recovery completion. A sixth, Release provenance, describes the legacy repo-owned release workflow. |
| `epics.md` ARCH-37 | Requires "story-evidence integrity" from CI/release. |
| `technical-enablers.md` TE-2 | `review`, with TE-2.5 `in-progress`. |
| ADRs | `isolated-policy-snapshots.md:146-148` requires a passing TE-2 contract. `live-recovery-validation-drivers.md:144-152,288` defines the `recovery-primary` completion lane. |
| `_bmad-output/implementation-artifacts/evidence/` | 11 contracts and their reports. These remain frozen history. |
| `deferred-work.md` | About 25 open entries (rough regex count) cite TE-2, the legacy workflows or deleted guards. That is R22-W5's scope. |
| UX | Not applicable. |

### 2.4 Technical

- CI stays red on every push until Hexalith.CiCd delivers generated callers, and no ChatBot release can run until then.
- Server.Tests and all other rows are protected only by local runs (R22-W2).
- The shared CI classifies Recovery.Tests and Release.Tests as unit projects (R21-W4), so the registration must exclude both to the `nightly` hook.

## 3. Options Considered and Recommended Approach

### (a) How stories reach `done`

- **A1, chosen: retire TE-2.** A story completes through a short, reviewer-approved completion record. *Pros:* it matches the baseline, has no tooling to maintain, unblocks 1.5 and 1.6, and leaves the non-vacuous checks to product CI (`failSkips`, `minimumExpectedTests`). *Cons:* the digest-bound mechanical reconciliation is gone, so completion depends on review discipline. Criterion 4 and product CI's skip and test-count gates are the mitigation.
- **A2: restore the gate as a local preflight.** *Cons:* a large module-owned tool, with self-attested provenance, no `recovery-primary` producer and stale pins. The 1.7 recipe also showed that transition-only pushes never triggered primary lanes.
- **A3: rehome the gate to shared tooling.** *Cons:* a cross-repo project with no other consumer, which would delay 1.5 and 1.6 indefinitely. Builds' `hexalith-evidence` validates readiness matrices only.

### (b) Story 1.5's definition of done

- **B1, chosen: 1.5 is done when the contracts have landed and are proven to fail closed.**
  - In-process proof: Release Server, Contracts, Client, CLI and Conformance lanes.
  - Live proof: a Tier-3 run of `TrivialGovernedCommandAspireE2eTests`. That test asserts the live `503 canonical-execution-unavailable` refusal (`:597`).
  - The live A6/A13 acceptance, the retry profile, the three owner decisions and R17-W5 move to the new Story 1.12.
  - *Cons:* 1.5 counts as done while live canonical execution is still closed. That state is recorded in 1.12.
- **B2: stay in `review` until A6/A13 close.** *Cons:* those are external approvals with no date, and the gate would exceed the frozen intent.
- **B3: B1, with the live acceptance folded into Story 1.8's ACs.** *Cons:* 1.8 grows, against the fine-grained story preference.

### (c) The CI path

- **C1, chosen: one complete-perimeter Hexalith.CiCd adoption request** (§4.9). It closes R21-D1's workflow half, R22-W2, R21-W1 and R21-W4 in one delivery. *Cons:* nothing changes until the ChatBot preparation has landed, and the first hosted Tier-3 run is unproven.
- **C2: staged request, push CI first and nightly later.** *Cons:* the exclusions would need a manual placeholder job, and it takes more CiCd round-trips.
- **C3: delete the legacy workflows only.** *Cons:* no push CI at all, while the baseline keeps the existing gates until adoption.

**Path:** direct adjustment, with no rollback and no MVP change.
- **Effort:** one records/doc session for §4.1–§4.8; two to three sessions of ChatBot CiCd preparation; one session to complete Story 1.5.
- **Risk:** medium. Losing the mechanical gate matters, the CiCd timeline is external, and the CS0618 exemption under `-warnaserror` is unverified.

## 4. Detailed Change Proposals

Each item below was approved individually (incremental mode). Line endings: files that are all CRLF get byte-level edits. `docs/story-evidence-integrity.md` is rewritten as CRLF per `.editorconfig`. The LF planning artifacts keep LF.

### 4.1 `docs/story-evidence-integrity.md` — rewrite in place as the Story Completion policy

The 182-line TE-2 runbook (preflight, contract grammar, digest scope, TRX and provenance, primary lanes, input limits, reason codes) is replaced by:

```markdown
# Story Completion

This policy defines how a ChatBot story or technical-enabler item reaches `done`. It replaces
the TE-2 story-evidence integrity gate, retired on 2026-11-21 by
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-21.md`.

## Lifecycle

- Development ends with the spec at `review` (or `in-review`) and the sprint entry at `review`.
  Review rounds keep that status.
- `done` is a separate, records-only commit. It changes the spec status, the sprint entry and the
  spec's `## Completion` section, and nothing else. Product changes ship in earlier commits.
- An agent may prepare the completion record, but it never commits a `done` transition without
  the maintainer's explicit approval for that story.

## Completion criteria

A story may move to `done` only when all of these hold:

1. **Review is closed.** The latest code-review round has no open patch or decision item, and
   every deferral has an owner and a closure condition in
   `_bmad-output/implementation-artifacts/deferred-work.md`.
2. **Acceptance is mapped.** The spec's `## Completion` section names the exact commit being
   completed and maps every acceptance criterion to the tests that prove it (project, class and
   method).
3. **Product CI passed.** The module's required CI succeeded on that commit, cited by run link.
   Until ChatBot adopts Hexalith.CiCd, run each affected test project individually from a Release
   package-mode build, and record the command, commit and passed/failed/skipped counts.
4. **Live claims ran live.** An acceptance criterion that claims browser, SignalR, hosting/asset,
   Aspire/Dapr or recovery behavior cites a non-skipped run of the named live test: either a CI
   run link, or a local Tier-3 run with its command and commit. Fixtures, fallbacks, screenshots
   and unrelated suites never satisfy a live claim. If the required live path cannot run, the
   story stays in `review`.
5. **Scope gaps are explicit.** Anything the story deliberately leaves open, such as owner
   approvals or external acceptance, is named in the Completion section together with the story
   that now owns it.

Do not commit TRX files or bulk diagnostics. Keep the Completion section short: record decisions
and current state, and leave run-by-run history to commits and CI run links.

## History

TE-2 (`sprint-change-proposal-2026-08-03.md`) reconciled story status, File Lists, scoped diffs,
TRX provenance and primary-path lanes mechanically in module CI. `bf0f44f` removed its tool,
policy and scripts during the shared-CI migration, and the Hexalith baseline keeps story-evidence
checks out of module CI. The evidence contracts and reports under
`_bmad-output/implementation-artifacts/evidence/` remain as frozen history for the stories they
completed. No new contracts are created.
```

### 4.2 `technical-enablers.md` — retire TE-2

- Front matter: `updated: 2026-08-03` → `updated: 2026-11-21`.
- TE-2 **Status:** `review; implementation self-validates prospectively, but completion remains blocked on the required protected check.` → `retired 2026-11-21 (sprint-change-proposal-2026-11-21.md). bf0f44f removed the gate tool, policy and scripts during the shared-CI migration, and the Hexalith baseline keeps story-evidence checks out of module CI. Stories now complete under the Story Completion policy in docs/story-evidence-integrity.md.`
- **Invariant** → **Former invariant**, in the past tense.
- TE-2.5 status: `in-progress — repository work complete; external required-check activation remains open` → `withdrawn — the required branch check was never activated; retired with TE-2`.
- `### Completion evidence rule` and its paragraph → `### Retirement`: "TE-2 never reached `complete`. TE-2.1 through TE-2.4 stay recorded as built; the code is in history before `bf0f44f`. Its evidence contracts and reports under `../implementation-artifacts/evidence/` remain frozen history and are not re-validated. The Epic 13 action items that named this gate are closed by `sprint-change-proposal-2026-11-21.md`."
- TE-1 and TE-3 are unchanged.

### 4.3 `architecture.md` §Delivery and Evidence Integrity

- **Shared ownership** (replaced): "Hexalith.CiCd owns module workflows, `.github/hexalith-ci.json`, Dependabot and the Builds gitlink, and renders the callers of the shared `Hexalith.Builds` CI and release implementation. Module-specific product checks live in product actions that CiCd enables (`.github/actions/after-unit-tests`, `.github/actions/nightly`). Modules neither hand-edit workflows nor duplicate standard build, test or release mechanics."
- **Story status** (replaced; the **Completion authority** bullet is deleted): "A story reaches `done` under the Story Completion policy (`docs/story-evidence-integrity.md`). That requires a closed review, an acceptance-to-test map bound to the completed commit, green required product CI on that commit, a non-skipped run of the named live test for every live claim, an owner for every deliberate scope gap, and maintainer approval. Fixtures, fallbacks, screenshots and unrelated suites never satisfy a live claim. Story-evidence checks do not run in module CI. TE-2 was retired on 2026-11-21."
- **Recovery diagnostics** and **Recovery completion** are merged into **Recovery sweep**: "The destructive live-recovery sweep (`Hexalith.ChatBot.Recovery.Tests`) runs only in the product `nightly` hook. Raw live TRX stays outside retention, and published reports and manifests are sanitized."
- **A10 operational evidence**, last sentence: "They can inform A10 but cannot satisfy story completion." → "They can inform A10. A story's live recovery claim cites the sweep run that executed the named test."
- **Release provenance**, ChatBot-exception sentence → "ChatBot's legacy repository-owned release workflow is retired when Hexalith.CiCd delivers its generated callers (`sprint-change-proposal-2026-11-21.md`, C1). ChatBot then follows the central nightly release, and its positive-state Aspire/Dapr gates and weekly recovery sweep run in the product `nightly` hook. Until that delivery, no ChatBot release can run."
- Dependency modes, Non-vacuous gates, Activation, Security boundary and Release inventory are unchanged.

### 4.4 `epics.md` — ARCH-37 and the new Story 1.12

- **ARCH-37:** "… non-vacuous topology/browser gates, exact source/evidence provenance, story-evidence integrity, and checkout-root-only non-recursive submodule initialization." → "… non-vacuous topology/browser gates, exact source provenance, and checkout-root-only non-recursive submodule initialization. Story completion follows the Story Completion policy (docs/story-evidence-integrity.md), outside module CI."
- Front matter: `storyCount: 146` → `147`.
- Insert after Story 1.11:

```markdown
### Story 1.12: Accept Live Canonical Operation Execution

As an authorized user submitting governed work,
I want canonical operations to execute live once their data-protection and owner-execution contracts are accepted,
So that the operation identity, concurrency, and status contracts hold in persisted owner state, not only in synthetic folds.

**Acceptance Criteria:**

**Given** A6 or A13 acceptance is missing, or the retry profile is unapproved
**When** a canonical operation is requested in the live runtime
**Then** it fails closed exactly as Story 1.5 established
**And** no configuration, fixture, or caller switch can open canonical execution.

**Given** the accepted A6 policy-snapshot data class (Story 1.6) and the accepted A13 owner-dispatched write target (Story 1.8)
**When** a canonical operation is created, replayed, raced, and polled in the supported Aspire/Dapr topology
**Then** its lifetime operation ID, immutable metadata, expected-revision guard, and monotonic status hold in the persisted owner state
**And** assertions inspect the state-store and event-stream end state rather than only responses.

**Given** the owner decisions on multi-event revision semantics, accepted numeric equivalence, and successor uniqueness
**When** each decision is recorded
**Then** the reducer, status-update policy, and OpenAPI contract implement it with regression coverage
**And** every still-undecided case keeps failing closed.

**Given** an approved retry profile
**When** an eligible pre-effect failure is retried
**Then** public retry eligibility reflects the profile and the retry is a new predecessor-linked attempt
**And** no terminal outcome is reopened.

**Given** the live recovery sweep
**When** its mailbox admission probe runs against open canonical execution
**Then** the probe is admitted and the sweep's admission phase passes without a Tier-3 skip.

**Requirements:** FR80, FR90, ARCH-13, ARCH-20, ARCH-39, NFR13, NFR13a, NFR15, NFR70.
**Depends on:** Story 1.5 (contracts), Story 1.6 (A6), Story 1.8 (A13).
```

### 4.5 `sprint-status.yaml`

- `last_updated: 2026-11-01` → `2026-11-21`.
- Add `1-12-accept-live-canonical-operation-execution: backlog` after the 1-11 entry.
- Epic 13 action item 1 → `status: done`, preceded by the comment `# 2026-11-21: superseded by sprint-change-proposal-2026-11-21 (TE-2 retired; the Hexalith baseline keeps story-evidence checks out of module CI; completion follows docs/story-evidence-integrity.md).`
- Epic 13 action item 2 → `status: done`, preceded by the comment `# 2026-11-21: delivered as Story Completion criterion 4 (live claims cite non-skipped live runs; fixtures and fallbacks never satisfy them) by sprint-change-proposal-2026-11-21.`
- 1-5 and 1-6 stay `review`. Action items 3–6 stay `open`.

### 4.6 ADRs (CRLF byte edits)

- `docs/adrs/isolated-policy-snapshots.md:146-148`: "…and a passing TE-2 completion contract under [`story-evidence-integrity.md`](../story-evidence-integrity.md)." → "…, recorded under the Story Completion policy in [`story-evidence-integrity.md`](../story-evidence-integrity.md)."
- `docs/adrs/live-recovery-validation-drivers.md`, under `## Story-completion provenance lifecycle`: insert the note below and keep the original paragraphs, including the 2026-08-24 sign-off, as the historical decision:
  > **Superseded 2026-11-21** (`sprint-change-proposal-2026-11-21.md`): TE-2 is retired, and `bf0f44f` removed the `story-evidence-integrity` job, policy and producer, so no `recovery-primary` completion lane exists. The destructive sweep runs only in the product `nightly` hook (`Hexalith.ChatBot.Recovery.Tests`). A story's live recovery claim cites that run under the Story Completion policy in `../story-evidence-integrity.md`. The section below is kept as the historical decision.
- The same ADR's Consequences bullet at `:288` ("A story-completion transition may additionally run the same live class inside `story-evidence-integrity`…") → "(Superseded 2026-11-21: TE-2 is retired, so no story-completion transition runs the live class. The sweep runs only in the product `nightly` hook.)"
- The destructive-scheduler Consequences bullet stays until the CiCd delivery (see §4.8).

### 4.7 Story 1.5 spec (CRLF byte edits; status stays `in-review`, frozen intent unchanged)

- Verification bullet 1 → "`dotnet build Hexalith.ChatBot.slnx --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false`, then run Contracts, Client, CLI, Conformance and Server individually through their built xUnit runners (`tests/<project>/bin/Release/net11.0/<project> -noAutoReporters -failSkips -result-trx …`), keeping TRX local. For Debug source mode, wrap the build in `python3 .github/scripts/with-source-mapping.py`."
- Verification bullet 3 → "Before proposing done, follow the Story Completion policy in `docs/story-evidence-integrity.md`. AC5's live proof is a non-skipped Tier-3 run of `TrivialGovernedCommandAspireE2eTests`, which asserts the live `503 canonical-execution-unavailable` refusal. Live A6/A13 acceptance belongs to Story 1.12."
- New first Spec Change Log entry: "2026-11-21: `sprint-change-proposal-2026-11-21.md` retired TE-2 and redefined done for this story as "contracts landed and proven to fail closed". Live A6/A13 acceptance, retry-profile approval, the three owner decisions (multi-event revision semantics, numeric equivalence, successor uniqueness) and R17-W5 moved to Story 1.12. The frozen intent, which already gated live acceptance, and the AC wording, baseline and status are unchanged."
- New `### Correct course (2026-11-21)` at the end of the Review Triage Log. It records that R21-D1 is closed and supersedes the completion-gate lists in rounds 14–22. It lists the four remaining gates:
  1. a non-skipped, passing Tier-3 run of `TrivialGovernedCommandAspireE2eTests` on the completion commit;
  2. passing Release package-mode Contracts, Client, CLI, Conformance and Server lanes on that commit;
  3. a `## Completion` AC-to-test map;
  4. maintainer approval.

  It also records what moved to Story 1.12, and that R22-W2 belongs to the CiCd adoption request rather than being a gate for this story.

### 4.8 `deferred-work.md` (CRLF byte appends)

- R21-D1 gets: `resolution: Resolved 2026-11-21 by sprint-change-proposal-2026-11-21.md: TE-2 is retired, and stories complete under the Story Completion policy (docs/story-evidence-integrity.md). The doc and ADR bindings are updated. Retiring the legacy workflows moved to the Hexalith.CiCd adoption entry below, because CiCd owns .github/workflows/**.`
- A new section `## Deferred from: correct course sprint-change-proposal-2026-11-21 (2026-11-21)` holds one entry, "Adopt Hexalith.CiCd so that a Release solution build and Server.Tests run on every push and the legacy workflows retire (medium)".
  - Evidence: the two runs above.
  - Closure: CiCd delivers `setup/Hexalith.ChatBot.yml` and generated callers, and the first push run passes Server.Tests (closes R22-W2).
  - ChatBot prerequisites: the Folders wrapper, the `ChatBotMetricsTests` flake, R21-W2/R21-W3, a Release build under `-warnaserror` (CS0618), and a standalone `check.py` run.
  - Also at delivery: update the scheduler bullet in the live-recovery ADR's Consequences section.
- R22-W2, R22-W5, R17-W5 and the owner-decision entries keep their text.

### 4.9 Hexalith.CiCd adoption request (draft, not filed)

```markdown
## Request: adopt Hexalith.ChatBot into the shared CI/CD (complete perimeter)

**Why.** ChatBot's legacy `ci.yml`/`release.yml` call scripts that were removed in the shared-CI
preparation (`bf0f44f`). On `bd61641`:
- ci run 44897519125: Release solution build OK (0 errors, 7 CS0618 warnings), then exit 127 at
  `ci.yml:119` (`run-merge-test-lanes.sh` missing).
- release run 44897518989: every job exits 127 at `build-release-configuration.sh` (missing).
No lane runs `tests/Hexalith.ChatBot.Server.Tests` on push.

**Requested registration** — `setup/Hexalith.ChatBot.yml`, rendered against `builds-version` v6.4.4:

    solution: Hexalith.ChatBot.slnx
    moduleManifest: module/build.module-manifest.v2.json
    dapr: true
    aspire: true
    browsers: true
    codeql: [csharp]
    ciTimeoutMinutes: 90
    nightlyTimeoutMinutes: 330
    hooks: [after-unit-tests, nightly]
    tests:
      exclude:
        tests/Hexalith.ChatBot.Recovery.Tests/Hexalith.ChatBot.Recovery.Tests.csproj: nightly
        tests/Hexalith.ChatBot.Release.Tests/Hexalith.ChatBot.Release.Tests.csproj: nightly
      minimumExpectedTests:
        tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj: 3632
    submodules: [references/Hexalith.Platform]   # final list from the standalone check.py run

**Requested delivery:** the complete owned perimeter: generated callers replacing the legacy
`ci.yml` and `release.yml`, `.github/hexalith-ci.json`, Dependabot, the Builds gitlink, the
delivered CI/CD instruction blocks and the required `.claude/settings.json` deny entries.
Publication stays disabled during cutover.

**Product actions already on ChatBot `main`:** `.github/actions/after-unit-tests` (live topology and
browser prerequisites; sets the Tier-3 flags) and `.github/actions/nightly` (positive-state release
project daily; destructive recovery sweep on Sundays UTC).

**Please confirm:**
1. A 330-minute nightly budget is acceptable (the sweep uses a 250-min in-process deadline, a 280-min
   interrupt and a 300-min step limit).
2. Whether the nightly hook can have its evidence uploaded by the shared workflow, or whether the
   ChatBot action must upload `TestResults/live-recovery/<runId>` itself.
3. `-warnaserror` handling of CS0618, which ChatBot lists in `WarningsNotAsErrors` (6 hits in
   Platform source compiled by the Release build, 1 in ChatBot test code).
4. The `ciTimeoutMinutes` value, once the first hosted unit + Tier-3 integration + UI.E2E run is
   measured (the Release build alone took 4m12s).

**Known, by design:** the nightly Release.Tests M2 stop-ship gate stays red until ChatBot Story 1.8
lands commit participation, so no ChatBot release can pass before then.

**ChatBot preparation before this request is filed:** Folders wrapper realignment, the
`ChatBotMetricsTests` flake, nightly-hook evidence locator and deadline (R21-W2/W3), a clean build
under the shared flags, and a passing `python3 references/Hexalith.Builds/check.py` in a
standalone clone.
```

`minimumExpectedTests` pins Server.Tests at its current 3632, which makes the lane non-vacuous; removing tests would then need a CiCd change. `failSkips` is left at its default (`false`) until the first hosted run has been measured.

## 5. Implementation Handoff

**Scope: moderate.** The backlog is reorganized (new Story 1.12, Epic 13 actions closed) and the policy and docs change. The Developer agent applies it. No PM or Architect replan is needed.

### 5.1 Ordered plan

1. **Apply §4.1–§4.8** (Developer) in one records-only commit. Validate the message with `references/Hexalith.Builds/node_modules/.bin/commitlint`. Check `git diff --stat` for line-ending churn on the CRLF files.
2. **Complete Story 1.5** (Developer, after this proposal is approved):
   - run `TrivialGovernedCommandAspireE2eTests` live (Tier-3) on the completion commit;
   - rerun the Release package-mode lanes;
   - add the `## Completion` section;
   - ask the maintainer to approve;
   - then make a records-only commit setting spec `done` and sprint `done`.
3. **ChatBot CiCd preparation** (Developer; separate sessions):
   - Folders wrapper drift: realign `CommunityToolkit.Aspire.Hosting.Dapr` inside `references/Hexalith.Folders`, then bump the submodule pointer.
   - `ChatBotMetricsTests` flake: capture instruments by the test's own meter instance.
   - R21-W2/R21-W3: the nightly evidence upload and the deadline export.
   - Verify CS0618 under `-warnaserror`. Fix `RecoverySandboxTopologyComposer.cs:55`; the Platform hits go upstream.
   - Run `check.py` in a standalone clone with a scratch `hexalith-ci.json`, and record the submodule list and timings.
4. **File §4.9 with Hexalith.CiCd** (maintainer decision; outward-facing). At delivery, update the scheduler bullet in the live-recovery ADR.
5. **R22-W5 sweep** (Developer, after step 1): retire or re-scope the roughly 25 ledger entries bound to TE-2, the legacy workflows and deleted guards. This includes R13-W1 (Story 1.2's evidence contract), which becomes moot.

### 5.2 Success criteria

- No ChatBot document, ADR or planning artifact still requires a TE-2 contract or the `story-evidence-integrity` job. The `evidence/` folder is explicitly frozen history.
- Story 1.5 reaches `done` under the Story Completion policy, with a cited live fail-closed run. Story 1.12 is in the backlog with its dependencies.
- Hexalith.CiCd delivers ChatBot's perimeter, and the first push run executes Server.Tests (at least 3632 tests) in a Release solution build.
