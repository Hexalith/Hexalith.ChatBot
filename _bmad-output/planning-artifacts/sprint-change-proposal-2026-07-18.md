---
title: Sprint Change Proposal - Central NuGet Version Authority
project: Chatbot
date: 2026-07-18
status: approved
mode: Incremental
trigger: "Manage all NuGet package-reference versions in references/Hexalith.Builds/Props/Directory.Packages.props across the superproject and all root-declared submodules"
scope_classification: Moderate
recommended_approach: Direct Adjustment through Epic 1 Story 1.1e
owner: Jerome
prepared_by: Correct Course workflow
approved_by: Jerome
approved_at: 2026-07-18
implementation_state: planning-updates-applied-package-migration-not-started
handoff_status: routed-to-product-owner-and-developer
incremental_approvals:
  - "Proposal 1 - Epic 1 Story 1.1e"
  - "Proposal 2 - Architecture invariant"
  - "Proposal 3 - Repository rollout and version-resolution policy"
  - "Proposal 4 - Sprint tracking and handoff"
---

# Sprint Change Proposal - Central NuGet Version Authority

## 1. Issue Summary

**Trigger (Jerome, 2026-07-18):** All NuGet package-reference versions must be managed from
`references/Hexalith.Builds/Props/Directory.Packages.props`. Local package-version definitions in the
superproject and its root-declared submodules must move to `Hexalith.Builds`.

**Issue type:** New stakeholder requirement that strengthens an existing build-governance rule. The current
planning and architecture prohibit inline `PackageReference` versions but still permit repository-local
`PackageVersion` entries and overrides. This allows package-version drift even though Central Package
Management is enabled.

**Problem statement:** `Hexalith.Builds` is described as the shared package catalog, but seven repositories
still define dependency versions locally. The current test floor catches inline versions in project files but
does not enforce exclusive ownership by the shared catalog. The workspace therefore has multiple effective
version authorities and cannot guarantee one reproducible package graph across consumers.

### Evidence collected

The inventory covered the superproject and all 13 root-declared submodules without initializing nested
submodules.

| Evidence | Finding |
| --- | --- |
| Repositories inspected | Superproject plus 13 root-declared submodules |
| .NET projects | 281 |
| `Directory.Packages.props` files | 14 |
| Shared catalog entries | 266 package IDs |
| Local `<PackageVersion>` declarations | 102 |
| Local package-version properties | 1 (`HexalithCommonsVersion` in EventStore) |
| Repositories with local definitions | ChatBot plus EventStore, Parties, Memories, Commons, Timesheets, and PolymorphicSerializations |
| Local unique package IDs | 71 |
| Already equal to the shared catalog | 26 |
| Missing from the shared catalog | 15 |
| Different from the shared catalog | 30 package IDs, plus one property-based override |
| Inline `PackageReference` versions or `VersionOverride` | 0 |
| NuGet SDK resolver pins outside CPM | 10 `Aspire.AppHost.Sdk` pins |
| Repository tool pins outside CPM | 5 tools in 3 `.config/dotnet-tools.json` manifests |

The AppHost SDK and .NET tool pins are NuGet-delivered dependencies, but Central Package Management does not
resolve their versions from `Directory.Packages.props`. They must remain explicit, documented exceptions with
separate alignment checks; they must not become a reason to reintroduce local `PackageReference` versions.

