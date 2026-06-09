---
title: Sprint Change Proposal - Dependency Update to Latest Release Versions
project: Chatbot
date: 2026-06-09
status: approved
mode: Batch
trigger: "Update centrally-managed NuGet packages to latest release version (latest preview only where needed)"
scope_classification: Minor
recommended_approach: Direct Adjustment (dependency version bump, no plan change)
owner: Jerome
prepared_by: Developer (Correct Course workflow)
approved_by: Jerome
implementation_state: applied-and-verified (Release build clean, 0 warnings / 0 errors)
---

# Sprint Change Proposal - Dependency Update to Latest Release Versions

## 1. Issue Summary

**Trigger (Jerome, 2026-06-09):** Update the project's centrally-managed NuGet packages to their latest **release** versions, falling back to the latest **preview** only where a stable release does not exist or is otherwise needed for lockstep compatibility.

**Issue type:** Routine dependency maintenance / version hygiene — **not** an implementation defect, scope change, or plan correction. No epic, story, PRD, architecture, or UX artifact is affected.

**How it was assessed:** A live query of nuget.org (flat-container index) was run for all 56 packages declared in `Directory.Packages.props`, comparing each pinned version against the latest stable and latest prerelease available on 2026-06-09. The AppHost project's hardcoded `Aspire.AppHost.Sdk` version was also checked because it is **not** centrally managed and must move in lockstep with `Aspire.Hosting`.

**Context:** This follows the 2026-06-09 Aspire bump to 13.4.2 (which resolved the DCP 0.24.3 startup break recorded in the Tier-3 live-DAPR notes). Aspire **13.4.3** is now the latest stable on the same minor line — a low-risk patch follow-up.

## 2. Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | No failing story. Trigger is proactive dependency maintenance. |
| 1.2 Core problem | [x] Done | Several packages have newer releases on the same major/minor line; two preview-pinned Aspire integrations have a newer preview aligning with the 13.4.3 line. |
| 1.3 Evidence | [x] Done | Live nuget.org query (56 packages); `Directory.Packages.props`; `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj:1`. |
| 2.1 Current epic impact | [x] Done | None. No epic reopened; no story status changes. |
| 2.2 Epic-level changes | [N/A] | None. |
| 2.3 Remaining epics | [N/A] | None. |
| 2.4 Future epic validity | [x] Done | No epic affected by the version bump. |
| 2.5 Priority/order | [N/A] | None. |
| 3.1 PRD conflicts | [N/A] | None. |
| 3.2 Architecture conflicts | [x] Done | None. Stack versions in `architecture.md` reference major lines (net10, Aspire 13.4.x, FluentUI v5) that remain accurate. No ADR needed for a patch bump. |
| 3.3 UI/UX conflicts | [N/A] | None. FluentUI stays on `5.0.0-rc.3-26138.1` (no change). |
| 3.4 Other artifacts | [x] Done | `Directory.Packages.props` (8 lines) + AppHost `.csproj` SDK attribute (1 line). No test or sprint-status edits. |
| 4.1 Direct Adjustment | [x] Viable | Edit pinned versions only. Effort Low; risk Low. |
| 4.2 Rollback | [N/A] | Nothing to roll back. |
| 4.3 MVP Review | [N/A] | No scope change. |
| 4.4 Recommendation | [x] Done | Direct Adjustment — version bump within existing plan. |
| 5.x Proposal components | [x] Done | This document. |
| 6.1-6.2 Consistency | [x] Done | No conflict with epics, architecture, PRD, UX. |
| 6.3 User approval | [!] Action-needed | Pending Jerome approval. |
| 6.4 Sprint-status update | [N/A] | No sprint-status change. |
| 6.5 Handoff | [x] Done | Minor scope → Developer implements directly. |

## 3. Impact Analysis

