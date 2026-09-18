---
title: 'Story 1.1: Scaffold the Runnable Canonical Module Foundation'
type: 'refactor'
created: '2026-09-17'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1047ef38d3845406227891639aaeb853e5d4f116'
context:
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/epics.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The inherited scaffold exists, but revised Story 1.1 is unproven: boundary guards are incomplete, 146 source files have multiple top-level types, Release automation has drifted, test lanes can pass vacuously, and release inventory/blockers are implicit.

**Approach:** Preserve behavior while enforcing source/dependency rules, restoring package-mode Release builds and non-vacuous tests, validating live local topology, and publishing blocked machine-checked release inventory.

## Boundaries & Constraints

**Always:** Preserve APIs, namespaces, XML docs, serialization, and AppHost behavior while splitting types. Require one top-level type per non-generated `src/**/*.cs`; exclude `*.g.cs`. Keep siblings root-declared, use only the shared Builds catalog, and retain a non-publishable AppHost. Metadata names Contracts/Client/Testing packages, Server/UI containers, and open A5/A6/A13 plus exact association/task-intent/action-risk A9a evidence.

**Never:** Add an allowlist for multi-type source files, edit `references/**`, initialize nested submodules, add standalone Aspire/ServiceDefaults projects, change public behavior to ease refactoring, or imply M0/M1, pilot, compliance, production, or gate readiness.

</frozen-after-approval>

## Human-Approved Scope Amendment (2026-09-18)

The user approved a narrow exception to the frozen `references/**` prohibition: update
`references/Hexalith.Builds` and only the affected root-declared sibling package metadata required to establish
shared central package-version authority and make ChatBot's Release dependency path genuinely package-based. This
does not authorize runtime/domain behavior changes, nested-submodule initialization, unrelated sibling cleanup, or
overwriting the pre-existing `references/Hexalith.Folders` working-tree correction.

## Code Map

