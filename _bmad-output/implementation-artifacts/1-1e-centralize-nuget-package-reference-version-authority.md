---
baseline_commit: 9567f43d61478192cf30d0cef08aa63857ae8796
---

# Story 1.1e: Centralize NuGet Package-Reference Version Authority

Status: ready-for-dev

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

- [ ] Re-establish the migration baseline without disturbing current work (AC: 1-5)
  - [ ] Read each affected repository's `AGENTS.md`/tracked guidance and inspect its branch, working tree, remotes, recent history, build configuration, solution, tests, and root `.gitmodules` before editing that repository.
  - [ ] Re-run the package-authority inventory over the ChatBot root and all root-declared submodules without initializing nested submodules. Compare the result with the proposal snapshot: 281 .NET projects, 14 package-props files, 102 local `PackageVersion` rows plus the EventStore property override, 71 unique local IDs, 15 missing catalog IDs, 30 conflicts, 26 already-equal IDs, ten AppHost SDK pins, and five tool pins.
  - [ ] Reconcile the known post-proposal baseline at commit `9567f43`: the Builds catalog has 268 rows; the 15 missing IDs remain; source comparison finds 31 unique conflicting IDs because `OpenTelemetry.Instrumentation.AspNetCore` and `OpenTelemetry.Instrumentation.Http` now differ, while the shared `OpenTelemetry.Instrumentation.Runtime` target moved from the approved `1.16.0` to `1.17.0`. Record an explicit planning/architecture disposition before changing consumers; do not infer approval from recency alone.
  - [ ] Stop and document any other unexplained difference from the approved matrix before changing versions. Do not reinterpret a snapshot count as permission to delete a newly introduced package.
  - [ ] Preserve all pre-existing root and submodule work represented by the recent commits. In particular, do not overwrite the ChatBot `Directory.Packages.props` updates, the Story 1.1c CI/AppHost work, the `1-1c` status transition, or newly integrated submodule pointers.
  - [ ] Work in the repository that owns each change. Do not commit, push, release, force-update, or recursively initialize submodules as an implicit implementation step.

- [ ] Make `Hexalith.Builds` the complete, fail-closed catalog authority (AC: 1-3)
  - [ ] Update `references/Hexalith.Builds/Props/Directory.Packages.props` first with the 15 additions and exact canonical versions in the binding matrix below.
  - [ ] Confirm every existing conflict already resolves to the approved Builds value; change only values explicitly authorized by the matrix. Keep shared Hexalith family properties in the authoritative catalog, but remove consumer-side compatibility/version properties.
  - [ ] Set `CentralPackageVersionOverrideEnabled` to `false` at the shared authority so `VersionOverride` cannot bypass CPM, while retaining a source scan as defense in depth.
  - [ ] Preserve the existing Builds self-wrapper as an import-only `Directory.Packages.props`; do not duplicate the catalog in the wrapper.
  - [ ] Verify the catalog through evaluated MSBuild output and restore before removing any consumer definitions.

- [ ] Extend Builds-owned catalog and consumer-authority validation (AC: 1-4)
  - [ ] Extend `Tools/validate-central-package-versions.ps1` and its fixture suite to reject blank IDs, case-insensitive duplicates, blank/unresolved/tag-prefixed/malformed versions, failed/malformed evaluation, and mismatched effective catalog results.
  - [ ] Add or extend a Builds-owned consumer-authority validator and fixture suite under `references/Hexalith.Builds/Tools/`. It must source-scan consumer XML and evaluate representative projects; either signal alone is insufficient.
  - [ ] Reject consumer `PackageVersion Include` and `Update`, `PackageReference` attribute/nested versions, `VersionOverride`, version-bearing `GlobalPackageReference`, project-local `ManagePackageVersionsCentrally=false`, and properties/expressions that feed forbidden version metadata.
  - [ ] Prove that a valid version-free wrapper passes, metadata such as `PrivateAssets`/`IncludeAssets` remains legal, imported consumer `Update` cannot hide behind the catalog's `DefiningProjectFullPath`, SDK-implicit references are handled correctly, and every forbidden form fails with a precise diagnostic.
  - [ ] Validate that every .NET consumer root imports the shared catalog and every evaluated project has CPM enabled. Compare evaluated effective versions with the authoritative catalog, not only XML text.
  - [ ] Preserve each repository's existing `CentralPackageTransitivePinningEnabled` posture; do not enable it globally. Where already enabled, verify resolved graphs and pack output for promoted transitive dependencies and NU1109 downgrade failures.
  - [ ] Add a machine-readable SDK/tool exception inventory and an alignment validator. Treat that inventory as an allowlist, not a general version escape hatch.

