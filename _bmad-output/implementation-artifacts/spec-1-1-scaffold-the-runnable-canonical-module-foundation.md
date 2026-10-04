---
title: 'Story 1.1: Scaffold the Runnable Canonical Module Foundation'
type: 'refactor'
created: '2026-09-17'
status: 'review'
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

## Human-Approved Scope Amendment (2026-10-04)

The user explicitly approved expanding this story to reconcile Builds' package-version validator with Folders'
stable Dapr dependency and resolve the remaining baseline and M2 failures. This supersedes the earlier metadata-only
restriction for the smallest necessary Builds catalog/validator/regression changes and ChatBot source, test,
normative-document, and evidence corrections required by the recorded failures. Keep Folders.Aspire on
`CommunityToolkit.Aspire.Hosting.Dapr` 13.0.0 and preserve other consumers' existing selections. Central selection
must remain exclusively owned by Builds; consumer overrides, duplicate identities, unresolved versions, and
unauthorized version drift must still fail. Diagnose failures against their intended invariants before fixing code
or stale assertions; do not weaken safety gates, skip required tests, fabricate provenance, or claim readiness.
Preserve unrelated user work and existing qualification artifacts. Nested submodule initialization, publication,
staging, commits, pushes, and unrelated dependency updates remain outside this approval.

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
- `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1`, `Tools/validate-central-package-versions.ps1`, `Tools/test-authoritative-package-catalog.ps1`, the shared catalog, and `references/Hexalith.Folders/Directory.Packages.props` -- move the stable Folders selection into central authority and validate the centrally selected version in each actual consumer context, retaining fail-closed override checks.
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/IngestionBinding*.cs`, existing Memories adapters, and `tests/Hexalith.ChatBot.Conformance.Tests/CorrectionPropagationWorkflowConformanceTests.cs` -- reconcile the ingestion workflow boundary with the dependency and sibling-mutation invariants without suppressing forbidden owner writes.
- `src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineCatalog.cs`, `tests/Hexalith.ChatBot.Contracts.Tests/OperatingBaselineAddendumDriftTests.cs`, and `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` -- restore the published-SLO documentation mirror from its authoritative catalog.
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver*.cs` and their read-model store fixture -- establish and assert the intended tenant-isolated rebuild end-state rather than merely adjusting a write-count expectation.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/{Epic10ReleaseReadinessE2ETests,Story12CssRetirementE2ETests}.cs`, the governed association-action component, and sprint/story evidence -- reconcile current story identity and rendered accessibility invariants without inventing completion history.
- `src/Hexalith.ChatBot.Server/Operations/PeriodicEnforcement/*M2*.cs`, the existing sweep implementations, and `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` -- capture safe gate diagnostics, find the live 503 cause, correct the smallest runtime or harness defect, and prove the full required live test passes with cleanup.
- `docs/story-evidence-integrity.md`, the existing StoryEvidenceGate tool/policy, and `_bmad-output/implementation-artifacts/evidence/1-1-scaffold-the-runnable-canonical-module-foundation.json` -- prepare exact, current evidence within the unchanged completion policy; report any genuine external lifecycle prerequisite instead of manufacturing a passing contract.

## Tasks & Acceptance

**Execution:**
- [x] `src/**/*.cs` and scaffold architecture tests -- split every non-generated top-level declaration into its named file and add a non-vacuous zero-tolerance guard.
- [x] `Hexalith.ChatBot.slnx`, `src/*/*.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.gitmodules`, `references/Hexalith.Builds/Props/Directory.Packages.props`, and only necessary root-declared sibling package metadata -- assert all required projects, typed-client-only surfaces, root-only submodules, C# 14/.NET 10, NuGet audit, exclusive shared central versions, and package-mode Release dependencies.
- [x] `.github/workflows/{ci,release}.yml`, `.github/scripts/run-merge-test-lanes.sh`, `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs` -- install the pinned SDK, invoke each real xUnit project independently through its v4 runner, emit machine evidence, and fail when discovery/execution is zero.
- [x] `src/Hexalith.ChatBot.AppHost/**`, `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`, `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs` -- prove canonical stores, pub/sub, sidecars, service health, and endpoint availability while preserving the non-publishable host boundary.
- [x] `release-metadata.json` plus architecture tests -- bind exact packages/containers and open A5/A6/A9a/A13 blockers without readiness claims.
- [x] `README.md` -- update only operational setup/release facts invalidated by the implementation.
- [x] Builds catalog/validator/regression files and Folders package wrapper listed above -- centralize the stable Folders selection, prove per-consumer authority, and prove unauthorized consumer overrides still fail.
- [x] The workflow, SLO mirror, projection-rebuild, and UI guard paths listed above -- resolve each recorded baseline failure with focused invariant verification, then execute every independent ordinary lane successfully.
- [x] The M2 runtime/sweep and required live-test paths listed above -- diagnose safe failure reasons, fix the cause, and pass the complete required topology/command/state/audit/replay/M2 test with zero skips and task-owned cleanup.
- [ ] Story 1.1 evidence contract and existing gate tool/policy -- produce the required current machine evidence and run the actual completion preflight without weakening status, scope, primary-path, or provenance rules.

**Acceptance Criteria:**
- Given a root-only checkout, when the project graph is inspected, then required projects/tests exist, surfaces use the typed Client, and forbidden edges fail tests.
- Given repository source, when architecture tests scan non-generated production C#, then every file has exactly one top-level type and the scan proves it examined real files.
- Given a Release restore/build under the SDK selected by `global.json` (currently 10.0.401 with `latestPatch` within the 10.0.4xx feature band), when packages and audit run, then shared versions/package mode are used and warnings remain errors.
- Given every xUnit project, when its lane runs independently, then at least one test executes and any zero-test, skipped-required, failed, or colliding lane fails closed.
- Given the local AppHost prerequisites, when Aspire starts, then canonical Dapr resources and required service health/endpoints are observed live; missing prerequisites are reported, never replaced by static evidence.
- Given release metadata, when conformance runs, then exact inventory/open blockers exist and no readiness claim is authorized.
- Given Folders.Aspire and the other real consumers, when central authority is evaluated in each consumer context, then Folders keeps stable Dapr integration 13.0.0, other existing versions are preserved, and local overrides or duplicated/missing identities fail.
- Given the recorded baseline failures, when focused invariants and each independent lane run, then the corrections preserve authorization, tenant isolation, documentation accuracy, and rendered accessibility and no ordinary lane fails or passes vacuously.
- Given fresh task-owned topology resources and required Tier-3 opt-in, when the complete required live test runs, then all sub-assertions and the M2 gate pass with zero skips and all task-owned resources are cleaned up.

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

- 2026-10-04 approved follow-up: Builds now selects the existing stable Dapr integration 13.0.0 only for `Hexalith.Folders.Aspire`; every other consumer retains the existing centrally selected preview. The consumer validator evaluates the trusted Builds catalog separately for each actual MSBuild project name, without importing consumer overrides. Consumer regression scenarios pass 32/32, central-validator scenarios pass 17/17, and the authoritative-catalog regression passes 50 required identities plus three shared family versions. Real authority checks pass for Folders (34 projects), ChatBot (25), Conversations (15), and Projects (24); the Projects check explicitly excludes only its unchanged tracked standalone remote-consumer qualification probe, whose inline-version policy is unrelated to this story.
- 2026-10-04 approved follow-up: Memories ingestion REST transport now lives behind a Server derived-store adapter; workflow activities retain scheduling, source identity, hashing, and idempotency behavior. The documentation drift test checks the explicit runtime compatibility mirror; its existing starter values do not replace the dimensioned normative M2 qualification targets. Projection rebuild verification now checks the complete source/attachment/index/governed key set and preserves a neighboring tenant's value and ETag. UI checks render the real association action component and validate conditional error descriptions. Canonical Story 13.2 remains `backlog`, and the source-coverage linkage test explicitly requires blocked release metadata with no authorized claims; it does not authorize release readiness from source presence.
- 2026-10-04 approved follow-up live diagnosis: The safely captured M2 503 reason was `derived-store-isolation-probe:breaches_detected`. Actual owner diagnostics established incomplete probe calls: anonymous Memories access returned 401; the existing sidecar component watcher exhausted local inotify capacity; owner provisioning then lacked an authenticated EventStore gateway, and the existing topology lacked a `memories-tenants` domain processor. A bounded real Keycloak service identity also exposed EventStore's first-claim normalization, so the fixture uses its supported space-delimited tenant/permission claim form and asserts exact two-tenant/two-command/one-domain scope without `global_admin`. No production sweep gate was weakened, no owner runtime source was edited, and no cross-tenant disclosure was inferred from incomplete probes.
- 2026-10-04 approved follow-up composition limit: The test-only loopback gateway forwards real EventStore admission and responses. Its SDK domain transport calls the existing Memories tenant aggregate Handle methods, SDK state replay, and owner Apply methods; owner REST provisioning and actual EventStore actor persistence remain in the path. The owner library must match the live Aspire project and explicit Release `dotnet run --no-build` arguments, loaded location, deployed SHA-256, and runtime deps entry; assembly and library versions are recorded separately. Default local/deployed topology still requires an authenticated owner gateway and a hosted tenant domain processor. This fixture cannot establish default AppHost, production, or release readiness.
- 2026-10-04 final verification corrections: The complete required live class exposed two fixture defects: its intentionally competing failure continuations could be canceled before faulting, and its smoke workflow ID contained colons rejected by the actual Dapr response (`ERR_INSTANCE_ID_INVALID`). The shared barrier now uses the external test cancellation token, and the smoke ID uses the documented alphanumeric/dash grammar; coordinator failure retention and production correction identities are unchanged. The final ordinary run also exposed stale shared UI assertions for Fluent UI prerelease and Playwright 1.62.0. They now assert the existing central selections (Fluent components/icons 5.0.0 and Playwright 1.63.0), without updating dependencies or loosening catalog authority.
- 2026-10-04 native evidence compatibility: The ordinary-lane script now emits fresh native xUnit 4 TRX alongside CTRF and checksums, removing only each lane's stale TRX/checksum before invocation. Existing runner scenarios prove distinct per-project TRX paths and fail closed on missing fresh TRX, including stale-only output; positive execution, failure, skip, and collision checks remain. Operational gate documentation now describes the actual native runner and shared 13-lane script, without changing policy, lifecycle, primary-evidence, provenance, or recovery requirements.
- 2026-10-04 completion policy remains unchanged: The actual workspace preflight rejects the absent canonical completion contract (`scope_digest_mismatch`); its generated report is `_bmad-output/implementation-artifacts/evidence/reports/1-1-scaffold-the-runnable-canonical-module-foundation-approved-preflight.json`. A separate actual-gate run against byte-identical current story/sprint/policy files and a system-temp prospective contract rejects `status_mismatch` at `story-transition`; report `_bmad-output/implementation-artifacts/evidence/reports/1-1-scaffold-the-runnable-canonical-module-foundation-current-status-preflight.json`. Subsequent scope/digest/primary/TRX/provenance checks were not evaluated. A valid future done event requires an independent committed base with this exact product story and sprint entry both at `review`, followed by the policy-defined completion transition. The original implementation baseline `1047ef38d3845406227891639aaeb853e5d4f116` is unchanged. Original-baseline mixed user history, source deletions, and the five primary evidence classes (browser, SignalR, hosting assets, Aspire/Dapr, recovery) remain to be reconciled; ordinary CTRF, focused tests, and self-tests cannot replace their current contract-bound primary evidence. No destructive recovery producer was run while the production plan's lifecycle prerequisite remains unsatisfied. Story/sprint stay in-progress and the completion evidence task stays unchecked.

## File List

Exact paths owned by the approved 2026-10-04 follow-up are listed below. Earlier committed foundation changes remain recorded in the Code Map and historical implementation notes; unrelated user history is not claimed as this follow-up scope.

- `.github/scripts/run-merge-test-lanes.sh`
- `README.md`
- `_bmad-output/implementation-artifacts/spec-1-1-scaffold-the-runnable-canonical-module-foundation.md`
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`
- `docs/story-evidence-integrity.md`
- `references/Hexalith.Builds/Props/Directory.Packages.props`
- `references/Hexalith.Builds/Tools/test-authoritative-package-catalog.ps1`
- `references/Hexalith.Builds/Tools/test-consumer-package-authority-validator.ps1`
- `references/Hexalith.Builds/Tools/validate-central-package-versions.ps1`
- `references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1`
- `references/Hexalith.Folders/Directory.Packages.props`
- `src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml`
- `src/Hexalith.ChatBot.AppHost/Program.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/OperatingBaselineCatalog.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/IngestionBindingFinalizeActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/IngestionBindingGetStatusActivity.cs`
- `src/Hexalith.ChatBot.Server/Lifecycle/Workflows/IngestionBindingStartSourceActivity.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/IIngestionBindingSourceAdapter.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/MemoriesDerivedStoreServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/MemoriesIngestionBindingSourceAdapter.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `tests/Hexalith.ChatBot.AppHost.Tests/AppHostTopologyTests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ReleaseWorkflowSafetyTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OperatingBaselineAddendumDriftTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/MemoriesProbeCommandGateway.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/MemoriesProbeDomainTransport.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/InMemoryRecoveryReadModelStore.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriver.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/LiveProjectionRebuildDriverTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/Recovery/RecoveryValidationTopologyContractTests.cs`
- `tests/Hexalith.ChatBot.IntegrationTests/TrivialGovernedCommandAspireE2eTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Lifecycle/Workflows/IngestionBindingActivitiesTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Story12CssRetirementE2ETests.cs`
- `tests/PackageCatalogTestHelper.cs`

## Spec Change Log

- 2026-10-04: Reconciled the SDK acceptance wording with the committed `global.json` patch update to 10.0.401. CI/release now install from `global.json`, and the guard preserves the .NET 10.0.4xx feature band plus `latestPatch` policy. The existing shared catalog's xUnit 4.0.1 and bunit 2.11.3 updates are consumed without adding a version authority or changing the frozen intent or baseline commit.

## Review Triage Log

## Design Notes

Historical design baseline: `3c787993213ccf33f8912e6ad5caac605586fa15`. The implementation review baseline remains the frontmatter's `1047ef38d3845406227891639aaeb853e5d4f116`. Prefer mechanical file moves. AppHost executable references are local composition resources, not library dependency authority.

## Verification

**Current-checkout verification (2026-10-04, HEAD `5c807dfe7c4ac8eb37a91d5b232017006e4119d5`):**

- All nine implementation tasks already have their recorded source changes; the completion-evidence task remains open. SDK `10.0.401`; `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` and `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` both exit 0, with zero build warnings/errors. Logs: `/tmp/story-1-1-current-dispatch-restore.log` and `/tmp/story-1-1-current-dispatch-build.log`.
- After that build, independent native xUnit 4 runner invocations pass the scaffold/release-workflow guard classes **64/64**, AppHost tests **17/17**, and StoryEvidenceGate self-tests **219/219**, each with `XUNIT_REQUIRE_ZERO_SKIPS=1` and zero skips. Current CTRF, native TRX, and runner checksum outputs are under `TestResults/story-1-1/current-dispatch/`; logs are `/tmp/story-1-1-current-dispatch-{foundation,apphost,gate-selftests}.log`. These focused checks do not replace the earlier ordinary/live execution or contract-bound primary evidence.
- Actual workspace completion validation against the unchanged original implementation baseline and current HEAD exits 1: `scope_digest_mismatch`, subject `1-1-scaffold-the-runnable-canonical-module-foundation.json`, because the canonical completion contract is absent. Generated report: `_bmad-output/implementation-artifacts/evidence/reports/1-1-scaffold-the-runnable-canonical-module-foundation-dispatch-preflight.json`.
- The actual gate against a byte-identical current story/sprint/policy snapshot and a system-temp prospective contract also exits 1: `status_mismatch`, subject `story-transition`. Generated report: `_bmad-output/implementation-artifacts/evidence/reports/1-1-scaffold-the-runnable-canonical-module-foundation-dispatch-current-status-preflight.json`. Scope, digest, results, mappings, primary paths, and provenance were not evaluated. The next lifecycle prerequisite is an independently committed base with this exact story and sprint entry both at `review`, before the policy-defined completion transition. That prerequisite does not remove the subsequent exact-scope and five current primary-evidence obligations. Story/sprint remain `in-progress`; no active contract, attestation, destructive recovery run, staging, or commit was produced.

Exact workspace completion-preflight command (exit 1, missing canonical contract):

```bash
dotnet tools/Hexalith.ChatBot.StoryEvidenceGate/bin/Release/net10.0/Hexalith.ChatBot.StoryEvidenceGate.dll validate \
  --story _bmad-output/implementation-artifacts/spec-1-1-scaffold-the-runnable-canonical-module-foundation.md \
  --contract _bmad-output/implementation-artifacts/evidence/1-1-scaffold-the-runnable-canonical-module-foundation.json \
  --target-status done \
  --base 1047ef38d3845406227891639aaeb853e5d4f116 \
  --head 5c807dfe7c4ac8eb37a91d5b232017006e4119d5 \
  --results TestResults \
  --report _bmad-output/implementation-artifacts/evidence/reports/1-1-scaffold-the-runnable-canonical-module-foundation-dispatch-preflight.json
```

**Final approved-follow-up verification (2026-10-04):**

- SDK `10.0.401`; `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` passes without feed/version overrides (`/tmp/story-1-1-new-restore.log`). After all final code/assertion edits, `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` passes with zero warnings/errors (`/tmp/story-1-1-final-build.log`). The earlier cold NuGet.org-only restore below remains the independent cold-cache observation.
- From `references/Hexalith.Builds`, `pwsh -NoProfile -File Tools/test-consumer-package-authority-validator.ps1`, `Tools/test-central-package-version-validator.ps1`, and `Tools/test-authoritative-package-catalog.ps1` pass 32 consumer scenarios, 17 central scenarios, and 50 required package identities plus three shared families. Logs: `/tmp/story-1-1-authority-regressions.log`, `/tmp/story-1-1-central-regressions-new.log`, `/tmp/story-1-1-catalog-regressions-new.log`.
- From `references/Hexalith.Builds`, `pwsh -NoProfile -File Tools/validate-consumer-package-authority.ps1 -RepositoryRoot <consumer> -CatalogPath Props/Directory.Packages.props` passes for `../Hexalith.Folders` (34 projects), `../..` (ChatBot, 25), and `../Hexalith.Conversations` (15). Projects (24) passes with `-RepositoryRoot ../Hexalith.Projects -ExcludedPath _bmad-output/implementation-artifacts/qualification-evidence/6-1-p1r-3102-20260905/remote-consumer-3102/RemoteConsumer.csproj`; that single unchanged tracked standalone qualification probe is explicitly excluded. Corresponding logs are `/tmp/story-1-1-folders-authority-new.log`, `/tmp/story-1-1-chatbot-authority-final.log`, `/tmp/story-1-1-conversations-authority-final.log`, and `/tmp/story-1-1-projects-authority-final.log`.
- Focused invariant checks passed: ingestion activities 6/6, correction-workflow conformance 3/3, compatibility SLO mirror 2/2, complete tenant-isolated rebuild 9/9, and actual-renderer UI guards 11/11. The final ordinary lanes below reran those code paths after the final fixture/assertion changes.
- `env -u HEXALITH_CHATBOT_TIER3 -u HEXALITH_CHATBOT_TIER3_REQUIRED -u XUNIT_REQUIRE_ZERO_SKIPS MERGE_TEST_RESULTS_ROOT=TestResults/story-1-1/ordinary-final bash .github/scripts/run-merge-test-lanes.sh` exits 0 after every independent lane. Log `/tmp/story-1-1-ordinary-verified.log`; native per-project CTRF/TRX/checksums in `TestResults/story-1-1/ordinary-final/`. Total: **3,607 total tests, 3,601 passed, zero failed, six optional live Integration skips**. These optional skips are separate from the zero-skip required live execution.

| Ordinary lane | Total | Passed | Failed | Optional skips |
| --- | ---: | ---: | ---: | ---: |
| AppHost | 17 | 17 | 0 | 0 |
| Architecture | 117 | 117 | 0 | 0 |
| Cli | 24 | 24 | 0 | 0 |
| Client | 47 | 47 | 0 | 0 |
| Conformance | 100 | 100 | 0 | 0 |
| Contracts | 484 | 484 | 0 | 0 |
| Integration | 342 | 336 | 0 | 6 |
| Mcp | 30 | 30 | 0 | 0 |
| Server | 1,917 | 1,917 | 0 | 0 |
| Testing | 41 | 41 | 0 | 0 |
| UI.E2E | 152 | 152 | 0 | 0 |
| UI | 304 | 304 | 0 | 0 |
| Workers | 32 | 32 | 0 | 0 |

- Required live execution: `python3 /tmp/story-1-1-run-live.py primary class` creates three unique task-owned, disposable Redis/placement/scheduler containers, sets `HEXALITH_CHATBOT_TIER3=1`, `HEXALITH_CHATBOT_TIER3_REQUIRED=1`, `XUNIT_REQUIRE_ZERO_SKIPS=1`, and invokes `bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.IntegrationTests/bin/Release/net10.0/Hexalith.ChatBot.IntegrationTests TestResults/story-1-1/live-primary.ctrf.json -class Hexalith.ChatBot.IntegrationTests.TrivialGovernedCommandAspireE2eTests -result-trx TestResults/story-1-1/aspire-dapr-primary.trx`. **All 44 tests pass, zero failures/skips** (`/tmp/story-1-1-live-primary.log`). Native TRX has the TeamTest 2010 namespace, a direct `Times` element, 44 results, and total/executed/passed 44 with failed/notExecuted 0. SHA-256: `faa52cd530dd156ce44d5f313002b54e5d970e7bdc08278e699316e9d44193c2`.
- Live assertions observed all ten required resources healthy, all six sidecars, the four canonical Dapr components, governed command/state/audit/replay, both owner-provisioned tenants Active, four real accepted and persisted EventStore owner commands, two SDK owner state replays, and real anonymous/outside-scope denials. Final M2 metadata has all three sweeps covered with no breaches, `stopShipReasons=[]`, and `isStopShip=false`. This is evidence for this test composition, subject to the explicit production owner-host prerequisite above.
- Exact owner binding: live Aspire launched the actual Memories project with explicit Release `dotnet run --no-build`; the transport loaded `/home/administrator/projects/hexalith/chatbot/references/Hexalith.Memories/src/Hexalith.Memories.Server/bin/Release/net10.0/Hexalith.Memories.EventStore.dll`, verified its actual loaded location/deployed SHA-256 `c8d545b2d9a1536e93ac0061aa2c57e29d025b340905f8ac58389f4cc0b7cdba`, and checked the colocated Server runtime deps entry. AssemblyVersion is `1.0.0.0`; the deps library version is `1.0.0`. No Debug/source fallback, bootstrap feed, or version override was introduced.
- `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.StoryEvidenceGate.Tests/bin/Release/net10.0/Hexalith.ChatBot.StoryEvidenceGate.Tests TestResults/story-1-1/story-evidence-gate-final.ctrf.json -result-trx TestResults/story-1-1/story-evidence-gate-final.trx` passes **219/219 with zero skips** (`/tmp/story-1-1-gate-selftests-final.log`). Native TRX has 219 results/executed/passed and failed/notExecuted 0; SHA-256 `df83e3679f8795f18cd2ba307a18636cc1ed760192453fdf2eb4bdcd126ef9ce`. This proves gate self-tests, not Story 1.1 completion.
- Cleanup: the live AppHosts and their fresh Memories Redis/FalkorDB/workflow stores were disposed; `/tmp/story-1-1-live-primary-cleanup.log` records removal of the three task-owned core containers. Safe Aspire metadata `/tmp/story-1-1-final-aspire-cleanup.json` shows only the preserved pre-existing external Tenants AppHost. Existing external containers/Dapr defaults and local volumes were preserved. Each of the 42 test-generated tracked Story 13.9 screenshots was restored to its pre-test original after unchanged HEAD/index/hash checks; diagnostics remain in `TestResults/story-1-1/screenshot-diagnostics-final/`, manifest `/tmp/story-1-1-screenshot-cleanup-final.json`. `git diff --check` passes in ChatBot, Builds, and Folders. Captured final root HEAD is `97bf6063782d1a4af1b63bc0e4df6681dff98b42`; user commits/pointers were preserved.
- Completion preflight still rejects the missing canonical contract and, in the separate byte-identical current-status snapshot with a system-temp draft, `status_mismatch/story-transition`. The reports and unevaluated later obligations are described above. No active prospective contract, completion provenance, done transition, or recovery-primary attestation was fabricated; the completion-evidence task remains unchecked and story/sprint remain in-progress.

**Earlier verification (2026-10-04, failures superseded by the approved follow-up above):**
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
