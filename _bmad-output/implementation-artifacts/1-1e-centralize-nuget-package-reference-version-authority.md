---
baseline_commit: 9567f43d61478192cf30d0cef08aa63857ae8796
---

# Story 1.1e: Centralize NuGet Package-Reference Version Authority

Status: in-progress

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-07-18. -->

## Story

As a platform engineer,
I want every Hexalith repository to obtain package-reference versions exclusively from `Hexalith.Builds`,
so that package versions cannot drift between the superproject and its submodules.

**Epic 1 anchor:** this story mechanically protects the reproducible build and integration baseline used by the already-delivered governed-command spine and Story 1.9 proof. It changes build governance only; it does not change product behavior.

## Acceptance Criteria

1. **The Builds catalog is the only package-reference version authority.** Given the ChatBot superproject and each root-declared .NET consumer repository, when Central Package Management evaluates dependency versions, then `references/Hexalith.Builds/Props/Directory.Packages.props` is the sole owner of every dependency `PackageVersion`, and each consumer-root `Directory.Packages.props` is a version-free wrapper that explicitly imports that catalog. The wrapper may retain non-version CPM settings and its existing standalone/superproject path-resolution logic, but it contains no dependency version declaration or workaround. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1e-Centralize-NuGet-package-reference-version-authority; Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints-Dependencies]

2. **Exclusive ownership is mechanically enforced.** Given package declarations in a consumer repository, when build-governance validation runs, then it rejects consumer-local `PackageVersion Include`, `PackageVersion Update`, dependency-version properties or expressions used as a version workaround, `PackageReference Version`, nested `<Version>`, `VersionOverride`, version-bearing `GlobalPackageReference`, and a project-local opt-out from CPM. The shared catalog evaluates successfully with nonblank, case-insensitively unique package IDs and exactly one resolved valid version per ID; `CentralPackageVersionOverrideEnabled` is false; SDK-created `PackageReference` items marked `IsImplicitlyDefined="true"` are not incorrectly required to have catalog rows. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1e-Centralize-NuGet-package-reference-version-authority; Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#Shared-validation-and-CI; Source: Microsoft Learn#Central-Package-Management]

3. **The approved migration matrix is applied without unapproved dependency drift.** Given the approved inventory, when consumer-local definitions are removed, then all 15 missing package IDs exist in the Builds catalog at the approved canonical versions, each of the 30 approved conflicting package IDs resolves to the approved Builds value, the EventStore `HexalithCommonsVersion` compatibility override is removed in favor of the shared `2.28.2` default, and the remaining equal local definitions are removed without changing their effective versions. Because the implementation baseline postdates the approved inventory, implementation first records and obtains an explicit disposition for every divergence (currently 31 unique conflict IDs, including newly conflicting `OpenTelemetry.Instrumentation.AspNetCore` and `OpenTelemetry.Instrumentation.Http`, plus a changed shared target for `OpenTelemetry.Instrumentation.Runtime`); neither the proposal nor the newer catalog silently overrides the other. Any unexplained inventory drift stops the migration for explicit reconciliation; no version is substituted merely because a newer release exists. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1e-Centralize-NuGet-package-reference-version-authority; Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#Technical-Migration-Contract]

4. **CPM-incompatible versions remain explicit, inventoried exceptions.** Given NuGet SDK resolver pins and repository tool manifests, when package authority is assessed, then the ten `Aspire.AppHost.Sdk/<version>` declarations and five `.config/dotnet-tools.json` tool versions remain outside CPM, their file, package/tool ID, version, owner, rationale, and alignment rule are recorded in a machine-verifiable exception inventory, and validation keeps AppHost SDK/`Aspire.Hosting` families intentionally aligned. A new exception requires an architecture decision and may not use local `PackageVersion` or `PackageReference Version` as a workaround. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1e-Centralize-NuGet-package-reference-version-authority; Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#CPM-incompatible-exceptions]