- [ ] Wire the invariant into Builds documentation, samples, and CI (AC: 1-4)
  - [ ] Update `references/Hexalith.Builds/README.md`, `Tools/README.md`, and `Samples/Module.Directory.Packages.props` so no example invites repository-specific `PackageVersion` rows.
  - [ ] Update `references/Hexalith.Builds/.github/workflows/build-release.yml` so catalog integrity, consumer-authority fixtures, and exception/alignment validation run before DAPR-family validation and release creation. Update reusable `domain-ci.yml`/`domain-release.yml` only where their validation interface or cache inputs must change.
  - [ ] Preserve reusable workflow cache inputs that include both the consumer wrapper and `references/Hexalith.Builds/Props/Directory.Packages.props`.
  - [ ] Verify `Hexalith.Builds` independently before consumer migration; do not use a consumer-local override to make a failing Builds catalog appear green.

- [ ] Migrate all twelve .NET consumers in their owning repositories (AC: 1-3)
  - [ ] Replace the ChatBot root's 57-row local catalog with a version-free wrapper importing the shared catalog; preserve current non-version settings only when still needed.
  - [ ] Remove EventStore's three local package rows and the `HexalithCommonsVersion` compatibility override.
  - [ ] Remove Parties' four local package rows/updates, Memories' thirteen, Commons' two, Timesheets' twenty-two, and PolymorphicSerializations' one.
  - [ ] Retain and validate the already version-free wrappers in Tenants, FrontComposer, Folders, Conversations, and Projects; normalize only what is necessary for the exclusive-authority contract.
  - [ ] Add `references/Hexalith.Builds` as a root-declared dependency of Timesheets for standalone consumption, using a non-recursive root initialization path. Do not initialize it as a nested submodule from the ChatBot umbrella without explicit authority.
  - [ ] Fix compatibility failures in the affected consumer's source/tests/configuration. Never restore a local version escape hatch, and never weaken warnings-as-errors, package-only Release validation, analyzer gates, or test assertions to absorb a canonical version change.
  - [ ] Preserve Debug source-reference versus Release package-reference behavior and rerun restore after switching dependency modes.

- [ ] Update ChatBot package-governance and package-pin tests (AC: 1-3, 5)
  - [ ] Update `ScaffoldArchitectureTests` so the MCP assertion evaluates shared-catalog `ModelContextProtocol` `1.4.1`; strengthen the inline-version guard for nested `Version`, `VersionOverride`, opt-out, wrapper import, and exclusive ownership.
  - [ ] Update the accessibility, responsive/touch, localization, and live-region/reduced-motion contract tests to resolve/evaluate the imported catalog and assert the approved canonical pins: Fluent UI `5.0.0-rc.4-26180.1`, Fluxor `6.10.0`, Playwright `1.61.0`, xUnit v3 `3.2.2`, and bUnit `2.8.4-preview`.
  - [ ] Reuse one test helper or evaluated-catalog mechanism where practical; do not copy five independent XML parsers that can drift.
  - [ ] Keep `.csproj` `PackageReference` items version-free and preserve legal asset metadata. Do not modify generated clients or product/UI behavior for this build-governance story.
  - [ ] Run a clean/no-incremental focused rebuild when validating changed package-pin tests so stale test assemblies cannot provide false evidence.

- [ ] Inventory and validate CPM-incompatible version mechanisms (AC: 4)
  - [ ] Record the ten current AppHost SDK declarations and the five tool pins listed below with repository owner and alignment rule.
  - [ ] Compare every AppHost SDK version with the effective shared `Aspire.Hosting` family. Resolve the current Conversations `13.4.2` versus shared `13.4.6` mismatch through the approved family-alignment rule or a named architecture exception; do not leave it silently divergent.
  - [ ] Keep EventStore/FrontComposer/Parties tool-manifest versions explicit and exact. Verify their ownership and restore behavior without attempting to move them into CPM.
  - [ ] Reject unlisted SDK/tool exceptions and require an architecture decision before expanding the allowlist.

- [ ] Produce independent consumer and umbrella completion evidence (AC: 3-5)
  - [ ] For each repository, run its canonical `.slnx` restore and build using that repository's documented serialization/configuration; run its documented focused tests rather than assuming every Hexalith repository shares one test runner convention.
  - [ ] Where a canonical change can affect the resolved graph, capture `dotnet package list --project <solution-or-project> --include-transitive --no-restore --format json` or equivalent evaluated evidence and compare it with the matrix.
  - [ ] For repositories using transitive pinning or publishing NuGet packages, run their package-only consumer/pack validation and inspect promoted dependencies.
  - [ ] Run the Builds validator fixture suites; run the ChatBot Architecture and UI test projects individually; run any affected consumer compatibility tests; use repository-approved direct xUnit v3 executables when Microsoft.Testing.Platform/VSTest filtering or listener constraints require the documented fallback.
  - [ ] After independently verified owning-repository changes are integrated, run ChatBot `dotnet restore Hexalith.ChatBot.slnx`, canonical Release build, package-authority scans, affected focused test projects, and relevant integration lanes.
  - [ ] Record a final zero-consumer-local-definition scan, the evaluated effective-version matrix, the SDK/tool exception inventory, exact commands/results, and `git diff --check` for every owning repository.

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

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.

### File List
