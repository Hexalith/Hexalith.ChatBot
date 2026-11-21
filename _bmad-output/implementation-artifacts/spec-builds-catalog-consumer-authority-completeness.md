---
title: 'Complete the Builds catalog so six consumers can drop local package versions'
type: 'chore'
created: '2026-11-21'
status: 'ready-for-dev'
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
- [ ] `Props/Directory.Packages.props` -- per D1, delete lines 39 and 41; add the two Memories rows -- one authority for every consumer
- [ ] `Tools/test-authoritative-package-catalog.ps1` -- require the two Memories packages; assert that a Folders.Aspire-named evaluation matches the neutral selection for every row -- guards against a project-conditioned divergence coming back
- [ ] `Tools/test-platform-version-catalog.ps1` -- the real-catalog Folders pin must equal the neutral pin
- [ ] `Tools/write-platform-version-catalog.ps1`, `README.md`, `docs/dotnet-11-migration.md` -- remove the exception prose
- [ ] `Tools/package-version-audit.json` -- after commit 1, back up the audit and regenerate it with `-ChangedFamily hexalith-memories`; validate; commit 2 (D4)

**Acceptance Criteria:**
- Given the edited catalog, when the catalog, consumer-authority, exception, platform and Dapr fixture suites run, then all pass.
- Given the edited catalog, when `dotnet restore`/`build` of `Hexalith.Builds.slnx` run in Release with the CI flags and each unit and integration test project and the Python suite run, then they pass, or any failure is shown to be pre-existing at d532b1e9.

## Design Notes

Sequencing: each consumer's own `references/Hexalith.Builds` gitlink (owned by CiCd) points at a catalog from 11-18 to 11-20: EventStore 4.2.0 (5.4.0 for Memories), Memories 3.0.27, Dapr beta.1052/1078, and the Folders.Aspire condition still present. Prompt 2 can only drop rows after CiCd advances those gitlinks to a commit that contains this change, so the CiCd request must include that step. Folders' `FoldersAspireShouldUseAspire17DaprIntegration` already fails at 73ca79b4 because it hard-codes beta.1078; Prompt 2 rewrites it.


D3 rationale: `MemoriesEventStoreMinimumVersion` is 5.4.0, and the catalog default `HexalithEventStoreVersion` is 5.4.0, the newest on nuget.org, so the floor is a no-op by default. It acts only when a caller selects a lower cohort (`-p:HexalithEventStoreVersion`, honored since 8e439af5), and there it silently contradicts that selection. No catalog change is needed; Memories should drop the floor in Prompt 2.

## Verification

**Commands:**
- `pwsh -NoProfile -File Tools/<suite>.ps1` for: validate-central-package-versions, test-authoritative-package-catalog, test-central-package-version-validator, validate-consumer-package-authority `-RepositoryRoot . -CatalogPath ./Props/Directory.Packages.props`, test-consumer-package-authority-validator, validate-package-version-exceptions `-InventoryPath ./Tools/package-version-exceptions.json -CatalogPath ./Props/Directory.Packages.props`, test-package-version-exception-validator, validate/test-platform-version-catalog, validate/test-dapr-package-version(s|-validator), validate-package-version-audit -- expected: exit 0
- `dotnet restore`/`dotnet build Hexalith.Builds.slnx` with the CI Release flags; `dotnet test --project` for Evidence.Tests, Module.Tests and Tooling.IntegrationTests; `uv run ... Tools/ci/run_python_tests.py` -- expected: pass

## Implementation Notes

## Spec Change Log

## Review Triage Log
