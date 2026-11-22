---
title: 'Complete the Builds catalog so six consumers can drop local package versions'
type: 'chore'
created: '2026-11-21'
status: 'done'
baseline_commit: '27fe4d62ca1f3cce07a9f19e1e30b02cbaa51357' # Hexalith.Builds worktree base (not a ChatBot revision)
route: 'dispatch'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Folders, Memories, Parties, Timesheets, Works and McpCli are declaring package versions locally again (deferred-work.md, 2026-11-21, source_spec 1-1e). Two catalog gaps cause part of this: the Builds catalog still forces `CommunityToolkit.Aspire.Hosting.Dapr` 13.0.0 on `Hexalith.Folders.Aspire`, which is binary-incompatible with Aspire 17 and which Folders already overrides, and the catalog lacks `Hexalith.Memories.ServiceDefaults` and `Hexalith.Memories.Telemetry`.

**Approach:** Settle those gaps in `Hexalith.Builds/Props/Directory.Packages.props` and update the Builds checks and prose that pin the old exception, so that `Props/Directory.Packages.props` can be the only package-version authority before any consumer change.

## Boundaries & Constraints

**Always:** The work happens in the Hexalith.Builds repository. Preserve the catalog's UTF-8 BOM and CRLF line endings. Keep the synthetic historical fixtures, which build their own catalog strings (`test-platform-version-catalog.ps1` `$catalogText`, `WorkspaceGitFixture`, `CatalogVersionTests`, `CompositionCatalogPackageViewTests`). Version rows use the existing `$(HexalithMemoriesVersion)` property.

**Never:** No commit or push without explicit approval. No consumer-repo edits; those belong to Prompt 2. No edits to `.github/workflows/**` or Dependabot. Do not disable or weaken validators or audits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Neutral consumer | catalog evaluated with no project name | Dapr toolkit = 17.0.0-preview.1.261120-0454 | N/A |
| Folders.Aspire | project named `Hexalith.Folders.Aspire` | same Dapr version as the neutral evaluation, no row diverges | fixture suite fails on divergence |
| Memories hosted profile | `HexalithUsePublishedSdkForHostedTests=true` | catalog supplies ServiceDefaults/Telemetry at `$(HexalithMemoriesVersion)` (4.1.0) | N/A |
| Memories cohort override | `-p:HexalithMemoriesVersion=X` | both new rows follow X | N/A |

## Decisions (2026-11-21, Jerome: "apply recommended")

- D1: Keep the catalog default `CommunityToolkit.Aspire.Hosting.Dapr` 17.0.0-preview.1.261120-0454 and delete the Folders.Aspire 13.0.0 condition (lines 39 and 41).
- D2: Add `Hexalith.Memories.ServiceDefaults` and `Hexalith.Memories.Telemetry` at `$(HexalithMemoriesVersion)`.
- D3: Make no catalog change for the EventStore floor. Memories drops `MemoriesEventStoreMinimumVersion` in Prompt 2.
- D4: Two local commits are allowed, with no push: first the catalog with its checks and prose, then `Tools/package-version-audit.json` regenerated from that commit. Validate both messages with Builds commitlint first.
- D5: Work in a new detached worktree, `/home/administrator/projects/hexalith/builds-catalog-consumer-authority`, created from origin/main d532b1e9 with no branch. Leave the main clone `~/projects/hexalith/builds` untouched.

</frozen-after-approval>

## Code Map

- `Props/Directory.Packages.props:39-41` -- Dapr default plus the Folders.Aspire 13.0.0 condition (added by 360a2b9c on 2026-10-04, when the catalog was on the 13.x line)
- `Props/Directory.Packages.props:242-252` -- Hexalith.Memories.* rows; the new rows go after `:252`
- `Props/Directory.Packages.props:26-28,546-561` -- EventStore 5.4.0 selection and the guards for the selected cohort (8e439af5)
- `Tools/test-authoritative-package-catalog.ps1:~36,194-212` -- `$requiredPackages`; real-catalog assertion that Folders.Aspire is 13.0.0
- `Tools/test-platform-version-catalog.ps1:216-224` -- real-catalog `$foldersPin -cne '13.0.0'`
- `Tools/write-platform-version-catalog.ps1:106`, `README.md:955-956`, `docs/dotnet-11-migration.md:34-36` -- stale exception prose
- `Tools/package-version-audit.json` -- `catalogSha256`; regenerate with `Tools/audit-central-package-versions.ps1 -PriorAuditPath <backup> -OutputPath Tools/package-version-audit.json -ChangedFamily hexalith-memories` from a committed catalog
- Memories (`references/Hexalith.Memories` e413124b): `Directory.Packages.props:47-50` and `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` hosted-profile PackageReferences -- the consumers of the new rows; read only

## Tasks & Acceptance

