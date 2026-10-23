---
title: Preserve authoritative catalog behavior after sibling synchronization
type: bugfix
created: 2026-10-23
status: in-review
route: dispatch
review_loop_iteration: 0
baseline_commit: d718220801f7855dc4c7b6c0d140f3785e95d6e9
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.Timesheets/AGENTS.md'
---

<frozen-after-approval reason="user authorized synchronization and CI repairs">

## Intent

Fix the two ordinary architecture failures exposed by synchronizing upstream submodules. Preserve the root Builds catalog as the sole effective package-version authority, while retaining Timesheets standalone compatibility with its older pinned catalog. Align the MCP architecture assertion with the explicitly synchronized ModelContextProtocol 2.3.0 catalog pin. Do not weaken production or release acceptance gates, and do not update any nested submodule.

</frozen-after-approval>

## Code Map

- Timesheets Directory.Packages.props unconditionally declares Works.Contracts1.0.2 although root Builds now also supplies1.0.2. Its standalone Builds gitlink359fee24 lacks that entry, so deleting fallback breaks standalone restore.
- tests/PackageCatalogTestHelper.cs already performs bounded dotnet msbuild evaluation and verifies effective PackageVersion defining source and duplicate identities. Reuse this logic for sibling wrappers; additionally compare evaluated versions with the root snapshot so local Update metadata cannot silently override a catalog version.
- Root ScaffoldArchitectureTests currently forbids even inert fallback declarations and hardcodes obsolete MCP2.2.0. Replace the sibling declaration-only assertion with evaluated authority validation, retaining wrapper/root inventory and shared pin checks.

## Tasks & Acceptance

**Execution:**
- [x] Remove the obsolete HexalithPartiesVersion1.4.0 override from references/Hexalith.Works/Directory.Packages.props and validate its affected host restore/build; shared Builds selects1.5.2. No nested gitlink changes.
- [x] In references/Hexalith.Timesheets/Directory.Packages.props, condition the existing fallback on absence of the already imported exact PackageVersion identity using WithMetadataValue. Keep its published1.0.2 version and clarify the compatibility comment.
- [x] Align Folders client/contracts1.3.1 and Projects client/contracts1.2.0 expected pins with the synchronized catalog. Align AssertUiFoundationPins with synchronized catalog versions: Fluent components/icons5.0.1, Playwright1.64.0, xunit4.0.2, bunit2.12.5; retain Fluxor6.11.0. These are expected-pin updates, not new dependency changes.
- [x] Extract/reuse the bounded evaluator in tests/PackageCatalogTestHelper.cs and expose a sibling authority assertion. Evaluate actual wrappers with UseHexalithProjectReferences=false and these global resolver properties pointing to root Builds: Hexalith1BuildPackageProps, HexalithBuildPackageVersions1, HexalithBuildsResolvedRoot. Avoid a pre-import project because Platform imports its catalog unconditionally. Reject duplicate effective IDs, foreign defining source, omitted IDs, and changed versions relative to the root snapshot.
- [x] Update ScaffoldArchitectureTests to call the sibling authority assertion for every discovered wrapper and expect MCP2.3.0. Retain all dependency-boundary assertions. Add focused mutation coverage demonstrating inactive fallback succeeds while active duplicate Include and version-changing Update fail.

**Acceptance Criteria:**
- Given an imported Works.Contracts9.9.9, when Timesheets wrapper evaluates, then exactly that imported item remains; given no imported item, exactly the1.0.2 fallback appears.
- Given a sibling overriding/duplicating the root catalog, when authority validation runs, then it fails.
- Given current synchronized wrappers, when full architecture tests run, then all pass.

## Implementation Notes

Work sequentially; no parallel builds are active. Parent will commit/push Timesheets in its owning repository first, then separately record the parent gitlink and root tests. Do not commit, push, initialize submodules, or modify any other submodule beyond the named Timesheets and Works catalog corrections. Treat root Builds and Timesheets nested Builds gitlinks as read-only. Keep this plan in review after verification: docs/story-evidence-integrity.md requires separate completion enrollment and no completion is claimed here.

## Verification

- Evaluate actual Timesheets wrapper with temporary present/absent catalogs and verify one effective Works.Contracts item in each case.
- Build the architecture project Release serially and run its xUnit executable independently.
- Aggregate all13 independently executed ordinary Release lanes; rerun the changed architecture and UI lanes after the focused repair. The other11 lanes already passed against the synchronized catalog.

- Follow-up requirement: verify sibling effective versions against the root snapshot in addition to DefiningProjectFullPath, because item Update metadata can preserve the original defining source.