## 2. Correct Course Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | N/A | No failing story triggered the change. Jerome issued a build-governance correction on 2026-07-18. Existing Stories 1.1a and 1.1d expose the gap because they establish local CPM and reject only inline project versions. |
| 1.2 Core problem | Done | New stakeholder requirement: replace multiple repository-local version authorities with one `Hexalith.Builds` catalog. |
| 1.3 Evidence | Done | Workspace inventory found 103 local version definitions, 15 missing catalog IDs, 30 differing package IDs, and one property override. |
| 2.1 Current epic | Done | Epic 1 remains viable. Add corrective Story 1.1e without rewriting completed Story 1.1a or 1.1d history. |
| 2.2 Epic-level change | Done | Add Story 1.1e under the scaffold work package and strengthen the cross-cutting build invariant. No new epic is required. |
| 2.3 Remaining epics | Done | Product behavior and story outcomes remain valid. Every remaining build/release lane consumes the stronger invariant. |
| 2.4 Future epic validity | Done | No planned epic becomes obsolete and no product epic is added or removed. |
| 2.5 Priority/order | Done | Implement Story 1.1e before further dependency changes or release-readiness sign-off. No epic resequencing is required. |
| 3.1 PRD conflict | N/A | Product requirements, actors, journeys, MVP scope, and NFR outcomes are unchanged. |
| 3.2 Architecture conflict | Done | Architecture currently names repository-root `Directory.Packages.props` as the catalog. It must distinguish the consumer wrapper from the authoritative Builds catalog and add exclusive-ownership rules. |
| 3.3 UI/UX conflict | N/A | No surface, flow, component, accessibility, responsive, or localization behavior changes. |
| 3.4 Other artifacts | Done | Builds catalog, validators, CI, README/sample, consumer wrappers, ChatBot architecture tests, cache inputs, Timesheets submodule declaration, and sprint status are affected. |
| 4.1 Direct Adjustment | Viable | Medium effort, medium risk. Add one corrective story and execute a catalog-first coordinated migration. |
| 4.2 Rollback | Not viable | No delivered product behavior needs rollback. Reverting existing package work would not establish shared ownership. |
| 4.3 PRD MVP Review | Not viable | MVP remains achievable and unchanged; this is build governance, not product-scope pressure. |
| 4.4 Recommended path | Done | Direct Adjustment through Story 1.1e, with the shared catalog updated before consumer overrides are removed. |
| 5.1 Issue summary | Done | Section 1. |
| 5.2 Epic/artifact impact | Done | Sections 3 and 5. |
| 5.3 Recommended path | Done | Section 4. |
| 5.4 MVP impact/action plan | Done | No MVP change; Sections 6 and 7 define implementation. |
| 5.5 Handoff | Done | Section 8. |
| 6.1 Checklist completion | Done | All applicable analysis items are resolved; final approval and post-approval tracking remain. |
| 6.2 Proposal accuracy | Done | Counts and matrices were derived from the checked-out root-declared repository set. |
| 6.3 Explicit user approval | Done | Incremental Proposals 1-4 and the assembled proposal were approved by Jerome on 2026-07-18. |
| 6.4 Sprint-status update | Done | Story 1.1e was added as `backlog`; Epic 1 remains `in-progress`. |
| 6.5 Next steps/handoff | Done | Catalog-first sequence and role ownership are explicit. |

## 3. Impact Analysis

### 3.1 Product and MVP impact

- No functional or non-functional product requirement changes.
- No user journey, UI surface, actor, command, event, or data contract changes.
- MVP remains achievable with the existing scope and sequence.
- No implementation rollback is proposed.

### 3.2 Epic and story impact

- Epic 1 remains `in-progress`.
- Completed Stories 1.1a and 1.1d remain unchanged historical records.
- New Story 1.1e owns the stronger package-version authority, migration, validation, and compatibility work.
- Other epics consume the corrected build baseline but require no story changes.

### 3.3 Architecture impact

The existing phrases "central package management" and "central `Directory.Packages.props`" are ambiguous
because they can describe either a repository-local catalog or the shared Builds catalog. The architecture must
state that:

1. `references/Hexalith.Builds/Props/Directory.Packages.props` is the sole package-reference version catalog.
2. Every consumer root retains a version-free `Directory.Packages.props` wrapper that imports the catalog.
3. No consumer may define `PackageVersion Include`, `PackageVersion Update`, dependency-version properties,
   `PackageReference Version`, or `VersionOverride`.
4. Architecture and CI tests evaluate the imported catalog rather than asserting version text in a wrapper.
5. NuGet SDK resolver and .NET tool manifest pins are explicit CPM-incompatible exceptions with alignment gates.

### 3.4 Secondary artifact impact