**Execution:**
- [x] `Props/Directory.Packages.props` -- per D1, delete lines 39 and 41; add the two Memories rows -- one authority for every consumer
- [x] `Tools/test-authoritative-package-catalog.ps1` -- require the two Memories packages; assert that a Folders.Aspire-named evaluation matches the neutral selection for every row -- guards against a project-conditioned divergence coming back
- [x] `Tools/test-platform-version-catalog.ps1` -- the real-catalog Folders pin must equal the neutral pin
- [x] `Tools/write-platform-version-catalog.ps1`, `README.md`, `docs/dotnet-11-migration.md` -- remove the exception prose
- [x] `Tools/package-version-audit.json` -- after commit 1, back up the audit and regenerate it with `-ChangedFamily hexalith-memories`; validate; commit 2 (D4)

**Acceptance Criteria:**
- Given the edited catalog, when the catalog, consumer-authority, exception, platform and Dapr fixture suites run, then all pass.
- Given the edited catalog, when `dotnet restore`/`build` of `Hexalith.Builds.slnx` run in Release with the CI flags and each unit and integration test project and the Python suite run, then they pass, or any failure is shown to be pre-existing at 27fe4d62.

## Design Notes

Sequencing: each consumer's own `references/Hexalith.Builds` gitlink (owned by CiCd) points at a catalog from 11-18 to 11-20: EventStore 4.2.0 (5.4.0 for Memories), Memories 3.0.27, Dapr beta.1052/1078, and the Folders.Aspire condition still present. Prompt 2 can only drop rows after CiCd advances those gitlinks to a commit that contains this change, so the CiCd request must include that step. Folders' `FoldersAspireShouldUseAspire17DaprIntegration` already fails at 73ca79b4 because it hard-codes beta.1078; Prompt 2 rewrites it.


D3 rationale: `MemoriesEventStoreMinimumVersion` is 5.4.0, and the catalog default `HexalithEventStoreVersion` is 5.4.0, the newest on nuget.org, so the floor is a no-op by default. It acts only when a caller selects a lower cohort (`-p:HexalithEventStoreVersion`, honored since 8e439af5), and there it silently contradicts that selection. No catalog change is needed; Memories should drop the floor in Prompt 2.

## Verification

**Commands:**
- `pwsh -NoProfile -File Tools/<suite>.ps1` for: validate-central-package-versions, test-authoritative-package-catalog, test-central-package-version-validator, validate-consumer-package-authority `-RepositoryRoot . -CatalogPath ./Props/Directory.Packages.props`, test-consumer-package-authority-validator, validate-package-version-exceptions `-InventoryPath ./Tools/package-version-exceptions.json -CatalogPath ./Props/Directory.Packages.props`, test-package-version-exception-validator, validate/test-platform-version-catalog, validate/test-dapr-package-version(s|-validator), validate-package-version-audit -- expected: exit 0
- `dotnet restore`/`dotnet build Hexalith.Builds.slnx` with the CI Release flags; `dotnet test --project` for Evidence.Tests, Module.Tests and Tooling.IntegrationTests; `uv run ... Tools/ci/run_python_tests.py` -- expected: pass

## Implementation Notes

- Builds commits, pushed to Hexalith.Builds `main` on 2026-11-22 (fast-forward 27fe4d62..0de06365): `75dce33a` `fix(catalog): make the Builds catalog every consumer's package-version authority` (catalog, checks, prose) and `0de06365` `build(deps): refresh hexalith-memories audit` (audit regenerated from 75dce33a with the 27fe4d62 audit as prior; 1 refreshed family and 156 preserved). They replace the earlier attempts (72935ae0/21397a99 and 548a36d6/dd6c87df), which had the same catalog bytes. Both messages passed the pinned Builds commitlint 21.2.4. The catalog keeps its BOM and CRLF.
- The Folders.Aspire guard in `test-authoritative-package-catalog.ps1` compares the row count and every added, missing or differing row with the neutral evaluation. With the condition put back in a scratch copy, both it and `test-platform-version-catalog.ps1` fail with the divergence message.
- I/O matrix: `test-authoritative-package-catalog.ps1` now covers every row. Row 1 binds the neutral Dapr row to `HexalithAspireHostingDaprVersion` and both Memories rows to `HexalithMemoriesVersion`, with no literals. The `Hexalith.Memories.` family is also enrolled in `$cohortFamilies`, so every Memories row must bind to that property. Row 2 is the Folders.Aspire guard. Row 3 evaluates with `-p:HexalithUsePublishedSdkForHostedTests=true` and requires each Memories row exactly once at the neutral `HexalithMemoriesVersion`. Row 4 evaluates with `-p:HexalithMemoriesVersion=9.8.7` and requires both rows at 9.8.7. Negative controls in scratch copies all fail as expected: a literal Telemetry version (row 4), a literal Dapr version (row 1), and a hosted-only `HexalithMemoriesVersion` override (row 3). A simulated Memories wrapper also gets both rows once.
- Verification: every listed suite passes. `validate-package-version-audit` failed only between the two commits, as expected. The Release restore/build has 0 warnings. Evidence.Tests 191/191; Module.Tests 3158 passed, 4 skipped; Tooling.IntegrationTests 1 passed, 12 live-opt-in skipped. The audit generator/validator suites pass (113/109). The Python suite passes 17/18 groups: in `Tools/ci/tests` only `test_real_zizmor_enforces_committed_third_party_pinning_policy_online` fails, because it needs a GitHub token. It fails the same way at 27fe4d62.
- Left unchanged: `docs/dotnet-11-migration.md` still says 17.0.0-beta.1052 (a historical migration note, not exception prose). Audit `historicalContext` entries that mention the old Folders pin are preserved history.