- Discovery: evaluated validation proved a previously hidden Works property override that selected six Parties-family packages at1.4.0 despite the shared catalog1.5.2 defaults. Owner repository configuration is now included in the non-frozen implementation scope. All14 other wrappers evaluate306 unique, authoritative items matching the root snapshot.

## Executed Verification

- `dotnet msbuild Directory.Packages.props -getProperty:HexalithPartiesVersion -p:UseHexalithProjectReferences=false` in Works: exit0, effective1.5.2.
- `dotnet restore src/Hexalith.Works/Hexalith.Works.csproj -p:Configuration=Release -p:UseHexalithProjectReferences=false -m:1 /nr:false`, followed by `dotnet build src/Hexalith.Works/Hexalith.Works.csproj --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false`: exit0, 0warnings/errors, using the workspace root Builds catalog. Logs `/tmp/chatbot-release-investigation/works-{restore,build}.log`. This does not claim a restore against Works' separate standalone pinned catalog.
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false`, then its Release executable with `-noLogo -result-trx /tmp/catalog-sync-architecture-final.trx`: exit0, 158passed, 0failed/skipped before review patches. This includes both actual Timesheets wrapper present/absent catalog cases and all sibling wrappers.
- UI Release build and executable: exit0, 499passed, 0failed/skipped; `/tmp/catalog-sync-ui.trx`.
- After non-overlapping upstream updates, `dotnet build Hexalith.ChatBot.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -m:1 /nr:false` with the same explicit Commons/Builds/EventStore root environment as CI: exit0, 0warnings/errors; `/tmp/chatbot-release-investigation/final-release-build.log`.
- No live authority, M2 atomic participation, recovery, or readiness completion is claimed. Those are still open release blockers documented separately.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind1: Release-only catalog mutations escape unset configuration | low | Real test guard gap: explicitly evaluate Release in the common evaluator and add a conditional Update mutation. Patch. |
| Blind2: injected resolver globals bypass standalone resolver defaults | false | Controlled root-catalog evaluation is intentional. Siblings may use older standalone catalogs, including the supported Timesheets missing-contract fallback. Their natural standalone resolver is not required to select this workspace catalog. Real solution build separately verifies actual workspace wiring. |
| Blind3: require transitive pinning on every sibling | false | Source inspection shows this is ChatBot consumer-wrapper policy; multiple valid siblings and shared Builds omit it. Catalog authority requires matching effective versions and origin, not a new global transitive-pinning policy. |
| Blind4: duplicate fallback regression coverage inside Timesheets CI | false | Both actual Timesheets wrapper branches are already executed by the normal root architecture lane against controlled imported catalogs and assert version, uniqueness and defining source. The proposed additional test location demonstrates no unverified behavior in this change. |
| Blind5: duplicate/foreign diagnostics omit wrapper | low | Real developer diagnostic gap in the sibling loop. Include the evaluated wrapper path using a private signature change. Patch. |
| Blind6: repair spec should record exact verification commands | low | Rejected as a finding whose fix edits this build's spec, as required by the review workflow. This record's normal verification section now retains commands, results and local evidence locations. |
| Edge-case reviewer | false | Empty findings array; no unhandled path or falsified claim reported. |
| Verification-gap reviewer | false | No verification gaps found after inspecting consumers and tests. |

- Final ordinary run: `MERGE_TEST_RESULTS_ROOT=/tmp/chatbot-release-investigation/final-ordinary-release bash .github/scripts/run-merge-test-lanes.sh`: exit0, all13 lanes executed, 5589passed/0failed/7opt-in integration skips before the extra review regression case. No live acceptance is inferred from these ordinary-lane skips.
- Review patches: architecture build exit0/0warnings/errors; focused authority8/8, then full architecture159/159 and UI499/499, both zero skips. Evidence `/tmp/catalog-sync-review-focused.trx`, `/tmp/catalog-sync-reviewed-architecture.trx`, `/tmp/catalog-sync-reviewed-ui.trx`; UI build log `/tmp/chatbot-release-investigation/reviewed-ui-build.log`. Aggregated current ordinary tests: 5590passed, 0failed, 7explicit live opt-ins.
- The42 screenshots regenerated by UI.E2E were archived under `/tmp/chatbot-release-investigation/final-generated-story-13.9-screenshots/` and their previously clean tracked bytes restored. They are test output, not intended source changes.
- Timesheets catalog fix pushed as2733931; Works catalog fix pushed as23eacec. Other root-declared siblings were fast-forwarded to the upstream snapshot observed during final verification; no nested gitlinks were changed.