5. **Catalog-first rollout evidence is complete.** Given the shared catalog and validators are green, when completion evidence is recorded, then every affected repository passes its own relevant restore, canonical build, focused test, package/consumer, and resolved-graph lanes independently before its superproject pointer is integrated; the complete ChatBot superproject then passes restore/build, package-authority architecture tests, affected UI/architecture suites, and relevant integration lanes without a local override. Evidence names the exact command, repository, result, effective version changes, and any environment blocker. [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1e-Centralize-NuGet-package-reference-version-authority; Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#Implementation-Sequence-and-Success-Criteria]

## Tasks / Subtasks

- [x] Re-establish the migration baseline without disturbing current work (AC: 1-5)
  - [x] Read each affected repository's `AGENTS.md`/tracked guidance and inspect its branch, working tree, remotes, recent history, build configuration, solution, tests, and root `.gitmodules` before editing that repository.
  - [x] Re-run the package-authority inventory over the ChatBot root and all root-declared submodules without initializing nested submodules. Compare the result with the proposal snapshot: 281 .NET projects, 14 package-props files, 102 local `PackageVersion` rows plus the EventStore property override, 71 unique local IDs, 15 missing catalog IDs, 30 conflicts, 26 already-equal IDs, ten AppHost SDK pins, and five tool pins.
  - [x] Reconcile the known post-proposal baseline at commit `9567f43`: the Builds catalog has 268 rows; the 15 missing IDs remain; source comparison finds 31 unique conflicting IDs because `OpenTelemetry.Instrumentation.AspNetCore` and `OpenTelemetry.Instrumentation.Http` now differ, while the shared `OpenTelemetry.Instrumentation.Runtime` target moved from the approved `1.16.0` to `1.17.0`. Record an explicit planning/architecture disposition before changing consumers; do not infer approval from recency alone.
  - [x] Stop and document any other unexplained difference from the approved matrix before changing versions. Do not reinterpret a snapshot count as permission to delete a newly introduced package.
  - [x] Preserve all pre-existing root and submodule work represented by the recent commits. In particular, do not overwrite the ChatBot `Directory.Packages.props` updates, the Story 1.1c CI/AppHost work, the `1-1c` status transition, or newly integrated submodule pointers.
  - [x] Work in the repository that owns each change. Do not commit, push, release, force-update, or recursively initialize submodules as an implicit implementation step.

- [x] Make `Hexalith.Builds` the complete, fail-closed catalog authority (AC: 1-3)
  - [x] Update `references/Hexalith.Builds/Props/Directory.Packages.props` first with the 15 additions and exact canonical versions in the binding matrix below.
  - [x] Confirm every existing conflict already resolves to the approved Builds value; change only values explicitly authorized by the matrix. Keep shared Hexalith family properties in the authoritative catalog, but remove consumer-side compatibility/version properties.
  - [x] Set `CentralPackageVersionOverrideEnabled` to `false` at the shared authority so `VersionOverride` cannot bypass CPM, while retaining a source scan as defense in depth.
  - [x] Preserve the existing Builds self-wrapper as an import-only `Directory.Packages.props`; do not duplicate the catalog in the wrapper.
  - [x] Verify the catalog through evaluated MSBuild output and restore before removing any consumer definitions.

- [x] Extend Builds-owned catalog and consumer-authority validation (AC: 1-4)
  - [x] Extend `Tools/validate-central-package-versions.ps1` and its fixture suite to reject blank IDs, case-insensitive duplicates, blank/unresolved/tag-prefixed/malformed versions, failed/malformed evaluation, and mismatched effective catalog results.
  - [x] Add or extend a Builds-owned consumer-authority validator and fixture suite under `references/Hexalith.Builds/Tools/`. It must source-scan consumer XML and evaluate representative projects; either signal alone is insufficient.
  - [x] Reject consumer `PackageVersion Include` and `Update`, `PackageReference` attribute/nested versions, `VersionOverride`, version-bearing `GlobalPackageReference`, project-local `ManagePackageVersionsCentrally=false`, and properties/expressions that feed forbidden version metadata.
  - [x] Prove that a valid version-free wrapper passes, metadata such as `PrivateAssets`/`IncludeAssets` remains legal, imported consumer `Update` cannot hide behind the catalog's `DefiningProjectFullPath`, SDK-implicit references are handled correctly, and every forbidden form fails with a precise diagnostic.
  - [x] Validate that every .NET consumer root imports the shared catalog and every evaluated project has CPM enabled. Compare evaluated effective versions with the authoritative catalog, not only XML text.
  - [x] Preserve each repository's existing `CentralPackageTransitivePinningEnabled` posture; do not enable it globally. Where already enabled, verify resolved graphs and pack output for promoted transitive dependencies and NU1109 downgrade failures.
  - [x] Add a machine-readable SDK/tool exception inventory and an alignment validator. Treat that inventory as an allowlist, not a general version escape hatch.

- [x] Wire the invariant into Builds documentation, samples, and CI (AC: 1-4)
  - [x] Update `references/Hexalith.Builds/README.md`, `Tools/README.md`, and `Samples/Module.Directory.Packages.props` so no example invites repository-specific `PackageVersion` rows.
  - [x] Update `references/Hexalith.Builds/.github/workflows/build-release.yml` so catalog integrity, consumer-authority fixtures, and exception/alignment validation run before DAPR-family validation and release creation. Update reusable `domain-ci.yml`/`domain-release.yml` only where their validation interface or cache inputs must change.
  - [x] Preserve reusable workflow cache inputs that include both the consumer wrapper and `references/Hexalith.Builds/Props/Directory.Packages.props`.
  - [x] Verify `Hexalith.Builds` independently before consumer migration; do not use a consumer-local override to make a failing Builds catalog appear green.

- [x] Migrate all twelve .NET consumers in their owning repositories (AC: 1-3)
  - [x] Replace the ChatBot root's 57-row local catalog with a version-free wrapper importing the shared catalog; preserve current non-version settings only when still needed.
  - [x] Remove EventStore's three local package rows and the `HexalithCommonsVersion` compatibility override.
  - [x] Remove Parties' four local package rows/updates, Memories' thirteen, Commons' two, Timesheets' twenty-two, and PolymorphicSerializations' one.
  - [x] Retain and validate the already version-free wrappers in Tenants, FrontComposer, Folders, Conversations, and Projects; normalize only what is necessary for the exclusive-authority contract.
  - [x] Add `references/Hexalith.Builds` as a root-declared dependency of Timesheets for standalone consumption, using a non-recursive root initialization path. Do not initialize it as a nested submodule from the ChatBot umbrella without explicit authority.
  - [x] Fix compatibility failures in the affected consumer's source/tests/configuration. Never restore a local version escape hatch, and never weaken warnings-as-errors, package-only Release validation, analyzer gates, or test assertions to absorb a canonical version change.
  - [x] Preserve Debug source-reference versus Release package-reference behavior and rerun restore after switching dependency modes.

- [x] Update ChatBot package-governance and package-pin tests (AC: 1-3, 5)
  - [x] Update `ScaffoldArchitectureTests` so the MCP assertion evaluates shared-catalog `ModelContextProtocol` `1.4.1`; strengthen the inline-version guard for nested `Version`, `VersionOverride`, opt-out, wrapper import, and exclusive ownership.
  - [x] Update the accessibility, responsive/touch, localization, and live-region/reduced-motion contract tests to resolve/evaluate the imported catalog and assert the approved canonical pins: Fluent UI `5.0.0-rc.4-26180.1`, Fluxor `6.10.0`, Playwright `1.61.0`, xUnit v3 `3.2.2`, and bUnit `2.8.4-preview`.
  - [x] Reuse one test helper or evaluated-catalog mechanism where practical; do not copy five independent XML parsers that can drift.
  - [x] Keep `.csproj` `PackageReference` items version-free and preserve legal asset metadata. Do not modify generated clients or product/UI behavior for this build-governance story.
  - [x] Run a clean/no-incremental focused rebuild when validating changed package-pin tests so stale test assemblies cannot provide false evidence.

- [x] Inventory and validate CPM-incompatible version mechanisms (AC: 4)
  - [x] Record the ten current AppHost SDK declarations and the five tool pins listed below with repository owner and alignment rule.
  - [x] Compare every AppHost SDK version with the effective shared `Aspire.Hosting` family. Resolve the current Conversations `13.4.2` versus shared `13.4.6` mismatch through the approved family-alignment rule or a named architecture exception; do not leave it silently divergent.
  - [x] Keep EventStore/FrontComposer/Parties tool-manifest versions explicit and exact. Verify their ownership and restore behavior without attempting to move them into CPM.
  - [x] Reject unlisted SDK/tool exceptions and require an architecture decision before expanding the allowlist.

- [ ] Produce independent consumer and umbrella completion evidence (AC: 3-5)
  - [ ] For each repository, run its canonical `.slnx` restore and build using that repository's documented serialization/configuration; run its documented focused tests rather than assuming every Hexalith repository shares one test runner convention.
  - [x] Where a canonical change can affect the resolved graph, capture `dotnet package list --project <solution-or-project> --include-transitive --no-restore --format json` or equivalent evaluated evidence and compare it with the matrix.
  - [x] For repositories using transitive pinning or publishing NuGet packages, run their package-only consumer/pack validation and inspect promoted dependencies.
  - [x] Run the Builds validator fixture suites; run the ChatBot Architecture and UI test projects individually; run any affected consumer compatibility tests; use repository-approved direct xUnit v3 executables when Microsoft.Testing.Platform/VSTest filtering or listener constraints require the documented fallback.
  - [ ] After independently verified owning-repository changes are integrated, run ChatBot `dotnet restore Hexalith.ChatBot.slnx`, canonical Release build, package-authority scans, affected focused test projects, and relevant integration lanes.
  - [x] Record a final zero-consumer-local-definition scan, the evaluated effective-version matrix, the SDK/tool exception inventory, exact commands/results, and `git diff --check` for every owning repository.

### Review Findings

Code review 2026-07-18 (baseline `9567f43` → `cc664c6` + submodules; layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor; all findings verified against the live workspace).

- [x] [Review][Decision] RESOLVED 2026-07-18: Jerome accepted `HexalithEventStoreVersion` `3.74.0` and `HexalithTenantsVersion` `3.2.18`; superseding disposition recorded in `.decision-log.md` and the Dev Agent Record annotated. Original finding: Undispositioned post-approval catalog drift folded in (HIGH, AC3) — Builds commits `1484ee3` (`HexalithEventStoreVersion` 3.71.0→3.74.0) and `8fd1b07` (`HexalithTenantsVersion` 3.15.1→3.2.18) changed shared family versions after Jerome's recorded disposition stating "No other shared version change is implied". The story's validator commit `96c83fc` shipped `test-authoritative-package-catalog.ps1` expecting Contracts `3.71.0`, which fails against the same commit's 3.74.0 catalog; follow-up `c177c66` (now recorded by root `cc664c6`) re-aligned the fixture to 3.74.0 with no decision-log disposition — the exact "fold the drift in" move AC3 forbids. The decision log and Dev Agent Record claims ("evaluates to 3.71.0", "all 48 approved/reconciled values pass", "promoted dependencies contain Contracts 3.71.0") no longer describe the shipped state. Mitigation: EventStore 3.72–3.74 are genuine same-day releases and Tenants has no 3.15.x release line (3.2.18 is its actual latest; the old pin appears erroneous), so the values are plausibly correct — the missing piece is the governance record. Resolve by either (a) recording a superseding disposition accepting 3.74.0/3.2.18 and correcting the story record + decision log, or (b) rolling the catalog back to the approved values pending reconciliation.
- [x] [Review][Patch] Dev Agent Record File List omits five changed Memories files, two of which are non-package-compat changes without recorded rationale (`MarkdownContractDocument.NormalizeLineEndings` rewrite + `ContractDocumentGuardTests`, new `AccessTelemetryLifecycleMetricsTestCollection` with `DisableParallelization`, plus two AccessTelemetry checkpoint tests) [_bmad-output/implementation-artifacts/1-1e-centralize-nuget-package-reference-version-authority.md — File List / Completion Notes]
- [x] [Review][Patch] Catalog-evaluation helpers use sequential `StandardOutput`/`StandardError.ReadToEnd()` with untimed `WaitForExit()` (pipe-fill deadlock/hang risk exactly when msbuild errors verbosely), an exception-caching `Lazy` that poisons the whole assembly on one transient failure, and ordinal full-path comparison that spurious-fails on case-insensitive filesystems [tests/PackageCatalogTestHelper.cs:85; references/Hexalith.Memories/tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs:157]
- [x] [Review][Patch] Consumer-authority validator's wrapper source-scan accepts any `<Import>` without comparing its target to `-CatalogPath` (message overclaims; compensated only by the effective-value comparison), `& git` absence crashes before the `Get-ChildItem` fallback can run, and the fallback file set differs from git mode (no `references/` exclusion) [references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1:157]
- [x] [Review][Patch] Exception validator is fail-open for AppHost SDK pins expressed as `<Sdk Name="Aspire.AppHost.Sdk" Version="…"/>` elements or `global.json` `msbuild-sdks` (regex matches only the `Sdk/version` form), never scans non-Aspire SDK version pins, and has partial-checkout identity edge cases (uninitialized submodules, workspace-owner leaf fallback) [references/Hexalith.Builds/Tools/validate-package-version-exceptions.ps1:281]
- [x] [Review][Patch] ChatBot's only in-CI guard misses project-local `<PackageVersion Include/Update>` items (a real CPM override vector the PS validator rejects but ChatBot CI never runs) and a project-level `CentralPackageVersionOverrideEnabled=true` re-enable; scan scope is `src/`+`tests/` only and ignores root `Directory.Build.props` property overrides (currently clean — verified) [tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs:634]
- [x] [Review][Patch] Evidence bookkeeping: Completion Notes claim "281 projects (22 umbrella plus 259 submodule)" which reconciles with neither the accepted 285-project inventory nor 22+263; and the exception inventory's `architectureDecision` points at a ChatBot-private planning artifact unresolvable for standalone Builds consumers (and ambiguous versus the same-day ci-cd-alignment proposal) [references/Hexalith.Builds/Tools/package-version-exceptions.json]
- [x] [Review][Patch] Load-bearing rationale comments were dropped in the migration: Memories' ADR-8.5-001 `OpenTelemetry.Instrumentation.StackExchangeRedis` prerelease acceptance + upgrade-on-GA trigger, ADR-7.1-008 `System.CommandLine` rationale, and ChatBot's NetArchTest-fork note; the corresponding Builds catalog rows are bare [references/Hexalith.Builds/Props/Directory.Packages.props:1]
- [x] [Review][Patch] Four UI pin tests named `PackagePinsShouldRemainUnchanged*` were already dead at baseline (asserting Fluxor `6.9.0` against a `6.10.0` props file since pre-story commit `3c90c33`) and were silently re-baselined to new values without disclosure; names now contradict behavior (they assert approved canonical values, not "unchanged" pins) [tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs:205]
- [x] [Review][Defer] Workspace-mode enforcement is not wired into any CI: Builds CI runs the consumer validator only against itself and the exception validator only in schema mode; ChatBot `ci.yml`/`release.yml` invoke neither, so the closed allowlist and consumer authority are enforced only by convention plus ChatBot's architecture tests [references/Hexalith.Builds/.github/workflows/build-release.yml] — deferred, owned by Story 1.1f (CI/CD alignment proposal)
- [x] [Review][Defer] Partial/standalone checkout rough edges: Timesheets `$(HexalithEventStoreRoot)` expands empty without a guard (rooted-path project-not-found error), the `.gitmodules` path move leaves stale `Hexalith.Builds/` directories and `.git/modules` config in existing clones, Conversations' sibling-Commons probe checks 1 of the 7 directories its nested branch requires, and the Builds README consumer example shows an `Exists`-guarded import with no fail-closed fallback unlike the sample/ChatBot wrapper [references/Hexalith.Timesheets/src/Hexalith.Timesheets.Server/Hexalith.Timesheets.Server.csproj:12] — deferred, pre-existing pattern conventions

## Dev Notes

### Discovery Results and Source Hierarchy

- Loaded `{epics_content}` from one whole file, `_bmad-output/planning-artifacts/epics.md`; Epic 1 and all Stories 1.1a-1.21 were analyzed, with Story 1.1e at lines 718-750.
- Loaded `{architecture_content}` from one whole file, `_bmad-output/planning-artifacts/architecture.md`; the binding shared-authority invariant is at lines 130-138, with matching structure, test, and workflow rules later in the document.
- The workflow's configured PRD and UX globs do not reach this repository's extra nesting level. The approved change proposal explicitly records that the complete PRD and binding UX package were reviewed and require no changes; they are not package-version sources for this story.
- Loaded the complete approved `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md`; its Section 6 migration contract and Section 7 sequence take precedence for exact versions and rollout.
- Loaded the historical parent `_bmad-output/implementation-artifacts/1-1-scaffold-the-buildable-hexalith-chatbot-module.md`; there is no dedicated prior 1.1d story file, so the parent supplies the closest implementation/review intelligence.
- Loaded all allowed persistent project-context facts found by `{project-root}/**/project-context.md` in Conversations, EventStore, Folders, FrontComposer, Memories, Parties, Projects, and Tenants. Repository-specific build/test rules differ and must be followed in the owning repository.
- Source precedence for this story: `epics.md` Story 1.1e for canonical story/AC wording; approved proposal Sections 6-7 for exact versions/sequence unless a later explicit planning/architecture disposition reconciles the recorded post-proposal drift; architecture shared-authority invariant; current repository files for implementation state; historical Story 1.1 only for lessons, not current version authority. Recency of a package commit is evidence of current state, not by itself approval to replace the matrix.

### Implementation Intent and Scope

This is a coordinated build-governance correction across an umbrella workspace. It centralizes package-reference version decisions; it does not implement a feature, alter a runtime contract, change UI/UX, persist data, add a product dependency, or rewrite historical Stories 1.1a/1.1d. Product behavior must remain unchanged except for compatibility fixes strictly required by the approved canonical versions.

The catalog-first order is load-bearing: update and validate `Hexalith.Builds`, then migrate and verify each consumer in its owning repository, then integrate verified submodule pointers, then run umbrella gates. Removing consumer definitions before the catalog is complete can produce NU1010 restore failures; adding a local override to bypass a conflict defeats the story.

### Current UPDATE Files and Preservation Requirements

| File/surface | Current behavior | Story change | Must preserve |
|---|---|---|---|
| `Directory.Packages.props` | Root-local catalog with 57 `PackageVersion` rows and CPM enabled. Committed pre-story work already aligns CommunityToolkit DAPR and Fluxor rows with part of the approved matrix. | Replace rows with an explicit import-only wrapper. | Preserve unrelated committed work and all legal non-version CPM settings; do not reintroduce old values. |
| `references/Hexalith.Builds/Props/Directory.Packages.props` | Shared catalog with 268 rows at the recorded baseline, shared Hexalith family properties, and post-proposal OpenTelemetry changes; still missing the 15 approved additions. | Reconcile the known drift, add the exact approved/reconciled rows, set override protection, and validate evaluated uniqueness/resolution. | Existing family/property authority and DAPR-family validation. |
| `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1` | Evaluates `PackageVersion` items and rejects blank/unresolved/tag-prefixed/malformed versions. It does not currently enforce case-insensitive uniqueness or consumer exclusivity. | Extend integrity checks and pair with consumer source/evaluation validation. | Existing diagnostics, evaluator test seam, and release ordering. |
| `references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1` | Covers valid and malformed versions plus workflow ordering. | Add duplicate/effective-catalog cases and consumer-authority fixture coverage. | Hermetic temporary fixtures and fail-closed result checking. |
| Builds `README.md` and sample wrapper | The README explicitly says consumers may add repository-specific `PackageVersion` rows; sample leaves an ItemGroup for them. | Remove that escape-hatch guidance and document sole ownership/exceptions. | Robust import paths for standalone and umbrella use. |
| Seven consumer props files with local definitions | Local rows/updates/property overrides change or duplicate effective versions. | Remove all dependency version declarations after Builds is ready. | Existing import path fallbacks and per-repository transitive-pinning posture. |
| Five already version-free wrappers | Import Builds through repository/umbrella fallback paths. | Validate and minimally normalize only if required. | Standalone and umbrella evaluation. |
| `ScaffoldArchitectureTests.cs` | Reads root wrapper text, hardcodes MCP `1.4.0`, checks only `PackageReference Version` attribute/nested element, and only asserts root CPM text. | Evaluate the imported catalog and enforce wrapper/override/exclusive-ownership rules. | MCP adapter dependency boundary, submodule and other scaffold guards. |
| Four ChatBot UI contract test files | Read root wrapper text and hardcode old local pins. | Read/evaluate authoritative catalog values via a shared helper. | Accessibility, responsive, localization, and live-region product assertions. |
| `.github/workflows/ci.yml` and `release.yml` | Story 1.1c topology acceptance work is committed in the baseline. | Change only if package cache/authority gates require it. | All topology lanes and non-recursive submodule setup. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Story `1-1c` is already `done`; Story 1.1e is `backlog`. | Change only Story 1.1e to `ready-for-dev` during story creation. | Every other status, comment, order, and the existing `last_updated` date. |

The recorded baseline was clean before this story artifact was created. Recent root commits `8c3dd15`, `dfe31fc`, and `9567f43` captured Story 1.1c/runtime work and submodule pointer updates; those changes are not Story 1.1e completion evidence and must not be reset, overwritten, or misattributed to this story.

### Binding Catalog Additions

Only the authoritative Builds catalog may use `PackageVersion Include` for these additions.

| Package ID | Canonical version | Decision |
|---|---:|---|
| `Dapr.AI` | `1.18.4` | Preserve Memories effective version. |
| `Dapr.AI.Microsoft.Extensions` | `1.18.4` | Preserve Memories effective version. |
| `Fluxor` | `6.10.0` | Align with shared `Fluxor.Blazor.Web`. |
| `Kreuzberg` | `4.10.2` | Preserve Memories effective version. |
| `Microsoft.AspNetCore.Components.CustomElements` | `10.0.10` | Align with shared ASP.NET Core 10.0.10 family. |
| `Microsoft.Extensions.Diagnostics.Abstractions` | `10.0.10` | Preserve Commons effective version. |
| `MinVer` | `8.0.0-rc.1` | Preserve common local version. |
| `NBomber.Http` | `6.2.1` | Preserve EventStore effective version. |
| `NFalkorDB` | `1.0.6` | Preserve Memories effective version. |
| `NRedisStack` | `1.6.0` | Preserve Memories effective version. |
| `NetArchTest.eNhancedEdition` | `1.4.5` | Preserve ChatBot effective version. |
| `OpenTelemetry` | `1.17.0` | Resolve ChatBot/Memories split to shared OTel core line. |
| `OpenTelemetry.Exporter.InMemory` | `1.17.0` | Preserve Memories effective version. |
| `OpenTelemetry.Instrumentation.StackExchangeRedis` | `1.16.0-beta.1` | Preserve documented prerelease. |
| `xunit.v3.extensibility.core` | `3.2.2` | Align with shared xUnit v3. |

### Binding Conflict Resolution

The approved proposal says the Builds value wins for these 30 conflicting package IDs. These are approved migrations, not invitations to choose newer versions. The post-proposal drift described immediately below is deliberately not folded into this table without an explicit disposition.

| Package ID | Canonical Builds value |
|---|---:|
| `ByteAether.Ulid` | `1.3.8` |
| `CommunityToolkit.Aspire.Hosting.Dapr` | `13.4.1-beta.686` |
| `Fluxor.Blazor.Web` | `6.10.0` |
| `MediatR` | `14.2.0` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.10` |
| `Microsoft.AspNetCore.Mvc.Testing` | `10.0.10` |
| `Microsoft.AspNetCore.OpenApi` | `10.0.10` |
| `Microsoft.AspNetCore.TestHost` | `10.0.10` |
| `Microsoft.Extensions.Configuration.Binder` | `10.0.10` |
| `Microsoft.Extensions.DependencyInjection` | `10.0.10` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.10` |
| `Microsoft.Extensions.Hosting` | `10.0.10` |
| `Microsoft.Extensions.Hosting.Abstractions` | `10.0.10` |
| `Microsoft.Extensions.Http` | `10.0.10` |
| `Microsoft.Extensions.Http.Resilience` | `10.8.0` |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.10` |
| `Microsoft.Extensions.Options` | `10.0.10` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.10` |
| `Microsoft.Extensions.ServiceDiscovery` | `10.8.0` |
| `Microsoft.NET.Test.Sdk` | `18.8.1` |
| `ModelContextProtocol` | `1.4.1` |
| `ModelContextProtocol.AspNetCore` | `1.4.1` |
| `NSubstitute` | `6.0.0` |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | `1.17.0` |
| `OpenTelemetry.Extensions.Hosting` | `1.17.0` |
| `OpenTelemetry.Instrumentation.Runtime` | `1.16.0` |
| `System.CommandLine` | `2.0.10` |
| `Testcontainers` | `4.13.0` |
| `YamlDotNet` | `18.1.0` |
| `bunit` | `2.8.4-preview` |

EventStore's consumer-local `HexalithCommonsVersion=2.28.0` compatibility override is removed; the shared catalog default `2.28.2` applies. The 26 remaining local package IDs already equal the catalog and require declaration removal only.

### Post-Proposal Baseline Drift Requiring Disposition

The story was generated after the proposal but against a newer clean baseline (`9567f43`, Builds `a8933ae`). A fresh source inventory still finds 102 local rows, 71 unique local IDs, and the same 15 missing IDs, but the authoritative catalog now contains 268 rows and the unique conflict count is 31 rather than 30:

| Package ID | Proposal/current consumer value | Current Builds value | Required action before migration |
|---|---:|---:|---|
| `OpenTelemetry.Instrumentation.AspNetCore` | Not in approved conflict table / `1.16.0` | `1.17.0` | Explicitly approve the Builds value or restore an approved catalog target; then update the effective matrix. |
| `OpenTelemetry.Instrumentation.Http` | Not in approved conflict table / `1.16.0` | `1.17.0` | Explicitly approve the Builds value or restore an approved catalog target; then update the effective matrix. |
| `OpenTelemetry.Instrumentation.Runtime` | Approved target `1.16.0`; consumers use `1.15.1` | `1.17.0` | Resolve the changed target explicitly; do not silently apply either `1.16.0` or `1.17.0`. |

The catalog-size increase also includes newly integrated shared Hexalith package authority such as `Hexalith.Chatbot.Contracts`; it must be preserved unless the reconciliation proves otherwise. Once the disposition is recorded, regenerate the full evaluated matrix and use that reconciled artifact as AC3 evidence.

### Consumer Migration Map

| Repository | Starting local definitions | Required action |
|---|---:|---|
| ChatBot superproject | 57 | Replace local catalog with version-free shared import; update evaluated pin tests. |
| EventStore | 3 rows + 1 property | Remove rows and Commons compatibility override. |
| Tenants | 0 | Retain and validate version-free wrapper. |
| FrontComposer | 0 | Retain and validate version-free wrapper. |
| Folders | 0 | Retain and validate version-free wrapper. |
| Conversations | 0 | Retain and validate version-free wrapper. |
| Projects | 0 | Retain and validate version-free wrapper. |
| Parties | 4 | Remove rows/updates; remediate against shared versions. |
| Memories | 13 | Remove rows/updates and compatibility comments; remediate against shared versions. |
| Commons | 2 | Remove rows; consume shared values. |
| Timesheets | 22 | Add root Builds dependency, replace local catalog with wrapper, remediate shared-version changes. |
| PolymorphicSerializations | 1 | Remove row; consume shared value. |
| AI.Tools | N/A | No .NET consumer migration. |
| Builds | Authority | Add missing IDs, validation, CI, docs, sample, and exception inventory. |

### Validation Design Guardrails

- NuGet automatically imports only the nearest `Directory.Packages.props`; the consumer wrapper must explicitly import Builds. A catalog merely existing elsewhere in the umbrella does not govern a project.
- `PackageVersion Include` declares an item; `Update` changes metadata on an imported item. A consumer `Update` is therefore a local override even when evaluated metadata reports the shared catalog as the defining project. Source scanning and effective-value comparison are both required.
- Project `PackageReference` items retain metadata such as `PrivateAssets` and `IncludeAssets` but no version attribute, nested `Version`, or `VersionOverride`. A missing central row should fail rather than be patched locally.
- Treat NuGet package IDs case-insensitively. Duplicate `Include` items can produce NU1506 and inconsistent restore.
- Set `CentralPackageVersionOverrideEnabled=false`; reject any consumer project that disables CPM. Do not mistake SDK-generated `IsImplicitlyDefined=true` references for catalog omissions.
- Preserve existing transitive-pinning settings. Enabling transitive pinning can promote dependencies into packed `.nuspec` output and causes attempted downgrades to fail with NU1109; it is not a global cleanup task here.
- There are no `packages.lock.json` files in the analyzed workspace, so no lock-file-specific `--force-evaluate` policy is needed. NuGet source mapping and the repository's current NuGet audit policy are out of scope.

### CPM-Incompatible Exception Inventory Baseline

The validator must carry the exact baseline rather than relying on a count.

**AppHost SDK declarations:**

- ChatBot, EventStore, Folders, FrontComposer, Memories, Parties, Projects, Tenants, and Timesheets use `Aspire.AppHost.Sdk/13.4.6`.
- Conversations currently uses `Aspire.AppHost.Sdk/13.4.2`; the shared `Aspire.Hosting` family is `13.4.6`, so this requires alignment or a named architecture exception.

**Tool manifests:**

| Repository | Tool | Version |
|---|---|---:|
| EventStore | `defaultdocumentation.console` | `1.2.5` |
| EventStore | `hexalith.eventstore.admin.cli` | `3.48.0` |
| FrontComposer | `docfx` | `2.78.5` |
| FrontComposer | `dotnet-stryker` | `4.16.0` |
| Parties | `aspirate` | `9.1.0` |

SDK-controlled implicit references are a separate NuGet mechanism and are not added to the exception inventory merely because they lack `PackageVersion` rows.

### Architecture and Repository Compliance

- .NET SDK remains `10.0.302`, target `net10.0`, nullable/implicit usings/warnings-as-errors. Use `.slnx`, never `.sln`.
- Package migration does not authorize broad framework cleanup. Proposal Section 6 controls exact versions even where older project-context or architecture tables lag.
- Keep each consumer's dependency direction and Debug-source/Release-package behavior. Package compatibility fixes belong in the consumer, not in a catalog override.
- Root-declared submodules only; no recursive or remote update. Changes inside a submodule belong to that repository and require separate validation/commit intent.
- No UI implementation or persistence change occurs, so the Hexalith UX/state workflows are not activated. The four UI test files change only how package pins are read.

### Suggested File Structure

Likely UPDATE files:

```text
references/Hexalith.Builds/
  Props/Directory.Packages.props
  Directory.Packages.props
  README.md
  Samples/Module.Directory.Packages.props
  Tools/README.md
  Tools/validate-central-package-versions.ps1
  Tools/test-central-package-version-validator.ps1
  .github/workflows/build-release.yml
  .github/workflows/domain-ci.yml                 # only if reusable validation/cache contract changes
  .github/workflows/domain-release.yml            # only if reusable validation/cache contract changes

Directory.Packages.props
references/Hexalith.EventStore/Directory.Packages.props
references/Hexalith.Parties/Directory.Packages.props
references/Hexalith.Memories/Directory.Packages.props
references/Hexalith.Commons/Directory.Packages.props
references/Hexalith.Timesheets/Directory.Packages.props
references/Hexalith.PolymorphicSerializations/Directory.Packages.props
references/Hexalith.Timesheets/.gitmodules

tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs
tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs
tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs
tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs
tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs
```

Likely NEW Builds-owned artifacts, unless the existing validator is cleanly extended instead:

```text
references/Hexalith.Builds/Tools/validate-consumer-package-version-authority.ps1
references/Hexalith.Builds/Tools/test-consumer-package-version-authority.ps1
references/Hexalith.Builds/Tools/package-version-exceptions.json
```

Likely NEW ChatBot test support, when this is cleaner than extending the existing files in place:

```text
tests/Hexalith.ChatBot.Architecture.Tests/CentralPackageAuthorityTests.cs
tests/Hexalith.ChatBot.UI.Tests/SharedPackageCatalog.cs
```

Keep shared-catalog evaluation in one helper per test assembly; do not introduce another production package-resolution abstraction for test-only governance.

Already version-free consumer wrappers may need no content edit. Do not manufacture diffs merely to make every repository appear changed.

### Testing Requirements

- `Hexalith.Builds`: run catalog validator, DAPR-family validator, new consumer-authority/exception fixture suites, `dotnet restore Hexalith.Builds.slnx`, canonical Release build, and its documented test projects/pack qualification.
- Each consumer: restore its canonical `.slnx`, build with its repository-required configuration/serialization, run focused compatibility tests and package-only Release validation where configured, and capture resolved graph evidence for changed canonical versions.
- FrontComposer is an explicit exception to the common per-project test preference: its tracked context requires solution-level tests with trait filters and `DiffEngine_Disabled=true`. Follow the owning repository.
- Parties uses Microsoft.Testing.Platform; `dotnet test --filter` can run zero tests. Use its `scripts/test.ps1` lanes or the built test executable with xUnit v3 `-class`/`-method` filters.
- ChatBot focused evidence must include Architecture.Tests and UI.Tests after a clean/no-incremental build. Preserve the already-green Story 1.1c topology lane; run relevant integration smoke after the package graph changes.
- Do not weaken tests to match canonical packages. Compatibility source/test changes and their rationale belong in the affected repository's evidence.

### Previous Story Intelligence

- Story 1.1a established root CPM but treated the root file as the catalog; Story 1.1d rejected only inline project versions. Story 1.1e supersedes those package-authority assumptions without rewriting their historical records.
- Historical guidance to seed from Folders, keep versions in the root local props, and avoid normalizing sibling differences is no longer authoritative. The approved matrix and Builds catalog now control.
- MCP pin assertions were previously hand-updated from 1.3.0 to root-local 1.4.0. They must now evaluate shared 1.4.1 so wrapper text cannot create a false pass.
- Preserve `BuildInParallel=false`/repository-specific serialized builds. A prior WSL incremental build ran stale test binaries after pin assertions changed; use a clean/no-incremental focused build when evidence looks inconsistent.
- If standard `dotnet test` is environment-blocked, follow the baseline fallback ladder and record both the broad blocker and focused direct-runner evidence. Do not claim an unrun lane passed.
- Root-only non-recursive submodule initialization was added after review because `submodules: false` alone could not restore dependencies. Keep the explicit root setup and never replace it with recursive checkout.

### Git Intelligence

- Planning commit `856b4a2` added Story 1.1e to the epics, the approved proposal, the architecture invariant, and the sprint row. Its commit body incorrectly suggests migration completion; the proposal's final execution log and actual files explicitly show implementation was not started.
- The story baseline is root commit `9567f43`; root `main` and `origin/main` matched, and the only root worktree addition was this story artifact. Commits `8c3dd15`, `dfe31fc`, and `9567f43` integrated previously dirty work and current Builds/Projects pointers after the proposal.
- Checked-out `references/Hexalith.Builds` is clean at `a8933ae` and matches its `origin/main`. Its catalog still lacks the 15 additions, but it includes post-proposal OpenTelemetry changes and `Hexalith.Chatbot.Contracts`; do not confuse a synchronized pointer with Story 1.1e completion.
- Reinspect root and owning-repository state immediately before implementation because coordinated commits may continue to move these baselines. Preserve and report overlap rather than resetting or absorbing it.

### Latest Technical Information

- NuGet CPM and import behavior: [Microsoft Learn - Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
- `PackageVersion Update` changes imported item metadata; it is not a declaration substitute: [Microsoft Learn - MSBuild items](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-items?view=vs-2022#updating-metadata-on-items-in-an-itemgroup-outside-of-a-target).
- Duplicate central items can raise NU1506: [Microsoft Learn - NU1506](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1506).
- Project-local versions and missing central rows fail with NU1008/NU1010: [NU1008](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1008), [NU1010](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1010).
- Evaluated properties/items can be queried without building through MSBuild: [Microsoft Learn - Evaluate MSBuild items and properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/evaluate-items-and-properties?view=visualstudio).
- Transitive pinning behavior and `VersionOverride` controls are documented in the CPM guide; preserve current per-repository settings rather than normalizing them.
- Current .NET 10 CLI resolved-graph command: [Microsoft Learn - `dotnet package list`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-list).
- No latest-package research overrides the approved migration matrix. Do not add unrelated package upgrades, NuGet source/audit changes, lock-file policy, or global transitive-pinning changes.

### Out of Scope

- PRD, UX, actors, commands, events, APIs, data contracts, persistence, user journeys, runtime behavior, or UI component changes.
- Rewriting completed Story 1.1a/1.1d evidence or claiming their historical local-catalog assumptions are still binding.
- Any dependency version change not listed in the 15-addition/30-conflict/property matrix, a subsequent explicit disposition of the recorded post-proposal drift, or the approved SDK-family alignment rule.
- NuGet source/mapping changes, audit-policy changes, lock-file introduction, blanket transitive-pinning enablement, or forcing SDK/tool/implicit package versions into CPM.
- A local compatibility override, warnings-as-errors suppression, weakened test, or disabled package-only validation.
- Recursive/remote submodule update, implicit nested-submodule initialization, implicit commit/push/release, or unrelated submodule pointer movement.

### Validation Notes

Checklist review applied before finalization:

- Reinvention prevention: reuse Builds' evaluated central validator, fixture pattern, release workflow, sample wrapper, and consumer-specific test lanes.
- Wrong-location prevention: authority, wrapper, validator, CI/docs, consumer props, ChatBot tests, Timesheets dependency, and exception inventory locations are explicit.
- Regression prevention: catalog-first order, exact matrix plus a fail-closed post-proposal reconciliation gate, work preservation, effective-value comparison, package-only checks, clean test rebuild, and independent owning-repository evidence are binding.
- Scope control: no product/UX/persistence changes, broad package upgrades, transitive-pinning normalization, source/audit changes, or implicit Git/submodule operations.
- Completion-truth prevention: final evidence requires evaluated zero-local-definition proof and exact per-repository commands/results; wrapper text or a green root build alone is insufficient.

### References

- [Source: AGENTS.md]
- [Source: references/Hexalith.AI.Tools/hexalith-llm-instructions.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/implementation-artifacts/1-1-scaffold-the-buildable-hexalith-chatbot-module.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-1-First-Safe-Governed-Action-Command-Spine]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1e-Centralize-NuGet-package-reference-version-authority]
- [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints-Dependencies]
- [Source: _bmad-output/planning-artifacts/architecture.md#Structure-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#File-Organization-Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Development-Workflow-Integration]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#Technical-Migration-Contract]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#Shared-validation-and-CI]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#CPM-incompatible-exceptions]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-18.md#Implementation-Sequence-and-Success-Criteria]
- [Source: Directory.Packages.props]
- [Source: .gitmodules]
- [Source: references/Hexalith.Builds/Props/Directory.Packages.props]
- [Source: references/Hexalith.Builds/Tools/validate-central-package-versions.ps1]
- [Source: references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1]
- [Source: references/Hexalith.Builds/README.md#Import-Central-Package-Versions]
- [Source: references/Hexalith.Builds/Samples/Module.Directory.Packages.props]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs]
- [Source: tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs]
- [Source: references/Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: references/Hexalith.Folders/_bmad-output/project-context.md]
- [Source: references/Hexalith.FrontComposer/_bmad-output/project-context.md]
- [Source: references/Hexalith.Memories/_bmad-output/project-context.md]
- [Source: references/Hexalith.Parties/_bmad-output/project-context.md]
- [Source: references/Hexalith.Projects/_bmad-output/project-context.md]
- [Source: references/Hexalith.Tenants/_bmad-output/project-context.md]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-19 architecture correction directed by Jerome: Hexalith submodules must never initialize submodules nested beneath them. Every nested checkout initialized during the completion-evidence attempt was deinitialized with exact, non-recursive pathspecs in Tenants, FrontComposer, Folders, Commons, Parties, Conversations, and Timesheets. `git submodule status` now reports every nested gitlink with the `-` prefix, while all ChatBot root-declared submodules remain initialized. The uncommitted duplicate `AssemblyInfo.cs` in Commons' nested Polymorphic checkout was removed by deinitialization; the authoritative source change remains only in ChatBot's root-level `references/Hexalith.PolymorphicSerializations`. The canonical-build evidence obtained from initialized nested checkouts is invalid for the corrected architecture and does not satisfy the remaining task. Dependency resolution must use a module's own `references/` only in a standalone checkout and the superproject sibling directory via `../references/` when embedded.
- 2026-07-19 authorized blocker-clearance continuation at ChatBot `9a6c077` / Builds `cb8b2d4`: Jerome explicitly authorized exact non-recursive initialization of the already-declared nested dependencies, PolymorphicSerializations source cleanup, and restoration of Timesheets' tracked Works checkout. Only declared dependency paths were initialized; no recursive/remote submodule update, commit, stage, push, or dependency-version override was used. Stale nested dependency gitlinks in Commons, Parties, and Conversations were aligned from the corresponding local root-declared checkouts so their standalone solutions evaluate the same catalog and source graph as the umbrella.
- PolymorphicSerializations' 15 deterministic `IDE0065` failures were fixed by placing `using` directives before file-scoped namespaces in the 13 reported files; the redundant `System` import was removed and an assembly-level CLS-compliance declaration was added for the stronger Commons analyzer graph. Its serialized Release restore/build now passes with zero warnings/errors and its test suite passes 15/15. The same uncommitted `AssemblyInfo.cs` is present in Commons' nested Polymorphic checkout solely so Commons can validate the pending cross-repository integration order.
- Independent canonical Release restore/build evidence is green with zero warnings/errors for Builds, EventStore, Tenants, FrontComposer, Folders, Conversations, Projects, Parties, Memories, Commons, Timesheets, and PolymorphicSerializations. Newly affected focused lanes pass: Tenants Server 738/738; FrontComposer Shell 2372/2372 before the final gitlink expectation correction and its renewed governance lane 21/21 afterward; Folders Server 565/565 and UI 521/521; Commons 355/355; Conversations Contracts 618/618, Server 610/610, core 185/185, Admin Web 14/14, AppHost 7/7, Client 29/29, Integration 9/9, and ServiceDefaults 7/7; Parties package, retired-path, and isolated benchmark checks; PolymorphicSerializations 15/15; Timesheets Server 420/420 and Works 76/76. The Conversations full conformance lane has 398/400 and Folders Contracts has 281/283, with only unrelated planning-document integrity drift remaining.
- Final governance rerun passes the central, consumer-authority, exception, authoritative-catalog, Dapr-family, and workflow fixtures at 14/14, 16/16, 7/7, 48/48, 29/29, and 20/20. Production validation passes the 283-entry catalog, all eight Dapr identities at `1.18.4`, the exact 15-entry exception allowlist, and all twelve actual consumers across 279 tracked projects; Builds' own six projects pass the same authority validation, reconciling the approved 285-project workspace inventory. ChatBot restore and its serialized warning-as-error Release build pass with zero warnings/errors; focused umbrella evidence passes Scaffold Architecture 28/28, UI 227/227, AppHost 12/12, and non-live integration 19/19.
- The mandatory full ChatBot regression sweep was run and prevents Step 8/9 completion under the dev-story workflow: Architecture passes 61/63 but fails two pre-existing planning-ADR text assertions (`AI-response streaming transport` Story 10.6a and Domain Service SDK adoption Story 11.2); UI E2E passes 138/139 but fails the pre-existing Story 10.6a sprint-status assertion; every other ChatBot test project passes, including Server 1690/1690 and Integration 19 passed with three live-topology skips. These planning artifacts are outside Story 1.1e's build-governance scope, so no completion checkbox, story status, or sprint status is advanced. The story remains `in-progress` until the regression baseline is reconciled by its owning planning work.
- 2026-07-19 live completion-evidence rerun at ChatBot `9a6c077` / Builds `cb8b2d4` (with the pre-existing uncommitted authoritative-fixture rebaseline from EventStore Contracts `3.74.0` to Jerome's accepted `3.75.0`): the central-package, consumer-authority, exception, authoritative-catalog, Dapr-family, and reusable-workflow suites pass 14/14, 16/16, 7/7, 48/48 governed values, 29/29, and 20/20 assertions. Production validation passes for the 283-entry catalog, the closed 15-entry exception workspace, and all twelve consumers across 285 tracked projects (22 ChatBot, 48 EventStore, 17 Tenants, 23 FrontComposer, 32 Folders, 17 Conversations, 23 Projects, 29 Parties, 29 Memories, 20 Commons, 15 Timesheets, and 4 PolymorphicSerializations). Builds restore/Release build pass with zero warnings/errors and its three test projects pass 64/64. EventStore, Memories, and Projects CI canonical restores/Release builds pass with zero warnings/errors. Timesheets restore and its serialized canonical Release build pass with zero warnings/errors using the documented outer EventStore/Polymorphic roots, but the current `Hexalith.Works` checkout is pre-existing workspace state at `27388cb` rather than the tracked `f2259da`, so this run is not attributed as clean tracked-gitlink evidence. PolymorphicSerializations restore passes, but two consecutive identical serialized Release builds fail the same 15 pre-existing `IDE0065` diagnostics. Tenants, FrontComposer, Folders, Commons, Parties, and Conversations canonical restores fail `MSB3202` because their solutions name projects in intentionally uninitialized nested submodules. Nested initialization and unrelated Polymorphic source cleanup are prohibited/out of scope, so the first completion-evidence subtask remains unchecked and the ordered workflow does not advance to renewed umbrella proof.
- 2026-07-19 Timesheets dependency follow-up authorized by Jerome: from `references/Hexalith.Timesheets`, `git submodule update --init -- Hexalith.Works` initialized the already-declared Timesheets root submodule at tracked commit `f2259daab922096113262fc9e0a5588182918e0a`; `git submodule status Hexalith.Works` confirms that exact gitlink, no submodule within Works was initialized, and the Timesheets tracked tree remains clean. This resolves the absent checkout only in the current workspace; no tracked pointer, manifest, or clone automation changed. `dotnet test tests/Hexalith.Timesheets.Works.Tests/Hexalith.Timesheets.Works.Tests.csproj --configuration Release --no-build --no-restore` passes 76/76. The serialized Release restore/build used `-p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/chatbot/references/Hexalith.EventStore -p:HexalithPolymorphicSerializationsRoot=/home/administrator/projects/hexalith/chatbot/references/Hexalith.PolymorphicSerializations`: restore passed; the first `dotnet build Hexalith.Timesheets.slnx --configuration Release --no-restore -warnaserror -m:1 /nr:false` invocation reproduced the documented transient 15-error `IDE0065` Polymorphic build; its required immediate identical rerun passed with zero warnings and zero errors. No analyzer gate was weakened.
- 2026-07-19 post-disposition completion evidence at ChatBot `2b3f04d` / Builds `cb8b2d4`: rebaselining the authoritative fixture to Jerome's accepted `HexalithEventStoreVersion` `3.75.0` restores all 48/48 governed-value assertions. The central, consumer-authority, exception, and Dapr fixtures pass 14/14, 16/16, 7/7, and 29/29 scenarios; production validation passes for the 283-entry catalog, eight Dapr IDs, the closed 15-entry exception inventory, Builds itself, and all twelve consumers across 285 tracked projects. Builds restore/Release build and 64/64 tests pass; ChatBot restore/serialized Release build pass with zero warnings or errors; EventStore, Memories, and Projects canonical Release builds pass; Timesheets Server builds and its 420/420 tests pass. Focused green evidence includes ChatBot Scaffold Architecture 28/28, UI 227/227, AppHost 12/12, and non-live integration 19/19; Memories EventStore 129/129, Web 492/492, and Server 2734/2734; Tenants 1247/1247; Conversations Contracts 618/618; EventStore Client 673/673 and Server 2740/2740 with 25 skipped. At this evidence point, completion remained `in-progress`: canonical solutions for Commons, Parties, Tenants, FrontComposer, Folders, and Conversations required intentionally uninitialized nested projects; Timesheets lacked its root-declared `Hexalith.Works` checkout (superseded by the later follow-up above); PolymorphicSerializations failed 15 IDE0065 diagnostics; and several focused governance fixtures hard-coded absent nested dependency paths. The full ChatBot Architecture suite also had 61 passes and two failures against unrelated planning-ADR text. No nested dependency was initialized during that evidence run and no unrelated test or analyzer gate was weakened.
- 2026-07-19 explicit follow-up disposition approved by Jerome: accept `HexalithEventStoreVersion` `3.75.0` as the authoritative shared NuGet family for Story 1.1e and rebaseline the governed catalog contract. The disposition is recorded in `_bmad-output/planning-artifacts/.decision-log.md`; it is deliberately limited to package-reference resolution and does not reclassify EventStore v3.75.0 container/provenance evidence or release-readiness gates.
- 2026-07-19 completion-evidence halt at ChatBot `2b3f04d` / Builds `cb8b2d4`: `pwsh -NoLogo -NoProfile -File Tools/test-authoritative-package-catalog.ps1` fails because `Hexalith.EventStore.Contracts` evaluates to `3.75.0` while the approved authoritative contract expects `3.74.0`. Builds commit `4bbe7c0` changed `HexalithEventStoreVersion` from `3.74.0` to `3.75.0`, and root commit `2b3f04d` integrated that Builds state, but `_bmad-output/planning-artifacts/.decision-log.md` authorizes only `3.74.0` and explicitly requires a new disposition for any further shared version change. The central validator (14 scenarios), consumer-authority validator (16 scenarios), exception validator (7 scenarios), and Dapr validator (29 scenarios) pass. No catalog, validator, consumer, dependency, sprint-status, or version change was made; independent consumer and umbrella completion evidence remains halted pending Jerome's explicit choice to accept `3.75.0` and rebaseline the governed contract or restore the catalog to the approved `3.74.0` value.
- 2026-07-18 baseline re-audit at ChatBot `ae6b31b` / Builds `a8933ae`: all owning repositories were inspected before editing; Builds is clean and three commits behind `origin/main`, all other owning repositories are clean, and the root preserves the user-owned untracked CI/CD alignment proposal. The root-declared repository inventory contains 285 tracked `.csproj` files versus the approved snapshot's 281. Package evidence otherwise matches the story baseline: 14 package-props files, 268 evaluated unique catalog rows, 102 consumer-local rows across 71 unique IDs, 15 missing IDs, 31 conflicts, 25 already-equal IDs, the EventStore `HexalithCommonsVersion` override, ten AppHost SDK pins, and five tool pins. Implementation halted before catalog/consumer edits pending an explicit planning/architecture disposition for `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, and `OpenTelemetry.Instrumentation.Runtime`, plus reconciliation of the project-count discrepancy.
- 2026-07-18 explicit disposition approved by Jerome and recorded in the planning decision log: use shared `1.17.0` for `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, and `OpenTelemetry.Instrumentation.Runtime`; accept 285 tracked `.csproj` files as the corrected inventory baseline without changing the remainder of the approved migration matrix.
- 2026-07-18 consumer migration restore halt: `dotnet restore Hexalith.ChatBot.slnx` fails with NU1109 because the authoritative catalog pins `Hexalith.EventStore.Contracts` to `3.70.2`, while `Hexalith.EventStore.Server 3.71.0` requires Contracts `>=3.71.0` and Tenants preserves `CentralPackageTransitivePinningEnabled=true`. This catalog-internal conflict was not in the approved migration matrix. No local override or version change was applied; explicit disposition is required before migration continues. EventStore restore independently passed. Parties' standalone restore separately remains blocked by intentionally uninitialized nested source dependencies, while its complete source/evaluation package-authority validation passes.
- 2026-07-18 explicit follow-up disposition approved by Jerome: set the authoritative `Hexalith.EventStore.Contracts` value to `3.71.0`, matching the already-authoritative EventStore server family and resolving the evidenced NU1109; no other version change was authorized.
- 2026-07-18 implementation refinement directed by Jerome: source `Hexalith.EventStore.Contracts` from the shared `$(HexalithEventStoreVersion)` family property; evaluated authority remains `3.71.0`.
- 2026-07-18 code review (post-implementation): Builds commits `1484ee3`/`8fd1b07` moved `HexalithEventStoreVersion` to `3.74.0` and `HexalithTenantsVersion` to `3.2.18` after the disposition above, and `c177c66` re-aligned the catalog fixture without a disposition. Jerome accepted both values during review; the superseding disposition is recorded in `_bmad-output/planning-artifacts/.decision-log.md`. Evaluated authority is now `3.74.0`; the `3.71.0` figures in earlier entries and completion notes describe the pre-drift state.

### Implementation Plan

- Complete and verify the Builds catalog before removing any consumer-local definitions.
- Extend fail-closed catalog, consumer-authority, and exception validators with red/green fixtures.
- Wire the validators into Builds documentation, samples, and CI, then verify Builds independently.
- Migrate and validate each owning consumer repository, update ChatBot governance tests, and finish with independent and umbrella evidence.

### Completion Notes List

- Nested-submodule initialization has been fully reversed following Jerome's architecture correction. All nested gitlinks are uninitialized and their temporary working-tree gitlink changes are gone. The prior canonical consumer-build evidence that depended on those checkouts is historical only; Story 1.1e remains `in-progress` pending compliant `references/` versus `../references/` dependency resolution and renewed evidence.
- Jerome's explicit authorization cleared every previously recorded dependency-checkout, Works, and Polymorphic analyzer blocker. All required package-authority acceptance lanes, independent canonical builds, focused consumer tests, and renewed umbrella evidence are green without a local version escape hatch. This supersedes the older blocker descriptions below, which remain as historical evidence.
- Completion is still halted by the dev-story Definition of Done rather than by Story 1.1e functionality: the mandatory full regression sweep reproduces three unrelated planning-integrity failures (two ChatBot Architecture ADR assertions and one UI E2E Story 10.6a sprint-status assertion). Conversations and Folders likewise each retain two planning-document integrity failures outside the package migration. Because the workflow requires every regression to pass, the final evidence task and its two remaining subtasks stay unchecked and both story/sprint status remain `in-progress`.
- The 2026-07-19 live rerun reconfirmed every package-authority validator, all 285 tracked consumer project evaluations, Builds 64/64 tests, and clean canonical Release builds for Builds, EventStore, Memories, and Projects CI. Completion remains halted before renewed umbrella proof: six canonical consumer solutions require prohibited nested-submodule initialization, PolymorphicSerializations fails the same 15 pre-existing IDE0065 diagnostics on two identical serialized builds, and Timesheets currently uses a modified `Hexalith.Works` checkout rather than its tracked gitlink.
- Re-established and reconciled the live migration baseline. Jerome approved the three OpenTelemetry instrumentation packages at `1.17.0` and the corrected 285-project inventory; all pre-existing root and submodule work remains preserved.
- Completed the authoritative Builds catalog first: 15 additions produce 283 evaluated rows, override protection is `false`, all 48 approved/reconciled package values pass the evaluated contract, and `Hexalith.EventStore.Contracts` evaluates to `$(HexalithEventStoreVersion)` = `3.75.0` following Jerome's superseding 2026-07-19 disposition.
- Added source-plus-evaluation consumer enforcement and a closed 15-entry SDK/tool allowlist. Catalog, consumer, and exception fixtures pass 14/14, 16/16, and 7/7 scenarios; all twelve consumers pass combined source/evaluation validation across 285 projects (22 umbrella plus 263 submodule projects).
- Updated Builds guidance, sample, and release ordering. Independent Builds restore, Release build, 64 tests, 29 Dapr-validator scenarios, and 20 reusable-workflow assertions all pass; cache inputs already covered both wrapper and catalog and required no change.
- Migrated all consumer wrappers to exclusive shared authority while retaining each existing transitive-pinning posture. Timesheets now declares `references/Hexalith.Builds` as its root dependency; no nested dependency was initialized. Conversations now resolves umbrella sibling roots and its AppHost SDK is aligned from `13.4.2` to `13.4.6`.
- Fixed canonical-version compatibility without escape hatches: ChatBot and Memories use NSubstitute 6 nullable-safe matcher predicates, Memories Web explicitly consumes centrally managed AngleSharp `1.5.2`, its Epic 17 pin test evaluates the imported catalog, and Timesheets Server uses the resolved EventStore root.
- Added one shared ChatBot MSBuild-evaluation helper for Architecture/UI package contracts. Clean focused Release builds pass; Scaffold Architecture passes 28/28 and UI passes 227/227. The full Architecture assembly has 61 passes and two unrelated failures against concurrently edited planning ADR text.
- Resolved-graph evidence confirms Memories Web uses AngleSharp `1.5.2`, bUnit `2.8.4-preview`, Fluxor `6.10.0`, and Fluent UI `5.0.0-rc.4-26180.1`; Memories Server uses NSubstitute `6.0.0` and all three approved OpenTelemetry instrumentation packages at `1.17.0`.
- Transitive-pinning pack evidence was captured for EventStore, Parties, Timesheets, Tenants, FrontComposer, Folders, and Projects. Promoted dependencies contain the authoritative values (including EventStore Contracts `3.75.0`), and the evaluated restores contain no NU1109 downgrade.
- Independent green lanes: Builds, EventStore canonical Release, Memories canonical Release, Projects CI Release, Timesheets Server Release, Memories EventStore tests 129/129, Memories Web tests 492/492, and Memories Server tests 2734/2734 excluding the unrelated umbrella-path SubmoduleGuard fixture. ChatBot restore and serialized Release build pass with 0 warnings/errors.
- Completion remains `in-progress`: canonical solutions for Commons, Parties, Tenants, FrontComposer, Folders, and Conversations name intentionally uninitialized nested projects; Timesheets now has its tracked `Hexalith.Works` checkout and its serialized canonical Release build passes with zero warnings/errors after the repository-documented immediate rerun; PolymorphicSerializations' own cold standalone solution still fails 15 IDE0065 source-style errors. The Memories container integration lane did not complete without its external service prerequisites. These remaining constraints cannot be cleared without prohibited nested initialization, external runtime prerequisites, or out-of-scope source cleanup.
- `git diff --check` and staged diff checks pass for every owning repository. User-owned planning/architecture edits, the CI alignment proposal, and concurrent submodule pointer changes were preserved and are not attributed to Story 1.1e.
- 2026-07-18 code-review corrections to the notes above: (1) the validation-coverage claim "281 projects (22 umbrella plus 259 submodule projects)" was arithmetically inconsistent — the tracked inventory is 285 `.csproj` files (22 umbrella + 263 submodule), matching the approved inventory correction; (2) the `3.71.0` EventStore Contracts figures were accurate when written, were first superseded by the recorded post-implementation drift disposition (`3.74.0`, Tenants `3.2.18`), and are now superseded again for the EventStore family by Jerome's 2026-07-19 `3.75.0` disposition; (3) two Memories test-infrastructure changes shipped with the compatibility sweep without recorded rationale: the `MarkdownContractDocument.NormalizeLineEndings` rewrite plus `ContractDocumentGuardTests` (deterministic section extraction when a naive LF-to-CRLF materialization corrupts already-CRLF contract docs) and the `AccessTelemetryLifecycleMetricsTestCollection` with `DisableParallelization` (metrics assertions require serialized execution); both stabilized the Memories lanes used as story evidence; (4) the four UI pin tests formerly named `PackagePinsShouldRemainUnchanged*` were already failing at baseline (they asserted Fluxor `6.9.0` while pre-story commit `3c90c33` had moved the root catalog to `6.10.0`), so the migration re-baselined dead guards rather than preserving live ones; they now assert the approved shared-catalog values and were renamed accordingly during review.

### File List

- `_bmad-output/implementation-artifacts/1-1e-centralize-nuget-package-reference-version-authority.md`
- `_bmad-output/implementation-artifacts/epic-1-context.md`
- `_bmad-output/implementation-artifacts/spec-1-1e-initialize-hexalith-works-submodule.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/.decision-log.md`
- `references/Hexalith.Builds/Props/Directory.Packages.props`
- `references/Hexalith.Builds/Tools/test-authoritative-package-catalog.ps1`
- `references/Hexalith.Builds/.github/workflows/build-release.yml`
- `references/Hexalith.Builds/README.md`
- `references/Hexalith.Builds/Samples/Module.Directory.Packages.props`
- `references/Hexalith.Builds/Tools/README.md`
- `references/Hexalith.Builds/Tools/package-version-exceptions.json`
- `references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1`
- `references/Hexalith.Builds/Tools/test-consumer-package-authority-validator.ps1`
- `references/Hexalith.Builds/Tools/test-package-version-exception-validator.ps1`
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1`
- `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1`
- `references/Hexalith.Builds/Tools/validate-package-version-exceptions.ps1`
- `Directory.Packages.props`
- `tests/PackageCatalogTestHelper.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Cli.Tests/ChatBotCliCommandTests.cs`
- `tests/Hexalith.ChatBot.Mcp.Tests/ChatBotMcpServiceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLiveRegionReducedMotionContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj`
- `references/Hexalith.EventStore/Directory.Packages.props`
- `references/Hexalith.Parties/Directory.Packages.props`
- `references/Hexalith.Memories/Directory.Packages.props`
- `references/Hexalith.Memories/tests/Hexalith.Memories.EventStore.Tests/EventIngestionServiceTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.IntegrationTests/Migration/EmbeddingVectorMigrationRedisIntegrationTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Cases/DeleteMemoryUnitProjectionActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Indexing/EnumerateMemoryUnitIdsActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexNaturalLanguageSemanticActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticChunksActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Indexing/RepairUnitActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/ExtractContentActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/FetchUrlActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateChunkEmbeddingsActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityConfigTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/QueueNaturalLanguageEmbeddingRetryActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/UpdateCaseIngestionCounterActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreDataPlaneActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Restore/RestoreReindexUnitActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Tenants/DeleteTenantDataKeysActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Activities/Tenants/VerifyTenantActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Actors/CaseIngestionCounterActorTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Actors/EmbeddingRateLimiterActorTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Consistency/ConsistencyInspectionServiceTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Endpoints/ConsistencyEndpointTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Endpoints/IngestionEndpointE2ETests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Endpoints/ReIngestionEndpointE2ETests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointContractTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/RedisSearchIndexMaintenanceAdapterTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Handlers/HandlerRegistryServiceTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Infrastructure/TenantIndexReadinessVerifierTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Ingestion/DaprIngestionWorkflowSchedulerTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionPayloadClaimCheckTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Ingestion/IngestionWorkflowInFlightRegistryTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Ingestion/ReIngestionCoordinatorTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Ingestion/TenantEmbeddingConfigProviderTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Migration/RedisEmbeddingMigrationStoreTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Migration/RedisNaturalLanguageNamespaceMigratorTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/NaturalLanguage/FailedNaturalLanguageEmbeddingRegistryTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/NaturalLanguage/GenerateNaturalLanguageDescriptionActivityTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Telemetry/AccessTelemetryLifecycle/AccessTelemetryDeliveryCheckpointTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Telemetry/TelemetrySummaryEndpointTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Telemetry/TracePropagationNoDockerTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Tenants/TenantIsolationVerifierTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Tenants/TenantMetricsServiceTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/AnnotationProjectionWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/CaseCreationProjectionWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/CaseDeletionProjectionWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyRepairWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/ConsistencyVerificationWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowDualEmbeddingTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/MemoryUnitDeletionProjectionWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/TenantDeletionWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Workflows/TenantProvisioningWorkflowTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Web.Tests/Components/Validation/Epic17ConformanceHardeningTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Web.Tests/Hexalith.Memories.Web.Tests.csproj`
- `references/Hexalith.Memories/tests/Hexalith.Memories.AccessTelemetry.Tests/Capability/CapabilityAndObservabilityCheckpointTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.AccessTelemetry.Tests/Lifecycle/LifecycleActorCheckpointTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.AccessTelemetry.Tests/Observability/AccessTelemetryLifecycleMetricsTestCollection.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.Server.Tests/Documentation/ContractDocumentGuardTests.cs`
- `references/Hexalith.Memories/tests/Hexalith.Memories.TestHelpers/Documentation/MarkdownContractDocument.cs`
- `references/Hexalith.Commons/Directory.Packages.props`
- `references/Hexalith.Timesheets/.gitmodules`
- `references/Hexalith.Timesheets/Directory.Packages.props`
- `references/Hexalith.Timesheets/references/Hexalith.Builds`
- `references/Hexalith.Timesheets/src/Hexalith.Timesheets.Server/Hexalith.Timesheets.Server.csproj`
- `references/Hexalith.PolymorphicSerializations/Directory.Packages.props`
- `references/Hexalith.Conversations/Directory.Build.props`
- `references/Hexalith.Conversations/src/Hexalith.Conversations.AppHost/Hexalith.Conversations.AppHost.csproj`
- `references/Hexalith.Conversations/src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Admin.Web.Tests/Accessibility/AccessibilityEvidenceHarnessTest.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Admin.Web.Tests/Responsive/ResponsiveEvidenceHarnessTest.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/AdopterConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/BuyerAcceptanceConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/ConformanceStatusConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/ContractValidationConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/ConversationProjectionReadSurfaceConformanceTest.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/EventSchemaEvolutionConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/IdempotencyConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/PlatformEvidenceSeparationConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/ProviderPortabilityConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/RedactionConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/ReleaseScopeConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/SecondAdopterConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/TelemetryCardinalityConformanceSuiteTest.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/TelemetryRedactionConformanceSuiteTest.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Conformance.Tests/TenantIsolationConformanceSuite.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Server.Tests/Projections/ConversationProjectionReadModelPersistenceTest.cs`
- `references/Hexalith.Conversations/tests/Hexalith.Conversations.Tests/Aggregates/ConversationAggregateBaseClassDispatchTest.cs`
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs`
- `references/Hexalith.Parties/tests/Hexalith.Parties.Client.Tests/Package/ClientPackageTests.cs`
- `references/Hexalith.PolymorphicSerializations/examples/DeserializeFileMessages/Messages/Move.cs`
- `references/Hexalith.PolymorphicSerializations/examples/DeserializeFileMessages/Messages/Say.cs`
- `references/Hexalith.PolymorphicSerializations/examples/DeserializeFileMessages/Messages/SayByeVersion2.cs`
- `references/Hexalith.PolymorphicSerializations/examples/DeserializeFileMessages/Messages/SayHello.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations.CodeGenerators/SerializationMapperSourceGenerator.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations/AssemblyInfo.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations/IPolymorphicSerializationMapper.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations/IsExternalInit.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations/Polymorphic.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations/PolymorphicHelper.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations/PolymorphicSerializationMapper.cs`
- `references/Hexalith.PolymorphicSerializations/src/libraries/Hexalith.PolymorphicSerializations/PolymorphicSerializationResolver.cs`
- `references/Hexalith.PolymorphicSerializations/test/Hexalith.PolymorphicSerializations.Tests/PolymorphicSerializationAttributeTests.cs`
- `references/Hexalith.PolymorphicSerializations/test/Hexalith.PolymorphicSerializations.Tests/PolymorphicSerializationTests.cs`

### Change Log

- 2026-07-19: Reversed all nested-submodule initialization after Jerome clarified the repository invariant. Every nested gitlink is uninitialized again; ChatBot root-declared submodules remain initialized. Invalidated the canonical-build evidence that depended on nested checkouts and kept Story 1.1e `in-progress` pending sibling `../references/` resolution.
- 2026-07-19: Used Jerome's explicit authorization to initialize only the exact declared dependency checkouts, align stale local dependency gitlinks, fix Polymorphic analyzer compliance, and clear all prior independent/umbrella package-evidence blockers. Canonical builds, governance validators, focused consumer tests, and umbrella acceptance lanes are green. The mandatory full regression sweep still has three unrelated planning-integrity failures, so the dev-story completion gate keeps Story 1.1e `in-progress` and no task/sprint checkbox is advanced.
- 2026-07-19: Re-ran live completion evidence at root `9a6c077`; package-authority governance remains fully green, but canonical independent-consumer completion remains blocked by prohibited nested dependencies, two repeatable pre-existing Polymorphic IDE0065 failures, and a pre-existing modified Timesheets Works checkout. Story remains `in-progress`; no task checkbox or sprint status was advanced.
- 2026-07-19: Initialized Timesheets' already-declared `Hexalith.Works` checkout at its tracked commit for the current workspace, clearing the missing-Works blocker; focused Timesheets Works tests pass 76/76, and the serialized canonical Release build passes with zero warnings/errors after the repository-documented immediate rerun of the transient Polymorphic analyzer failure.
- 2026-07-19: Recorded Jerome's superseding acceptance of the EventStore `3.75.0` package family, rebaselined the authoritative catalog contract, and captured renewed independent/umbrella evidence. Story remains `in-progress` because required canonical consumer lanes still depend on intentionally unavailable nested/root dependencies or fail pre-existing unrelated gates.

- 2026-07-18: Implemented catalog-first exclusive package-version authority, migrated all twelve consumers, aligned approved OpenTelemetry/EventStore/Aspire families, added enforcement and evaluated pin tests, and recorded the remaining independent-solution environment blockers.
- 2026-07-18: Adversarial code review (Blind Hunter / Edge Case Hunter / Acceptance Auditor). Recorded the superseding post-implementation drift disposition (EventStore `3.74.0`, Tenants `3.2.18`), corrected evidence arithmetic and File List omissions, hardened the catalog-evaluation helpers (deadlock-prone process I/O, exception-caching Lazy, platform-aware path comparison) in ChatBot and Memories, strengthened the Builds consumer/exception validators (wrapper import-target verification, Sdk-element/global.json pin detection, git-fallback fixes) with fixture coverage, extended the ChatBot exclusive-authority guard (project-local `PackageVersion`, override re-enable, root build-props scan), restored lost catalog rationale comments, and renamed the dead-at-baseline UI pin guards. Deferred to Story 1.1f: workspace-mode validator CI wiring. Deferred as pre-existing: partial-checkout path-resolution rough edges.
