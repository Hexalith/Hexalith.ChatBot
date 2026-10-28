---
title: 'Restore package-correct CI for a synchronized release candidate'
type: 'bugfix'
created: '2026-10-28'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'f568d43e99d2779e8d81bcd49552c7441d34de69'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="User authorized CI fixes, synchronization and release without further human validation">

## Intent

**Problem:** The latest ChatBot CI run fails three architecture checks after adopting native Folders integration. Root submodules have advanced, but the build still forces source dependencies whose required APIs now exist in published packages. The user's goal is to push all changes, synchronize the repository, fix CI/CD, create a release and verify NuGet publication.

**Approach:** Restore the existing Release/package-mode contract using the published Folders 1.3.2 and EventStore 3.128.3 APIs, retain local source development, and verify the ordinary CI lanes. The parent agent owns Git synchronization, commit validation, pushes, release execution and registry checks. This implementation owns build and architecture corrections only.

## Boundaries & Constraints

**Always:** Preserve the intent of the existing uncommitted architecture-test edits. Package versions belong in the authoritative Builds catalog, never a ChatBot override. Read owning guidance and configuration before edits. Preserve auditing, warnings-as-errors, test counts, automated release requirements, and current readiness metadata. Initialize no nested submodules. Do not load skills under references. Use root-declared source paths when an AppHost composition build requires executable source projects. Record exact commands and unresolved blockers. The user already authorized proceeding with a dirty tree and bypassing human checkpoints.

**Never:** Make commits, push, change workflow triggers to bypass checks, mark unavailable atomic participation as accepted, fabricate evidence, disable failing acceptance tests, or assert release/production readiness. The release M2 check intentionally requires atomic participation and Story 1.8 is in backlog; preserve its failure until implemented. Do not implement that product capability as a CI workaround.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Release dependencies | CI or explicit package mode | Folders Client/Contracts resolve from NuGet 1.3.2; EventStore uses published 3.128.3 | Missing packages or APIs fail build |
| Local source development | Debug and explicit source mode | Existing root sibling project references remain available | Report missing Platform enrollment mapping accurately |
| Native owner fixture | Package-mode Server tests | Source-only owner fixture stays excluded through its existing Compile Remove | Do not pretend the fixture ran |
| Lifecycle vocabulary | Folders native evidence source | Native accepted/pending evidence tokens do not count as ChatBot lifecycle literals | Guard other ChatBot lifecycle sources normally |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props`: current EventStore version is 3.128.3; Folders remains 1.3.1 although 1.3.2 is published. Update only required published dependency pins.
- `src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj`: native Folders Client/Contracts project references are unconditional; pair source and package groups like existing sibling dependencies.
- `tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj`: has an unconditional Folders Client reference and a source-only owner fixture alias; retain its package-mode exclusion and inspect the architecture guard's handling of this fixture.
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`: existing user edits exempt Folders source references and native evidence vocabulary. With new packages, restore full package counterpart enforcement while preserving the legitimate vocabulary distinction. Keep any fixture exemption precise and justified.
- `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.github/scripts/build-release-configuration.sh`: force EventStore source mode to obtain NativeSourceProof, now published in 3.128.3. Remove that obsolete force or explicitly select package mode; retain root paths used by AppHost resources.
- `.github/scripts/run-merge-test-lanes.sh`: authoritative list of thirteen ordinary test lanes, invoked independently through the v4 runner. Failures are retained in per-lane runner JSONL and CTRF files.
- `references/Hexalith.Conversations/Directory.Build.props`: newly synchronized source requires Platform tool-selected enrollment mappings. Do not invent mapping hashes or bypass validation.
- `release-metadata.json`, `.github/workflows/release.yml`, `ChatBotAdmissionEvidenceValidator.ValidateAtomicParticipationAsync`: independent release readiness and acceptance constraints remain unchanged.

## Tasks & Acceptance

**Execution:**

- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- advance Folders to published 1.3.2 and validate the owning catalog.
- [x] Server and Server.Tests project files -- pair source/package Folders dependencies and preserve the legitimate source-only owner fixture boundary.
- [x] `ScaffoldArchitectureTests.cs` -- remove obsolete Folders source workaround, retain native vocabulary intent, and keep package graph assertions meaningful.
- [x] CI/release workflows and Release build script -- stop forcing obsolete EventStore source mode.
- [x] Build and test relevant projects, then execute ordinary lanes as feasible; fix actual compatibility regressions without weakening checks.