| Artifact or repository | Required adjustment |
| --- | --- |
| `references/Hexalith.Builds/Props/Directory.Packages.props` | Add missing package IDs and remain the sole version authority. |
| `references/Hexalith.Builds/Tools` | Extend validation to reject consumer-local version declarations and missing catalog imports. |
| `references/Hexalith.Builds` CI | Run catalog integrity and consumer-authority fixture tests. |
| Builds README and sample wrapper | Remove guidance that invites repository-specific `PackageVersion` entries. |
| Twelve .NET consumer roots | Use version-free wrappers importing the shared catalog. |
| Seven repositories with local definitions | Remove all 103 local definitions after the shared catalog is ready. |
| Timesheets | Add a root-declared `references/Hexalith.Builds` dependency for standalone consumption. |
| ChatBot architecture/UI tests | Resolve the imported catalog instead of reading hardcoded local package rows. |
| CI cache keys | Continue including both the wrapper and the imported shared catalog. |
| `sprint-status.yaml` | Add Story 1.1e as `backlog`; preserve all other statuses. |

## 4. Path Forward Evaluation

### Option 1 - Direct Adjustment through Epic 1 (recommended)

Add Story 1.1e, update the architecture invariant, update `Hexalith.Builds` first, and migrate consumers in
their owning repositories.

- **Effort:** Medium.
- **Risk:** Medium because 30 catalog conflicts and one property override intentionally change effective
  consumer versions.
- **Benefits:** One sustainable authority, mechanical drift prevention, clear ownership, and no product-scope
  disruption.
- **Mitigation:** Explicit version matrix, catalog-first sequence, independent consumer verification, and no
  local override escape hatch.

### Option 2 - Roll back local dependency work

**Rejected.** Rollback would remove useful package and compatibility work without supplying a central catalog
or enforcement mechanism. It would also obscure which consumers require remediation.

### Option 3 - Review or reduce the PRD MVP

**Rejected.** The issue is unrelated to product feasibility or scope. Reducing the MVP would not address build
drift.

### Recommendation

Approve Option 1 as a **Moderate Direct Adjustment**. Product planning remains intact; the Product
Owner/Architect update the build contract, and Developers/Build maintainers execute the coordinated migration.

## 5. Approved Planning Artifact Changes

### 5.1 `epics.md` - add Story 1.1e

Add the following assignable story beneath Story 1.1d in the Epic 1 scaffold work package:

#### Story 1.1e: Centralize NuGet package-reference version authority

As a platform engineer,
I want every Hexalith repository to obtain package-reference versions exclusively from `Hexalith.Builds`,
So that package versions cannot drift between the superproject and its submodules.

**Acceptance Criteria:**

**Given** the superproject and each root-declared .NET submodule
**When** Central Package Management evaluates dependency versions
**Then** `references/Hexalith.Builds/Props/Directory.Packages.props` is the sole owner of every dependency
`PackageVersion`
**And** each consumer-root `Directory.Packages.props` is a version-free wrapper importing that catalog.

**Given** package declarations in consumer repositories
**When** build-governance validation runs
**Then** local `PackageVersion Include`, `PackageVersion Update`, dependency-version properties,
`PackageReference Version`, and `VersionOverride` are rejected
**And** the shared catalog is evaluated successfully with unique, resolved, valid versions.

**Given** the migration inventory
**When** local definitions are removed
**Then** all 15 missing package IDs exist in the shared catalog
**And** each of the 30 conflicting package IDs plus the EventStore property override uses the approved canonical
version
**And** no effective-version change occurs without being represented in the migration matrix and verified in
the affected consumer.

**Given** NuGet SDK resolver and repository tool versions
**When** package authority is assessed
**Then** their CPM incompatibility is documented
**And** separate validation keeps AppHost SDK/Hosting families and repository tool manifests intentionally
aligned.

**Given** the catalog-first rollout
**When** completion evidence is recorded
**Then** each owning repository passes its relevant restore, build, and focused test lanes independently
**And** the complete superproject passes its relevant integration lanes without local version overrides.

### 5.2 `architecture.md` - strengthen the package-management invariant