- **Epic / Story impact:** None. No epic reopened, no story status changed, no sprint-status edit.
- **Artifact conflicts:** None. PRD, epics, architecture, and UX reference major lines that are unchanged.
- **Technical impact:** Two files change — `Directory.Packages.props` (8 `PackageVersion` lines) and `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` (the `Aspire.AppHost.Sdk` version in the `<Project Sdk="...">` attribute, which is **not** centrally managed and must track `Aspire.Hosting`).
- **Decision policy applied:** *latest release by default; preview only where needed.* The two `Aspire.Hosting.Keycloak`/`.Kubernetes` integrations ship **preview-only** (no stable exists), so they move to the newest preview to stay aligned with the 13.4.3 line.

### Packages intentionally NOT changed (already at latest *release*; "latest any" is a higher prerelease we do not chase)

| Package(s) | Pinned (= latest release) | Latest prerelease (skipped) | Reason |
| --- | --- | --- | --- |
| `Dapr.*` (Client, AspNetCore, Actors, Actors.AspNetCore) | 1.17.9 | 1.18.0-rc02 | 1.17.9 is latest stable; 1.18 is RC. No need. |
| `Microsoft.Extensions.*` (8 pkgs) | 10.0.8 | 11.0.0-preview.4 | 11.x targets the next .NET; project is net10. |
| `Microsoft.AspNetCore.*` (JwtBearer, OpenApi, TestHost, Mvc.Testing) | 10.0.8 | 11.0.0-preview.4 | Same as above. |
| `System.CommandLine` | 2.0.8 | 3.0.0-preview.4 | Latest stable retained. |
| `Newtonsoft.Json` | 13.0.4 | 13.0.5-beta1 | Beta not needed. |
| `xunit.v3` / `.assert` | 3.2.2 | 4.0.0-pre.81 | Major prerelease; stay on stable. |
| `xunit.runner.visualstudio` | 3.1.5 | 4.0.0-pre.4 | Same. |
| `NSubstitute` | 5.3.0 | 6.0.0-rc.1 | Major RC; stay on stable. |
| `bunit` | 2.7.2 | 2.8.4-preview | Preview not needed. |
| `CommunityToolkit.Aspire.Hosting.Dapr` | 13.0.0 | 13.4.0-preview | **Decision: keep stable 13.0.0** (built fine against Aspire 13.4.2; honors release policy). |
| `Microsoft.FluentUI.AspNetCore.Components` | 5.0.0-rc.3-26138.1 | (none newer) | Latest/only 5.x; latest *stable* (4.14.2) is a deliberate major downgrade — keep rc.3. |
| `OpenTelemetry.*`, `FluentValidation*`, `MediatR`, `ModelContextProtocol`*, `Microsoft.Extensions.Http.Resilience`/`ServiceDiscovery`, `Microsoft.FluentUI...Icons`, `Fluxor*`, `ByteAether.Ulid`, `NSwag.MSBuild`, `coverlet.collector`, `Microsoft.NET.Test.Sdk`, `Shouldly`, `Testcontainers`, `YamlDotNet`, `Microsoft.Playwright`, `NetArchTest.eNhancedEdition` | latest stable | — | Already at latest release. (*ModelContextProtocol is bumped — see §5.) |

## 4. Recommended Approach

**Selected: Direct Adjustment.** Edit the pinned versions in place; no plan, epic, or artifact change.

**Scope classification: Minor.** Patch-level bumps on existing major/minor lines + a preview→preview alignment on integrations that have no stable release. No API surface changes expected.

**Effort: Low. Risk: Low.** The only non-trivial coupling is `Aspire.AppHost.Sdk` ↔ `Aspire.Hosting` — both move to 13.4.3 together. Verification is a Release build (`TreatWarningsAsErrors=true`) plus restore.

## 5. Detailed Change Proposals

### 5.1 `Directory.Packages.props` — Aspire line 13.4.2 → 13.4.3 (stable)