**Acceptance Criteria:**

- Given current published dependencies, when the CI-equivalent Release build runs, then ChatBot uses package dependencies and required AppHost source resources without unpublished library API workarounds.
- Given the architecture suite, when it inspects the dependency graph and native evidence vocabulary, then all relevant guards pass without broad source exemptions.
- Given automated release blockers remain, when reporting verification, then distinguish tested fixes from blocked topology or product acceptance and retain their exact errors.

## Implementation Notes

- Final synchronization fetched and fast-forwarded only the sixteen root-declared checkouts; no nested checkout was initialized. Incoming upstream `52986c7e436fd06dd471c122ac103108fdeaa743` introduced `NativeEvidenceTokens.Accepted`. The rebase preserved that production refactor and replaced its obsolete source-library exemptions with the published-package graph. The lifecycle exception now allows only `accepted` in its dedicated token owner; running/succeeded/cancelled/pending remain rejected there.
- The follow-up solution Release build against the Aspire 13.6.4 catalog completed with zero warnings/errors in 9m34s. After the concurrent upstream merge and final submodule fast-forwards, focused Architecture and Server.Tests Release builds also passed with zero warnings/errors. Post-rebase architecture execution remains 158/159: FrontComposer still selects EventStore 3.128.2 instead of the authoritative 3.128.3. The eight newly enabled native package HTTP failure cases passed. Four post-rebase mutation controls verified that the dedicated native token owner rejects running, succeeded, cancelled and pending; each probed file was restored exactly.
- All three declared libraries plus their symbol packages passed local pack preflight at candidate version 1.83.0. Each archive contains its README; Client/Testing depend on Contracts at 1.83.0. An isolated consumer restored the local feed and loaded the real Client assembly. These are local preflight artifacts, not publication evidence or a release-candidate provenance claim.
- Official NuGet flat-container indexes returned HTTP 404 for Contracts, Client and Testing. Existing GitHub release v1.82.0 has no assets. The current workflow already has no human approval environment, but its automated M2 acceptance requires Story 1.8: `ChatBotAdmissionEvidenceValidator.ValidateAtomicParticipationAsync` still unconditionally refuses canonical execution. Prior live runs also failed Memories tenant provisioning and mailbox authority admission. No automated gate, authorization check, readiness metadata or atomicity refusal was disabled.
- Work remains in progress. The user has been asked for explicit authorization to update recorded nested Builds pins where required by the repository baseline. Without that approval, wrapper reconciliation is pending; positive native-package owner persistence/replay qualification and the Story 1.8 release prerequisite remain unresolved.

