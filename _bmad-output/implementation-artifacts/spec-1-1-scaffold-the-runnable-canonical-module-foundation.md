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
- `tests/PackageCatalogTestHelper.cs`, `README.md` -- align the catalog-pinned xUnit 4 runner and remove obsolete source-mode workaround guidance.
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
- Given a Release restore/build under the SDK selected by `global.json` (currently 10.0.401 with `latestPatch` within the 10.0.4xx feature band), when packages and audit run, then shared versions/package mode are used and warnings remain errors.
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
- 2026-10-04: Superseding the earlier publication blocker, the exact cold package-mode Release restore now succeeds under SDK 10.0.401 with NuGet audit enabled, an isolated empty packages directory, NuGet.org only, and no feed/version overrides. The final ordinary package-mode Release restore and full solution build also succeed with zero warnings/errors. AppHost executable references remain the local-composition exception.
- 2026-10-04: Completed the approved missing-identity metadata centralization: Builds now owns Commons.Diagnostics, Commons.Publication, and Commons.TenantAccess through the existing Commons 2.30.1 family property, plus Conversations.Client through the existing Conversations 1.0.0 family property. Removed the corresponding Conversations declarations and redundant Projects Conversations/Folders pins without changing consumed versions. Builds central-catalog validation passes; ChatBot and Conversations consumer authority checks pass. The broader Projects check still rejects a retained, unrelated `qualification-evidence/6-1-p1r-3102-20260905/remote-consumer-3102/RemoteConsumer.csproj` artifact with inline versions and CPM disabled; that artifact is unchanged.
- 2026-10-04: Exclusive sibling authority remains open. The final foundation guards execute 63 tests, with 62 passing and zero skips; their only failure is `RootDeclaredSiblingPackageWrappersShouldDelegateVersionsToSharedBuildsCatalog`, identifying the deliberate Folders.Aspire `CommunityToolkit.Aspire.Hosting.Dapr` 13.0.0 local update. Builds selects 13.5.1-beta.757, and its consumer validator compares each project against a single context-free catalog evaluation. Neither a conditional central override nor removing the local update preserves both that policy and the stable package dependency. The metadata amendment does not authorize changing this selection or widening the validator. The dependency task stays unchecked and the story stays `in-progress` pending a scope decision.
- 2026-10-04: All 29 runner safety cases pass against catalog-pinned xUnit 4.0.1, including mismatched versions, zero execution/discovery, failed results, invalid/inconsistent counts, required skips, colliding lanes, and stale evidence. Static AppHost topology guards pass 16/16. The corrected integration topology fixture supplies the existing synthetic Projects endpoint/token prerequisites to successful AppHost compositions while preserving deliberate negative inputs and production behavior; all 29 `RecoveryValidationTopologyContractTests` pass with zero skips.
- 2026-10-04: The ordinary merge script fails closed at Architecture (115/116, zero skips) on the same retained Folders authority conflict. Every other real assembly was invoked independently and executed a positive test count: Cli 24/24, Client 47/47, Mcp 30/30, Server 1917/1917, StoryEvidenceGate 219/219, Testing 41/41, UI 304/304, Workers 32/32. Recorded unrelated failures remain in Conformance (99/100; `WorkflowRuntimeMustNotDirectlyMutateSiblingBoundedContexts` flags the Memories token in `IngestionBindingGetStatusActivity.cs`), Contracts (482/484; the normative addendum lacks published SLO `chatbot.command.execution.latency`), Integration (323/342 with 13 failures and six optional live skips before the focused fixture correction; the remaining unrelated rebuild assertion expects four writes and observes six), and UI.E2E (147/149; missing canonical Story 13.2 ledger key and association validation accessibility marker). Normative documents, runtime behavior, qualification artifacts, and generated screenshot baselines were left unchanged. These lane reports remain evidence of failures, not completion proof.
- 2026-10-04: Fresh-store live Tier-3 execution observed all seven required services Running/Healthy, distinct reserved HTTP endpoints, canonical stores/pub-sub and all five sidecars, plus real state, actor command, pub-sub projection, command/state/audit, and idempotent replay sub-assertions. The required test overall failed (one executed, one failed, zero skips) with `TaskCanceledException` in `AssertM2ReleaseGateIsClearAsync` after its separate six-minute deadline; existing diagnostics show repeated HTTP 503 responses without a captured M2 response body/reason code. The test disposed its AppHost, `aspire ps` confirmed only the pre-existing external Tenants app remained, and all three task-owned ephemeral Redis/placement/scheduler containers were stopped. This proves the recorded local topology observations and does not establish M2 or release readiness.
- 2026-10-04: The repository story-evidence completion preflight remains unsatisfied: no matching Story 1.1 completion contract/provenance was found. The 219 passing gate self-tests and CTRF/checksum outputs do not replace the required contract-bound primary evidence. No provenance, status transition, staging, or commit was fabricated; the frozen intent, baseline commit, and concurrent user index entries are preserved.

## Spec Change Log

- 2026-10-04: Reconciled the SDK acceptance wording with the committed `global.json` patch update to 10.0.401. CI/release now install from `global.json`, and the guard preserves the .NET 10.0.4xx feature band plus `latestPatch` policy. The existing shared catalog's xUnit 4.0.1 and bunit 2.11.3 updates are consumed without adding a version authority or changing the frozen intent or baseline commit.