## Spec Change Log

- 2026-11-22 (Jerome, during step 3): D5's base is amended from d532b1e9 to Builds origin/main `27fe4d62ca1f3cce07a9f19e1e30b02cbaa51357`. Main had moved on with the Commons 3.0.2 bump (`Props/Directory.Packages.props:18` plus a regenerated `Tools/package-version-audit.json`), and building on d532b1e9 would have made commit 2 conflict. This entry supersedes d532b1e9 everywhere the spec names it as the base. The detached worktree `/home/administrator/projects/hexalith/builds-catalog-consumer-authority` already exists at that commit with no branch: work there and do not recreate it. Pre-existing failures are proven at 27fe4d62.

## Review Triage Log

Verification-gap layer: no gaps filed. Its two "Other findings" were verified like the others.

| # | Layer | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| 1 | blind | Test comment (test-authoritative :229-230) says a project name never changes a central selection; commit title says the same | low | Catalog :168 adds Platform.TestExtension for Platform.EventStoreHost, and :206 drops EventStore.Contracts for Works source builds. The comment overstates the guard. The title is accurate: the catalog still decides those rows. | patch (G1) |
| 2 | blind | No general guard against new project-name conditions | low | No such guard exists. The two remaining conditions are approved and not version splits. A general scan would add a new guard. | rejected |
| 3 | blind | Hosted-profile evaluation proves nothing new | false | It asserts matrix row 3 on the catalog side: with the flag set, the catalog supplies both rows at HexalithMemoriesVersion. The Memories fallback ItemGroup is consumer code, which the intent excludes. | rejected |
| 4 | blind | Memories is missing from `$cohortFamilies` | low | Verified: the 7 Memories rows bind to $(HexalithMemoriesVersion), but the family isn't enrolled, so the 5 older rows are unguarded. | patch (G5) |
| 5 | blind | The neutral and Folders evaluations differ on -noAutoResponse | maybe-false | Already present at test-authoritative :17 before this change. `find ~/projects ~ -maxdepth 3` found no Directory.Build.rsp, and a divergence would fail loudly. At most low. | rejected |
| 6 | blind | test-platform's new neutral evaluation lacks an exit-code check and -noAutoResponse; Folders and neutral could regress together | low | A missing exit check only worsens the error message; the test still fails. It matches the Folders evaluation next to it. A joint regression to 13.0.0 is caught by the EventStore.Aspire nuspec range check in write-platform-version-catalog.ps1 :187-204. | rejected |
| 7 | blind | docs/dotnet-11-migration.md :34 still says 17.0.0-beta.1052 in present tense | low | An unchanged context line that was already stale at the base. | defer |
| 8 | blind | Commit 1 message says Memories "no longer declares" its versions locally | low | Memories keeps both rows until CiCd advances the gitlinks and Prompt 2 lands. | patch (G9) |
| 9 | blind | The deleted stable/prerelease rationale isn't replaced | low | The deleted comment became false under D1. NU5104 suppression belongs to Folders locally (Prompt 2). New prose goes beyond a direct correction. | rejected |
| 10 | blind | Tools/README.md :231-236 omits the new guarantees | low | The paragraph lists what the test protects but not Folders.Aspire neutrality or the Memories selections. | patch (G11) |
| 11 | blind | The audit refresh adds no-change history entries | low | Generator behavior, untouched by this diff; the fix would mean redesigning the generator. | rejected |
| 12 | blind | The audit can't see project-conditioned selections | low | By design, the audit evaluates only the neutral catalog. Not caused by this change. | rejected |
| 13 | verification-gap | False single-authority comment | low | Same as #1. | patch (G1) |
| 14 | verification-gap | Hosted-profile check doesn't exercise the Memories hosted profile | false | Same as #3. | rejected |
| 15 | edge-case | Hosted-profile evaluation equals the neutral one | false | Same as #3. | rejected |
| 16 | edge-case | Project-name condition for another consumer goes unguarded | low | Same as #2. | rejected |
| 17 | edge-case | Folders probe doesn't compare property-only selections | low | No property-level MSBuildProjectName condition exists; the only ones are :168 and :206. Comparing properties would add a guard. | rejected |
| 18 | edge-case | test-authoritative :17 lacks -noAutoResponse | maybe-false | Same as #5. | rejected |
| 19 | edge-case | test-platform :221-227 exit code and response file | low | Same as #6. | rejected |
| 20 | edge-case | Memories family could split from its cohort | low | Same as #4. | patch (G5) |
| 21 | edge-case | Claim: single-authority guarantee | low | Same as #1. | patch (G1) |