- Restored Folders Client/Contracts source/package pairing and retained the package-mode exclusion of the native owner fixture. The architecture guard exempts exactly its aliased Folders Server resource, asserts the source-only condition and fixture exclusion, and still enforces library package counterparts.
- Preserved the existing native Folders evidence-vocabulary distinction. Source-mode `ExcludeAssets="all"` package suppression entries cannot satisfy package-mode counterparts; Server.Tests now has a usable package-mode Commons.UniqueIds reference alongside its source sentinel.
- CI/release and the focused Release build script explicitly select EventStore package mode while preserving root executable resource paths.
- FrontComposer's stale EventStore 3.128.2 pre-import override was identified and temporarily removed to diagnose the shared-authority failure. Its recorded Builds gitlink selects EventStore 3.128.0, so the parent explicitly directed restoration of only that two-line removal to avoid a standalone restore regression pending nested dependency-pin authorization. No FrontComposer content change remains.
- PackageCatalogTestHelper passes the canonical `HexalithBuildsRoot` as well as older resolver property names, supporting the synchronized Conversations wrapper without relying on inherited process environment.
- Parent owns Builds catalog and revision-bound audit commits/publication. The parent reported successful refresh of only the five Folders 1.3.2 entries and passing audit validation (307 packages / 151 families); current Builds revision is `e111d0f841674e84c4b536b0eeee2d8ef4f68693`.
- Parent's pre-edit Aspire baseline: `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json` exited 2 because Conversations requires Platform tool-selected enrollment mapping. Log: `/home/administrator/.aspire/logs/cli_20261027T235540227_detach-child_2838ddc92c71418fbf87ee69d8eb0842.log`. No mapping hash or acceptance state was invented.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence | Outcome |
|---------|----------------------|---------|
| Blind 1: Aspire audit stale after upstream catalog update | High: upstream `1745ea0f09df9e518aeea3eeb1a7df836f67e09d` changed thirteen Aspire versions without refreshing its audit; Actions reported fourteen validation errors. | Refreshed only the Aspire family, preserved unrelated rows, validated all 307 packages/151 families, and pushed `d3633e14ebacea66005af07d56432cb2a3948d24`. |
| Blind 2: sibling EventStore selectors override authority | High: FrontComposer selects 3.128.2, Parties/Works select 3.128.0, and Timesheets selects 3.128.2 instead of shared 3.128.3. | Open. Recorded nested Builds pin updates require the authorization requested under AGENTS.md; no nested checkout was initialized. |
| Blind 3: sibling-owned package IDs bypass the catalog | High: Parties defines Contracts.UI and Works defines three Microsoft.Build package IDs locally. | Open together with the catalog-authority correction; fixing only EventStore selectors would leave the guard failing. |
| Blind 4: package-mode native failure tests excluded | Medium: three fixture-independent methods were in the source-only owner file. | Moved eight cases into an always-compiled class using the real native package. Positive owner persistence/replay qualification remains separate. |
| Blind 5: native-file lifecycle exemption too broad | Medium: excluding the whole file also allowed running/succeeded/cancelled. | Narrowed the exception to accepted/pending matches only. |
| Blind 6: child-element ExcludeAssets not recognized | Low: usable-counterpart filtering read only XML attributes. | Read attribute or child-element metadata, trimming and comparing case-insensitively. |
| Blind 7: AppHost source traversal emits Debug outputs | Medium: both CI-equivalent solution builds traverse typed resource projects in Debug as well as Release. | Existing limitation retained explicitly; no wholly Release topology claim is made. |
| Blind 8: AppHost SDK/catalog compatibility not qualified | Maybe-false: SDK 13.5.3 with hosting packages 13.6.4 needs live qualification; a version-number difference alone does not establish runtime failure. | Compile evidence is recorded separately from the blocked live topology evidence. |
| Edge 1: lifecycle exemption permits unrelated tokens | Medium: same verified guard gap as Blind 5. | Same narrow token correction; finding retained independently. |
| Edge 2: shared-authority assertion remains red | High: same verified selector mismatch as Blind 2. | Remains open; the assertion is preserved. |
| Verification 1: no executed positive native-package owner round trip | Medium: real-owner persistence/replay tests require the source-only Folders owner fixture, which package mode excludes. | Open qualification gap. The newly executed negative package tests do not establish positive persistence, owner identity, or duplicate suppression. |

## Verification

- Initial focused Release architecture build passed with zero warnings/errors (23.99 s), using Folders Client/Contracts 1.3.2 and EventStore.Contracts 3.128.3 as NuGet package libraries in `project.assets.json`.
- Builds checks: `pwsh -NoProfile -File Tools/validate-central-package-versions.ps1` passed 307 entries; `pwsh -NoProfile -File Tools/test-authoritative-package-catalog.ps1` passed 50 approved identities and three shared versions. Initial revision-bound audit validation correctly failed pending the parent's catalog commit and audit refresh; parent subsequently reported its successful validation.
- Live NuGet flat-container indexes list all five newly pinned Folders packages at 1.3.2, plus EventStore.Contracts 3.128.3.
- MSBuild mode evaluation confirms Server.Tests Release has no Folders source project references and excludes `FoldersNativeAttachmentTests.cs`; Debug keeps Folders Server/Client project references and includes the fixture.
- CI-equivalent solution command prefix: `env CI=true HexalithCommonsRoot="$PWD/references/Hexalith.Commons" HexalithBuildsRoot="$PWD/references/Hexalith.Builds" HexalithEventStoreRoot="$PWD/references/Hexalith.EventStore" HexalithEventStoreFromSource=false`. With this prefix, `dotnet restore Hexalith.ChatBot.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` passed (71 projects), then `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` passed with zero warnings/errors (2m27s). Logs: `/tmp/chatbot-solution-release-restore.log`, `/tmp/chatbot-solution-release-build.log`. Existing AppHost resource traversal also emits Debug assets; this is not evidence of a wholly Release topology.
- Architecture runner after graph fixes: 158/159 passed, zero skipped; remaining shared-authority failure comes from synchronized sibling wrappers, first reported at Parties' locally defined `Hexalith.FrontComposer.Contracts.UI`. Additional observed drift exists in Parties/Works EventStore selectors and Timesheets package overrides. No guard was disabled. Evidence: `/tmp/chatbot-ci-verification/architecture-ci.ctrf.json` and `/tmp/chatbot-ci-verification-architecture-ci.runner.jsonl`.