Apply these semantic changes wherever package management appears:

- Replace repository-local catalog wording with the sole authority path
  `references/Hexalith.Builds/Props/Directory.Packages.props`.
- Describe root `Directory.Packages.props` as a version-free import wrapper.
- Add `references/Hexalith.Builds/` to the documented project structure as the build-policy and package-catalog
  source.
- Change the MCP package note from "repo-pinned" to "shared-catalog pinned and evaluated through the consumer
  wrapper."
- Add the prohibited local forms and the SDK/tool exception rule.
- Require architecture and CI validation to inspect evaluated imports rather than local text.

### 5.3 `sprint-status.yaml`

After final approval, add:

```yaml
1-1e-centralize-nuget-package-reference-version-authority: backlog
```

Keep `epic-1: in-progress`. Preserve every other status and all historical evidence.

### 5.4 PRD and UX

No changes. The PRD and complete binding UX package were reviewed and have no conflict with this correction.

## 6. Technical Migration Contract

### 6.1 Canonical resolution policy

1. When the shared catalog already contains a package ID, its current value is canonical.
2. Local definitions already equal to the catalog are removed without version change.
3. Missing package IDs are added with their current effective local version, except for the three approved
   family-alignment resolutions in Section 6.2.
4. Consumer compatibility problems are fixed in the consumer. A local version override is not an acceptable
   remediation.
5. Package IDs are compared case-insensitively and must be unique in the evaluated catalog.

### 6.2 Package IDs to add to `Hexalith.Builds`

| Package ID | Local evidence | Canonical version | Decision |
| --- | --- | --- | --- |
| `Dapr.AI` | Memories `1.18.4` | `1.18.4` | Preserve local version. |
| `Dapr.AI.Microsoft.Extensions` | Memories `1.18.4` | `1.18.4` | Preserve local version. |
| `Fluxor` | ChatBot `6.9.0` | `6.10.0` | Align with shared `Fluxor.Blazor.Web` `6.10.0`. |
| `Kreuzberg` | Memories `4.10.2` | `4.10.2` | Preserve local version. |
| `Microsoft.AspNetCore.Components.CustomElements` | Parties `10.0.9` | `10.0.10` | Align with the shared ASP.NET Core 10.0.10 family. |
| `Microsoft.Extensions.Diagnostics.Abstractions` | Commons `10.0.10` | `10.0.10` | Preserve local version. |
| `MinVer` | Four consumers `8.0.0-rc.1` | `8.0.0-rc.1` | Preserve common local version. |
| `NBomber.Http` | EventStore `6.2.1` | `6.2.1` | Preserve local version. |
| `NFalkorDB` | Memories `1.0.6` | `1.0.6` | Preserve local version. |
| `NRedisStack` | Memories `1.6.0` | `1.6.0` | Preserve local version. |
| `NetArchTest.eNhancedEdition` | ChatBot `1.4.5` | `1.4.5` | Preserve local version. |
| `OpenTelemetry` | ChatBot `1.16.0`; Memories `1.17.0` | `1.17.0` | Resolve the local split to the shared OTel core line. |
| `OpenTelemetry.Exporter.InMemory` | Memories `1.17.0` | `1.17.0` | Preserve local version. |
| `OpenTelemetry.Instrumentation.StackExchangeRedis` | Memories `1.16.0-beta.1` | `1.16.0-beta.1` | Preserve documented prerelease version. |
| `xunit.v3.extensibility.core` | EventStore `3.2.2` | `3.2.2` | Align with shared xUnit v3. |

### 6.3 Existing catalog conflicts - shared value wins

