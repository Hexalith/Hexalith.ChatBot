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

## Code Map

- `Hexalith.ChatBot.slnx` -- canonical nine-project module and independent test-lane inventory; remove explicit sibling library projects and assert Workers/UI test coverage.
- `src/Hexalith.ChatBot.Workers/Hexalith.ChatBot.Workers.csproj` -- remove the surface's direct Contracts edge; Client remains its facade.
- `src/**/*.cs` -- split non-generated multi-type files without semantic changes.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` -- enforce project graph, one-type files, root submodules, release inventory, compiler/package settings, and local-only host boundaries.
- `src/Hexalith.ChatBot.AppHost/**` and its AppHost/integration tests -- extend live resource/health proof.
- `.github/workflows/{ci,release}.yml`, `.github/scripts/run-merge-test-lanes.sh`, `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- align SDK/package mode and require a positive executed-test count per lane.
- `tests/PackageCatalogTestHelper.cs`, `README.md` -- align xUnit 4.0.0 and remove obsolete source-mode workaround guidance.
- `release-metadata.json` -- package/container inventory, open gates, and prohibited claims.

## Tasks & Acceptance

**Execution:**
- [x] `src/**/*.cs` and scaffold architecture tests -- split every non-generated top-level declaration into its named file and add a non-vacuous zero-tolerance guard.
- [ ] `Hexalith.ChatBot.slnx`, `src/*/*.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.gitmodules` -- assert all required projects, typed-client-only surfaces, root-only submodules, C# 14/.NET 10, NuGet audit, exclusive central versions, and package-mode Release dependencies.
- [x] `.github/workflows/{ci,release}.yml`, `.github/scripts/run-merge-test-lanes.sh`, `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- install the pinned SDK, invoke each real xUnit project independently through its v4 runner, emit machine evidence, and fail when discovery/execution is zero.
- [ ] `src/Hexalith.ChatBot.AppHost/**`, `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`, `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` -- prove canonical stores, pub/sub, sidecars, service health, and endpoint availability while preserving the non-publishable host boundary.
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

## Spec Change Log

## Review Triage Log

## Design Notes

Baseline: `3c787993213ccf33f8912e6ad5caac605586fa15`. Prefer mechanical file moves. AppHost executable references are local composition resources, not library dependency authority.

## Verification

**Commands:**
- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` -- package-mode restore and audit succeed.
- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` -- all projects compile warning-free.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Release/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor -class Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests` -- scaffold guards pass.
- `.github/scripts/run-merge-test-lanes.sh` -- every ordinary lane executes non-vacuously; run the evidence-gate lane separately as CI does.
- `aspire run` followed by `aspire describe` and the focused Tier-3 topology test -- required resources, health, and endpoints are live; then stop the topology.