- `Hexalith.ChatBot.slnx` -- canonical nine-project module and independent test-lane inventory; remove explicit sibling library projects and assert Workers/UI test coverage.
- `src/Hexalith.ChatBot.Workers/Hexalith.ChatBot.Workers.csproj` -- remove the surface's direct Contracts edge; Client remains its facade.
- `src/**/*.cs` -- split non-generated multi-type files without semantic changes.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` -- enforce project graph, one-type files, root submodules, release inventory, compiler/package settings, and local-only host boundaries.
- `src/Hexalith.ChatBot.AppHost/**` and its AppHost/integration tests -- extend live resource/health proof.
- `.github/workflows/{ci,release}.yml`, `.github/scripts/run-merge-test-lanes.sh`, `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- align SDK/package mode and require a positive executed-test count per lane.
- `tests/PackageCatalogTestHelper.cs`, `README.md` -- align xUnit 4.0.0 and remove obsolete source-mode workaround guidance.
- `release-metadata.json` -- package/container inventory, open gates, and prohibited claims.
- `references/Hexalith.Builds/Props/Directory.Packages.props` and the smallest necessary root-declared sibling package metadata -- add the missing shared package authority needed by ChatBot's Release graph.

## Tasks & Acceptance

**Execution:**
- [x] `src/**/*.cs` and scaffold architecture tests -- split every non-generated top-level declaration into its named file and add a non-vacuous zero-tolerance guard.
- [ ] `Hexalith.ChatBot.slnx`, `src/*/*.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.gitmodules`, `references/Hexalith.Builds/Props/Directory.Packages.props`, and only necessary root-declared sibling package metadata -- assert all required projects, typed-client-only surfaces, root-only submodules, C# 14/.NET 10, NuGet audit, exclusive shared central versions, and package-mode Release dependencies.
- [x] `.github/workflows/{ci,release}.yml`, `.github/scripts/run-merge-test-lanes.sh`, `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- install the pinned SDK, invoke each real xUnit project independently through its v4 runner, emit machine evidence, and fail when discovery/execution is zero.
- [x] `src/Hexalith.ChatBot.AppHost/**`, `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`, `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` -- prove canonical stores, pub/sub, sidecars, service health, and endpoint availability while preserving the non-publishable host boundary.
- [x] `release-metadata.json` plus architecture tests -- bind exact packages/containers and open A5/A6/A9a/A13 blockers without readiness claims.
- [x] `README.md` -- update only operational setup/release facts invalidated by the implementation.

**Acceptance Criteria:**
- Given a root-only checkout, when the project graph is inspected, then required projects/tests exist, surfaces use the typed Client, and forbidden edges fail tests.
- Given repository source, when architecture tests scan non-generated production C#, then every file has exactly one top-level type and the scan proves it examined real files.
- Given a Release restore/build under SDK 10.0.400, when packages and audit run, then shared versions/package mode are used and warnings remain errors.
- Given every xUnit project, when its lane runs independently, then at least one test executes and any zero-test, skipped-required, failed, or colliding lane fails closed.
- Given the local AppHost prerequisites, when Aspire starts, then canonical Dapr resources and required service health/endpoints are observed live; missing prerequisites are reported, never replaced by static evidence.
- Given release metadata, when conformance runs, then exact inventory/open blockers exist and no readiness claim is authorized.

## Implementation Notes

- 2026-09-17: Package-mode Release restore passed under SDK 10.0.400. All nine ChatBot source projects and all fourteen real test projects compiled independently with zero warnings; the focused scaffold lane passed 32/32 through the xUnit 4.0.0 direct runner, and the separate story-evidence gate passed 219/219.
- 2026-09-17: The exact solution build remains externally blocked in the clean root-declared `references/Hexalith.Folders` submodule: `HexalithFoldersIdempotencyHelpers.g.cs` lines 375, 389, and 402 report CS0037 for null checks/conditional access against non-nullable `PathMetadataPathPolicyClass`. No `references/**` source was changed.
- 2026-09-17: Live `aspire run`/`aspire describe`/Tier-3 execution was not attempted after the exact build blocker; static AppHost topology guards and the focused scaffold guards pass.
- 2026-09-17: The ordinary-lane script proves positive discovery and stops closed at the Architecture lane. Its current fallback-built binary reports 104/108 passing: two pre-existing guard failures (`DaprWorkflowTypesStayInsideServerWorkflowRuntimeLayer`, Architecture D8 mapping) plus two reflection-load failures because the blocked solution build could not populate the transitive `Hexalith.Memories.Contracts` runtime dependency. The runner correctly returns non-zero and retains CTRF evidence.
- 2026-09-17: Independent diff audit passed after removing three split-file blank lines detected by `git diff --check`; the scaffold and release-workflow safety classes then passed 55/55 with zero skips through the direct xUnit 4.0.0 boundary.
- 2026-09-17: Package-only Release dependency conversion is incomplete. `UseHexalithProjectReferences=false` still traverses unconditional sibling project references, while the shared Builds catalog has no `Hexalith.Folders.*` or `Hexalith.Projects.*` package entries. The frozen constraints prohibit editing `references/**` or introducing a local version authority, so the dependency graph and live topology require an external catalog/sibling correction before this story can complete.
- 2026-09-17: Superseding the earlier build blocker, the exact solution Release build now succeeds with zero warnings and errors after the external Folders working-tree correction. Its output still builds sibling dependencies from `bin/Debug`, so this does not close the package-only Release dependency task; the pre-existing Folders changes remain untouched.
- 2026-09-17: The focused Tier-3 run started the canonical Aspire topology and observed all required services running and healthy, unique Dapr sidecar endpoints, canonical state/pub-sub components, and the governed command/state/audit/replay path. The same broad test later timed out in its separate M2 release gate because persisted local state triggered `delivery-row-schema-regression` and `post-cutover-writer-protocol`; all Aspire resources stopped cleanly afterward.
- 2026-09-17: Final focused guards pass (AppHost topology 16/16 and scaffold architecture 32/32). The ordinary merge-lane runner now reaches 106/108 in the Architecture lane and fails closed only on the unrelated workflow-type-location and Architecture D8 mapping guards; `git diff --check` passes.
- 2026-09-17: Superseding that Architecture-lane result, all three Memories-backed Dapr ingestion activities now live in the Server workflow-runtime layer and the D8 SDK contract pairings use the canonical architecture notation. Both formerly failing methods pass individually, the affected projects build with zero warnings and errors, the ingestion-activity regression class passes 6/6, and the complete Architecture lane passes 108/108 with no skips.
- 2026-09-18: Every non-AppHost cross-repository library edge now has mutually exclusive source/package forms, while the AppHost executable references remain the sole typed local-composition exception. The shared Builds catalog now owns the complete Folders and Projects package families plus `LibGit2Sharp`; the Folders wrapper's former local version declaration was removed without touching its pre-existing source correction. Evaluated Release metadata for Server, UI, and Server.Tests contains only ChatBot `ProjectReference` items. A Debug source-mode focused build succeeds with zero warnings/errors and the expanded scaffold guard passes 34/34 with no skips.
- 2026-09-18: The exact clean package-mode Release restore now fails closed only because NuGet.org has no published `Hexalith.Folders.Client`, `Hexalith.Folders.Contracts`, or `Hexalith.Projects.Client` package IDs (`NU1101`). No local/bootstrap feed was introduced and no packages were published. The Release build cannot be run until those three centrally declared 1.0.0 packages are available from an authorized source.
- 2026-09-18: Ephemeral packaging validation produced `Hexalith.Folders.Contracts` and `Hexalith.Folders.Client` 1.0.0 packages successfully. `Hexalith.Projects.Contracts` and `Hexalith.Projects.Client` stable 1.0.0 packaging fails closed with `NU5104` because their centrally pinned `Microsoft.FluentUI.AspNetCore.Components` and `.Icons` v5 dependencies are prerelease (`5.0.0-rc.5-26219.1`), and the authoritative NuGet feed has no stable v5 release. Projects therefore needs an authorized prerelease-package strategy or must wait for stable Fluent v5 before the package-mode restore can become green.
- 2026-09-18: The shared catalog now also owns `Hexalith.Commons.Serialization` at the Commons 2.30.0 family version and `Hexalith.Conversations.Contracts` at the new authoritative Conversations 1.0.0 family version. Conversations.Contracts and the five external Projects.Contracts edges now evaluate to sibling source only in explicit source mode and to centrally versioned packages otherwise; the FrontComposer SourceTools edge remains private analyzer-only and does not enter the packed dependency graph. Debug remains source-first and Release remains package-first in both owning repositories.
- 2026-09-18: Ephemeral prerelease validation packed Conversations.Contracts 1.0.0 and Projects.Contracts/Client 1.0.0-preview.1. Nuspec inspection proved Commons.Serialization 2.30.0, EventStore.Contracts 3.100.1, Conversations.Contracts 1.0.0, and FrontComposer Contracts/Shell 4.2.0 dependencies, plus the aligned Projects.Contracts preview dependency from Projects.Client. With those packages and the already validated Folders 1.0.0 pair supplied through an explicit temporary source, the exact ChatBot Release package-mode restore and full solution build passed with zero warnings/errors; the focused scaffold lane passed 34/34.
- 2026-09-18: A subsequent `--force --no-http-cache` restore into an isolated packages directory, without the temporary source or version override, failed only with `NU1101` for `Hexalith.Folders.Client`, `Hexalith.Folders.Contracts`, and `Hexalith.Projects.Client`. Completion therefore remains externally blocked on publishing the Folders pair and selecting an authorized Projects version channel: the shared catalog requests stable 1.0.0, while the currently packable Projects artifacts must be prerelease because they depend on prerelease Fluent UI v5.

## Spec Change Log

## Review Triage Log

## Design Notes

Baseline: `3c787993213ccf33f8912e6ad5caac605586fa15`. Prefer mechanical file moves. AppHost executable references are local composition resources, not library dependency authority.

## Verification

**Commands:**
- `dotnet restore Hexalith.ChatBot.slnx --force --no-http-cache -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:RestorePackagesPath=<isolated-empty-directory> -m:1 /nr:false` -- fails closed with `NU1101` for only the three unpublished Folders/Projects package IDs recorded above; no warmed global cache or non-AppHost sibling library fallback masks the publication blocker.
- `dotnet pack` for Commons.Serialization 2.30.0, Conversations.Contracts 1.0.0, Folders Contracts/Client 1.0.0, and Projects Contracts/Client 1.0.0-preview.1 into one ephemeral directory -- succeeds without publishing; nuspec inspection confirms the shared-catalog dependency versions and confirms SourceTools remains private.
- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:HexalithProjectsVersion=1.0.0-preview.1 -p:RestoreAdditionalProjectSources=<ephemeral-package-directory> -m:1 /nr:false` -- package-mode restore succeeds against the explicit validation source.
- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -p:HexalithProjectsVersion=1.0.0-preview.1 -p:RestoreAdditionalProjectSources=<ephemeral-package-directory> -m:1 /nr:false` -- full solution build succeeds with zero warnings/errors.
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` -- focused source-mode compile succeeds with zero warnings/errors.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Release/net10.0/Hexalith.ChatBot.Architecture.Tests -noColor -class Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests` -- expanded scaffold guards pass 34/34 with no skips against the package-mode build.
- `.github/scripts/run-merge-test-lanes.sh` -- every ordinary lane executes non-vacuously; run the evidence-gate lane separately as CI does.
- `aspire run` followed by `aspire describe` and the focused Tier-3 topology test -- required resources, health, and endpoints are live; then stop the topology.