- `bash .github/scripts/build-release-configuration.sh tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj` -- clean focused build, or retain exact upstream blocker.
- Direct architecture runner using `.github/scripts/run-xunit-v4.sh` -- nonempty passing CTRF results for relevant checks.
- CI-equivalent solution restore/build followed by `.github/scripts/run-merge-test-lanes.sh` -- thirteen ordinary lanes execute; report any independent failure.
- Builds catalog's existing focused validation -- catalog remains authoritative and every newly pinned package exists.

- Final focused architecture Release build passed with zero warnings/errors (19.60 s), including the canonical Builds-root helper change. Final direct v4 architecture run executed 159 tests: 158 passed, one failed, zero skipped. Exact remaining error: `The package wrapper '/home/administrator/projects/hexalith/chatbot/references/Hexalith.FrontComposer/Directory.Packages.props' changes 'Hexalith.EventStore.Admin.Abstractions' from catalog version '3.128.3' to '3.128.2'.` Final evidence: `/tmp/chatbot-ci-verification/architecture-final.ctrf.json` and `/tmp/chatbot-ci-verification-architecture-final.runner.jsonl`.
- `env HexalithBuildsRoot="$PWD/references/Hexalith.Builds" MERGE_TEST_RESULTS_ROOT=/tmp/chatbot-ci-verification/merge-test-lanes bash .github/scripts/run-merge-test-lanes.sh` correctly exited 1 after AppHost and Architecture. The remaining eleven ordinary lanes were invoked independently through `.github/scripts/run-xunit-v4.sh` with identical per-lane CTRF/TRX and runner JSONL paths; every remaining lane passed. All thirteen ordinary lanes therefore executed, but the authoritative merge script is still failing closed on shared catalog authority. Current ordinary-lane counts:

| Lane | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Hexalith.ChatBot.AppHost.Tests | 19 | 0 | 0 |
| Hexalith.ChatBot.Architecture.Tests | 158 | 1 | 0 |
| Hexalith.ChatBot.Cli.Tests | 61 | 0 | 0 |
| Hexalith.ChatBot.Client.Tests | 95 | 0 | 0 |
| Hexalith.ChatBot.Conformance.Tests | 151 | 0 | 0 |
| Hexalith.ChatBot.Contracts.Tests | 531 | 0 | 0 |
| Hexalith.ChatBot.IntegrationTests | 445 | 0 | 7 |
| Hexalith.ChatBot.Mcp.Tests | 70 | 0 | 0 |
| Hexalith.ChatBot.Server.Tests | 3447 | 0 | 0 |
| Hexalith.ChatBot.Testing.Tests | 41 | 0 | 0 |
| Hexalith.ChatBot.UI.E2E.Tests | 265 | 0 | 0 |
| Hexalith.ChatBot.UI.Tests | 499 | 0 | 0 |
| Hexalith.ChatBot.Workers.Tests | 32 | 0 | 0 |

- The seven Integration skips remain the existing Tier-3/Docker/Dapr, retained live recovery evidence, and production-browser acceptance prerequisites. Package-mode `FoldersNativeAttachmentTests.cs` remains excluded and was not run. Story 1.8 atomic participation, release readiness metadata, and all automated acceptance gates remain unchanged.
- Remaining work belongs to the parent: explicit authorization for any nested dependency gitlink updates, synchronized sibling-wrapper authority corrections with valid standalone dependencies, independent review, Git operations, release execution, and NuGet verification. No release/production readiness is asserted.

- Across all thirteen ordinary lanes: 5,822 tests, 5,814 passed, one architecture failure, seven skips. The 42 screenshots regenerated by UI.E2E were retained under `/tmp/chatbot-ci-verification/generated-screenshots` and only their previously clean tracked files were restored to pre-task baseline bytes. Spec remains in progress because shared-authority acceptance is unresolved.