| Package ID | Local value and consumer | Canonical Builds value |
| --- | --- | --- |
| `ByteAether.Ulid` | ChatBot `1.3.7` | `1.3.8` |
| `CommunityToolkit.Aspire.Hosting.Dapr` | ChatBot, Timesheets `13.4.0-preview.1.260602-0230` | `13.4.1-beta.686` |
| `Fluxor.Blazor.Web` | ChatBot `6.9.0` | `6.10.0` |
| `MediatR` | ChatBot `14.1.0` | `14.2.0` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.AspNetCore.Mvc.Testing` | ChatBot, Timesheets `10.0.9` | `10.0.10` |
| `Microsoft.AspNetCore.OpenApi` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.AspNetCore.TestHost` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.Configuration.Binder` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.DependencyInjection` | Timesheets `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | ChatBot, Timesheets `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.Hosting` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.Hosting.Abstractions` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.Http` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.Http.Resilience` | ChatBot, Timesheets `10.7.0` | `10.8.0` |
| `Microsoft.Extensions.Logging.Abstractions` | ChatBot, Timesheets `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.Options` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | ChatBot `10.0.9` | `10.0.10` |
| `Microsoft.Extensions.ServiceDiscovery` | ChatBot, Timesheets `10.7.0` | `10.8.0` |
| `Microsoft.NET.Test.Sdk` | ChatBot, Timesheets `18.7.0` | `18.8.1` |
| `ModelContextProtocol` | ChatBot, Parties `1.4.0`; Memories `1.4.1` | `1.4.1` |
| `ModelContextProtocol.AspNetCore` | Parties `1.4.0` | `1.4.1` |
| `NSubstitute` | ChatBot, Memories `5.3.0`; Timesheets `6.0.0-rc.1` | `6.0.0` |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | ChatBot, Timesheets `1.16.0` | `1.17.0` |
| `OpenTelemetry.Extensions.Hosting` | ChatBot, Timesheets `1.16.0` | `1.17.0` |
| `OpenTelemetry.Instrumentation.Runtime` | ChatBot, Timesheets `1.15.1` | `1.16.0` |
| `System.CommandLine` | ChatBot, Memories `2.0.9` | `2.0.10` |
| `Testcontainers` | ChatBot `4.12.0` | `4.13.0` |
| `YamlDotNet` | ChatBot `18.0.0` | `18.1.0` |
| `bunit` | ChatBot `2.7.2` | `2.8.4-preview` |
| `HexalithCommonsVersion` property | EventStore `2.28.0` | Shared default `2.28.2` |

The remaining 26 local package IDs already equal the shared catalog and require only local declaration removal.

### 6.4 Consumer repository changes

| Repository | Current local definitions | Required action |
| --- | ---: | --- |
| ChatBot superproject | 57 | Replace catalog with version-free wrapper; update package-pin tests. |
| EventStore | 3 package rows + 1 property | Remove local items and `HexalithCommonsVersion` compatibility override. |
| Tenants | 0 | Retain and validate version-free shared import wrapper. |
| FrontComposer | 0 | Retain and validate version-free shared import wrapper. |
| Folders | 0 | Retain and validate version-free shared import wrapper. |
| Conversations | 0 | Retain and validate version-free shared import wrapper. |
| Projects | 0 | Retain and validate version-free shared import wrapper. |
| Parties | 4 | Remove local items; consume shared versions. |
| AI.Tools | N/A | No .NET projects or package props; no package migration. |
| Memories | 13 | Remove local items and compatibility comments; remediate against shared versions. |
| Commons | 2 | Remove local items; consume shared versions. |
| Builds | 266 shared entries | Add missing IDs; keep self-wrapper version-free. |
| Timesheets | 22 | Add root-declared Builds dependency, replace local catalog with wrapper, and remediate shared-version changes. |
| PolymorphicSerializations | 1 | Remove local item; consume shared version. |

### 6.5 Shared validation and CI

The Builds-owned validation must mechanically enforce:

- the central catalog evaluates successfully;
- every evaluated package ID is nonblank, case-insensitively unique, and has one resolved valid version;
- consumer project files contain no `PackageReference Version`, nested `Version`, or `VersionOverride`;
- consumer props contain no dependency `PackageVersion` or version-property workaround;
- every .NET consumer root imports the shared catalog through its wrapper;
- cache keys include the consumer wrapper and imported catalog;
- fixture tests prove each forbidden form fails and a valid wrapper passes.