```diff
- <PackageVersion Include="Aspire.Hosting" Version="13.4.2" />
- <PackageVersion Include="Aspire.Hosting.Azure.AppContainers" Version="13.4.2" />
- <PackageVersion Include="Aspire.Hosting.Docker" Version="13.4.2" />
+ <PackageVersion Include="Aspire.Hosting" Version="13.4.3" />
+ <PackageVersion Include="Aspire.Hosting.Azure.AppContainers" Version="13.4.3" />
+ <PackageVersion Include="Aspire.Hosting.Docker" Version="13.4.3" />
- <PackageVersion Include="Aspire.Hosting.Redis" Version="13.4.2" />
- <PackageVersion Include="Aspire.Hosting.Testing" Version="13.4.2" />
+ <PackageVersion Include="Aspire.Hosting.Redis" Version="13.4.3" />
+ <PackageVersion Include="Aspire.Hosting.Testing" Version="13.4.3" />
```

### 5.2 `Directory.Packages.props` — Aspire preview integrations → 13.4.3-preview (no stable exists)

```diff
- <PackageVersion Include="Aspire.Hosting.Keycloak" Version="13.4.0-preview.1.26281.18" />
- <PackageVersion Include="Aspire.Hosting.Kubernetes" Version="13.4.0-preview.1.26281.18" />
+ <PackageVersion Include="Aspire.Hosting.Keycloak" Version="13.4.3-preview.1.26305.13" />
+ <PackageVersion Include="Aspire.Hosting.Kubernetes" Version="13.4.3-preview.1.26305.13" />
```

### 5.3 `Directory.Packages.props` — ModelContextProtocol 1.3.0 → 1.4.0 (stable)

```diff
- <PackageVersion Include="ModelContextProtocol" Version="1.3.0" />
+ <PackageVersion Include="ModelContextProtocol" Version="1.4.0" />
```

### 5.4 `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` — AppHost SDK lockstep

```diff
- <Project Sdk="Aspire.AppHost.Sdk/13.4.2">
+ <Project Sdk="Aspire.AppHost.Sdk/13.4.3">
```

**Rationale (all):** Latest patch on the already-adopted 13.4.x Aspire line; `ModelContextProtocol` 1.4.0 is the latest stable minor; Keycloak/Kubernetes have no stable release so they track the newest preview matching the 13.4.3 build. The AppHost SDK must equal `Aspire.Hosting` to avoid a host/SDK version split.

### Decisions confirmed by Jerome (2026-06-09)

- `CommunityToolkit.Aspire.Hosting.Dapr` → **keep 13.0.0** (latest stable; not the 13.4.0-preview).
- `Dapr.*` → **keep 1.17.9** (latest stable; not 1.18.0-rc02).
- All other "latest-any-is-preview" packages → **keep stable**.

## 6. Implementation Handoff

**Scope classification: Minor → Developer implements directly.**

| Recipient | Responsibility |
| --- | --- |
| Developer (this session) | Apply the 9 edits across the 2 files (§5), then run a Release build to verify restore + compile are clean. |

**Success criteria:**

- `Directory.Packages.props` shows Aspire 13.4.3 (5 stable pkgs), Keycloak/Kubernetes 13.4.3-preview, ModelContextProtocol 1.4.0.
- AppHost `.csproj` `Sdk` attribute reads `Aspire.AppHost.Sdk/13.4.3`.
- `dotnet restore` resolves all versions; `dotnet build -c Release` is clean under `TreatWarningsAsErrors=true`.
- No epic/story/PRD/architecture/UX/sprint-status edits (Minor scope confirmed).

## 7. Approval and Routing

Approval status: **approved by Jerome (2026-06-09).**

**Implementation result:** All 9 edits applied across `Directory.Packages.props` (8 lines) and `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` (SDK attribute). Verified:

- `dotnet restore Hexalith.ChatBot.slnx` — all 47 projects resolved, including `Aspire.AppHost.Sdk/13.4.3` and the 13.4.3-preview Aspire integrations.
- `dotnet build Hexalith.ChatBot.slnx -c Release --no-restore` — **Build succeeded: 0 Warning(s), 0 Error(s)** (under `TreatWarningsAsErrors=true`).

No epic/story/PRD/architecture/UX/sprint-status edits were required (Minor scope confirmed).