## Review Triage Log

## Design Notes

Baseline: `3c787993213ccf33f8912e6ad5caac605586fa15`. Prefer mechanical file moves. AppHost executable references are local composition resources, not library dependency authority.

## Verification

**Current verification (2026-10-04):**
- `dotnet restore Hexalith.ChatBot.slnx --force --no-http-cache -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:RestorePackagesPath=/tmp/chatbot-story-1-1-isolated-packages-20261004 -m:1 /nr:false` -- passes without publication/feed/version workarounds; log `/tmp/chatbot-story-1-1-release-restore.log`.
- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` and `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` -- final state passes, zero warnings/errors; logs `/tmp/chatbot-story-1-1-release-{restore,build}-final.log`.
- From `references/Hexalith.Builds`, `pwsh -NoProfile -File Tools/validate-central-package-versions.ps1` -- 303 catalog entries pass (`/tmp/chatbot-story-1-1-central-catalog.log`); `pwsh -NoProfile -File Tools/validate-consumer-package-authority.ps1 -RepositoryRoot ../Hexalith.Folders -CatalogPath Props/Directory.Packages.props` -- fails with the single consumer `PackageVersion Update` identified above (`/tmp/chatbot-story-1-1-folders-package-authority.log`).
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh <Release-Architecture-runner> TestResults/story-1-1/foundation-guards-final.ctrf.json -class Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests -class Hexalith.ChatBot.Architecture.Tests.ReleaseWorkflowSafetyTests` -- 62/63, the retained Folders authority conflict only; log `/tmp/chatbot-story-1-1-foundation-tests-final.log`.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh <Release-Integration-runner> TestResults/story-1-1/recovery-topology-contract.ctrf.json -class Hexalith.ChatBot.IntegrationTests.Recovery.RecoveryValidationTopologyContractTests` -- 29/29, zero skips; log `/tmp/chatbot-story-1-1-recovery-topology-contract.log`.
- `.github/scripts/run-merge-test-lanes.sh` -- fails closed at the remaining Architecture authority guard; `/tmp/chatbot-story-1-1-merge-lanes.log` and `TestResults/merge-test-lanes/*.ctrf.json`. Independent remaining lane results are retained in `TestResults/story-1-1/independent-lanes/*.ctrf.json` and `/tmp/chatbot-story-1-1-Hexalith.ChatBot.*.log`.
- Required Tier-3 direct-runner execution selected `TrivialGovernedCommandAspireE2eTests.TrivialGovernedCommandShouldFlowEndToEndThroughTheRealDaprTopology` with `HEXALITH_CHATBOT_TIER3=1`, `HEXALITH_CHATBOT_TIER3_REQUIRED=1`, `XUNIT_REQUIRE_ZERO_SKIPS=1`, and fresh task-owned Redis/placement/scheduler endpoints -- topology sub-assertions observed, overall M2 timeout retained in `TestResults/story-1-1/live-topology.ctrf.json` and `/tmp/chatbot-story-1-1-live-topology.log`. Resource snapshot `/tmp/chatbot-story-1-1-aspire-test-resources.json`; cleanup log `/tmp/chatbot-story-1-1-topology-cleanup.log`.

**Earlier commands (2026-09-18 retained evidence):**
- `dotnet restore Hexalith.ChatBot.slnx --force --no-http-cache -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:RestorePackagesPath=<isolated-empty-directory> -m:1 /nr:false` -- fails closed with `NU1101` for only the three unpublished Folders/Projects package IDs recorded above; no warmed global cache or non-AppHost sibling library fallback masks the publication blocker.
- `dotnet pack` for Commons.Serialization 2.30.0, Conversations.Contracts 1.0.0, Folders Contracts/Client 1.0.0, and Projects Contracts/Client 1.0.0-preview.1 into one ephemeral directory -- succeeds without publishing; nuspec inspection confirms the shared-catalog dependency versions and confirms SourceTools remains private.
- `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:HexalithProjectsVersion=1.0.0-preview.1 -p:RestoreAdditionalProjectSources=<ephemeral-package-directory> -m:1 /nr:false` -- package-mode restore succeeds against the explicit validation source.
- `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -p:HexalithProjectsVersion=1.0.0-preview.1 -p:RestoreAdditionalProjectSources=<ephemeral-package-directory> -m:1 /nr:false` -- full solution build succeeds with zero warnings/errors.
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -m:1 /nr:false` -- focused source-mode compile succeeds with zero warnings/errors.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Release/net10.0/Hexalith.ChatBot.Architecture.Tests -noColor -class Hexalith.ChatBot.Architecture.Tests.ScaffoldArchitectureTests` -- expanded scaffold guards pass 34/34 with no skips against the package-mode build.
- `.github/scripts/run-merge-test-lanes.sh` -- every ordinary lane executes non-vacuously; run the evidence-gate lane separately as CI does.
- `aspire run` followed by `aspire describe` and the focused Tier-3 topology test -- required resources, health, and endpoints are live; then stop the topology.