ChatBot tests that currently read local `Directory.Packages.props` text must instead resolve or evaluate the
shared catalog. This includes the MCP boundary assertion and the package-pin assertions in the accessibility,
responsive, localization, and live-region test suites.

### 6.6 CPM-incompatible exceptions

The following are explicitly outside `Directory.Packages.props` resolution:

- ten `Aspire.AppHost.Sdk/<version>` project SDK declarations;
- five tool package versions in EventStore, FrontComposer, and Parties tool manifests.

They remain local only because the consuming mechanisms cannot import CPM versions. Validation must inventory
them, document ownership, and verify required family alignment. New exceptions require an architecture decision;
they cannot use `PackageReference Version` or local `PackageVersion` as a workaround.

## 7. Implementation Sequence and Success Criteria

### Sequence

1. Update `Hexalith.Builds` catalog with Section 6.2 and validate all existing entries.
2. Add or extend Builds-owned exclusive-authority validation, fixtures, CI, README, and sample wrapper.
3. Verify the Builds repository independently.
4. Migrate each consumer in its owning repository, update its Builds reference when applicable, and fix
   compatibility against the approved canonical versions.
5. Add the missing root-declared Builds dependency to Timesheets.
6. Verify each consumer independently with its narrowest relevant restore, build, and test lanes.
7. Update superproject submodule references only after owning repositories pass.
8. Run the superproject restore/build, architecture tests, and affected focused suites.
9. Record SDK/tool exceptions and the final zero-local-definition scan as release-readiness evidence.

No nested submodule initialization, recursive update, dependency update beyond the approved matrix, commit,
push, or release occurs implicitly.

### Success criteria

- The shared catalog contains all required package IDs with the approved canonical versions.
- All 12 .NET consumer roots import the shared catalog through version-free wrappers.
- No dependency version is declared locally through `PackageVersion`, dependency-version properties,
  `PackageReference Version`, nested `Version`, or `VersionOverride`.
- Builds-owned validation and fixtures pass.
- Each affected repository passes its relevant restore, build, and focused test lanes.
- The complete superproject passes relevant integration and architecture lanes.
- All NuGet SDK/tool exceptions are inventoried, justified, and alignment-checked.

## 8. Handoff and Routing

| Recipient | Responsibility |
| --- | --- |
| Product Owner / Architect | Apply the approved Epic 1 story and architecture invariant; preserve historical story evidence. |
| Hexalith.Builds maintainer | Own catalog entries, resolution policy, validators, fixtures, reusable CI, documentation, and sample wrapper. |
| Consumer repository maintainers | Remove local versions, update Builds references and wrappers, remediate compatibility, and verify their owning repositories. |
| Test Architect | Verify exclusive ownership, conflict resolution, restore/build evidence, focused tests, and SDK/tool exceptions. |
| Superproject maintainer | Integrate verified consumer references and run the umbrella validation lanes. |

**Scope routing:** Moderate. Route jointly to Product Owner/Architect and Developer/Build maintainers. No Product
Manager or UX escalation is required because product scope and experience remain unchanged.

## 9. Approval

Incremental Proposals 1-4 were approved by Jerome on 2026-07-18.

**Final approval status:** Approved by Jerome on 2026-07-18.

The approved Story 1.1e, architecture invariant, and sprint-status entry have been applied. Package catalogs,
consumer wrappers, submodule declarations, and compatibility code have not been changed by Correct Course;
those implementation tasks are routed to the Product Owner / Developer handoff described in Section 8.

## 10. Workflow Execution Log

| Event | Result |
| --- | --- |
| Change analysis | Completed against the PRD, 112-story epic plan, architecture, complete binding UX package, sprint status, project contexts, and checked-out repository inventory. |
| Incremental review | Proposals 1-4 approved by Jerome. |
| Final approval | Approved by Jerome on 2026-07-18. |
| Planning updates | Story 1.1e added to `epics.md`; the architecture invariant was strengthened; sprint status records Story 1.1e as `backlog`. |
| Scope classification | Moderate. |
| Handoff | Routed to Product Owner / Architect and Developer / Hexalith.Builds maintainers. |
| Package implementation | Not started by this workflow. |
