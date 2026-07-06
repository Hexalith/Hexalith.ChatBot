---
title: Sprint Change Proposal - Move Root Submodules Under references
date: 2026-07-06
project: Hexalith.ChatBot
trigger: "Repository submodules must live under references/"
mode: batch
status: implemented
scope: minor
---

# Sprint Change Proposal - Move Root Submodules Under `references/`

## 1. Issue Summary

The repository had root-declared Hexalith submodules checked out directly under the repository root, for example `Hexalith.EventStore`, `Hexalith.FrontComposer`, and `Hexalith.AI.Tools`.

Current repository instructions require root-declared submodules to live under `references/` and prohibit nested or recursive submodule initialization. The old layout created a mismatch between `.gitmodules`, agent instructions, planning artifacts, solution paths, and MSBuild root detection.

Evidence:

- `.gitmodules` declared 13 submodule paths at the root.
- `Directory.Build.props` detected sibling modules at root paths such as `Hexalith.EventStore`.
- `Hexalith.ChatBot.slnx` referenced sibling projects at root paths.
- `AGENTS.md`, `CLAUDE.md`, and Copilot instructions pointed at `Hexalith.AI.Tools/hexalith-llm-instructions.md`.
- Planning artifacts still described EventStore and FrontComposer as root-level submodules.

## 2. Impact Analysis

### Epic Impact

Epic 1 Story 1.1b was directly affected because it defined the submodule dependency policy. It now needs to describe EventStore and sibling dependencies as root-declared submodules under `references/`, not as root-level working tree directories.

No new epic is required. No epic sequencing changes are required.

### Story Impact

Affected story text:

- Story 1.1b: submodule and sibling dependency resolution.
- Story 1.1 / scaffold references that mention "EventStore root submodule".
- Epic 10 FrontComposer dependency wording, because the UI consumes FrontComposer from the moved submodule path.

### Artifact Conflicts

- PRD: no product requirement conflict. The PRD names sibling bounded contexts, but not their filesystem placement.
- Epics: update required for submodule policy wording and path assumptions.
- Architecture: update required for source-context paths, submodule policy, scaffold sequence, and file tree.
- UX: no UI or interaction impact.
- Repository config: update required for `.gitmodules`, `.slnx`, MSBuild root properties, project references, CI/setup docs, and path-pinning tests.

### Technical Impact

The change is structural. It moves gitlinks only; it does not change submodule commits or edit submodule contents.

Build and restore path resolution must use:

- `references/Hexalith.EventStore`
- `references/Hexalith.Tenants`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Projects`
- `references/Hexalith.Parties`
- `references/Hexalith.Folders`
- `references/Hexalith.Conversations`

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The requested change is a repository structure correction, not a product scope change.
- The policy already exists in current Hexalith instructions.
- No rollback is needed because no completed product behavior depends on root-level submodule directories.
- MVP scope remains unchanged.

Effort: Low to medium.

Risk: Medium, because solution and project paths must all move together and restore/build can expose existing sibling package issues.

## 4. Detailed Change Proposals

### Git Metadata

OLD:

```text
path = Hexalith.EventStore
path = Hexalith.FrontComposer
path = Hexalith.AI.Tools
```

NEW:

```text
path = references/Hexalith.EventStore
path = references/Hexalith.FrontComposer
path = references/Hexalith.AI.Tools
```

Rationale: Align root `.gitmodules` with the required `references/` layout.

### Build Configuration

OLD:

```xml
<HexalithEventStoreRoot ...>$(MSBuildThisFileDirectory)Hexalith.EventStore</HexalithEventStoreRoot>
```

NEW:

```xml
<HexalithEventStoreRoot ...>$(MSBuildThisFileDirectory)references\Hexalith.EventStore</HexalithEventStoreRoot>
```

Rationale: Resolve sibling project references from the new root-declared submodule location.

### Project References

OLD:

```xml
<ProjectReference Include="..\..\Hexalith.FrontComposer\src\Hexalith.FrontComposer.Shell\Hexalith.FrontComposer.Shell.csproj" />
<ProjectReference Include="..\..\Hexalith.EventStore\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj" />
```

NEW:

```xml
<ProjectReference Include="$(HexalithFrontComposerRoot)\src\Hexalith.FrontComposer.Shell\Hexalith.FrontComposer.Shell.csproj" />
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj" />
```

Rationale: Keep source references centralized and resilient to future path policy changes.

### Setup Instructions

OLD:

```markdown
[`Hexalith.AI.Tools/hexalith-llm-instructions.md`](./Hexalith.AI.Tools/hexalith-llm-instructions.md)
```

NEW:

```markdown
[`references/Hexalith.AI.Tools/hexalith-llm-instructions.md`](./references/Hexalith.AI.Tools/hexalith-llm-instructions.md)
```

Rationale: Agent entry points must remain valid after the submodule move.

### Planning Artifacts

OLD:

```markdown
Hexalith.EventStore added as a root-level git submodule
```

NEW:

```markdown
Hexalith.EventStore added as a root-declared git submodule under `references/Hexalith.EventStore`
```

Rationale: Keep story and architecture guidance aligned with the implemented repository layout.

## 5. Implementation Handoff

Scope classification: Minor.

Route: Developer agent direct implementation.

Completed implementation tasks:

- Moved all 13 root-declared submodules into `references/`.
- Updated `.gitmodules` paths.
- Updated root instruction entry points and setup wording.
- Updated `Directory.Build.props` root detection.
- Updated `.slnx` sibling project paths.
- Updated direct project references to use `$(Hexalith*Root)` properties.
- Updated architecture tests and E2E helper paths.
- Updated relevant ADR and planning references.

Success criteria:

- `git submodule status` reports all root-declared submodules under `references/`.
- No root-level `Hexalith.*` submodule working directories remain.
- Root-owned build/test/source files no longer reference old root-level submodule paths.
- `git diff --check` is clean.

Validation notes:

- `git diff --check` passed.
- `git submodule status` reports all 13 submodules under `references/`.
- `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` reached the moved paths but failed on existing sibling package metadata warnings treated as errors:
  - `NU1506` duplicate `PackageVersion` items in Conversations, Parties, and Projects.
- `dotnet restore src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj -m:1 /nr:false` reached `references/Hexalith.FrontComposer` but failed on an existing Fluent UI package downgrade:
  - `NU1605` `Microsoft.FluentUI.AspNetCore.Components` rc.4 from FrontComposer versus rc.3 in ChatBot.

No submodule contents were modified.

## Checklist Summary

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Trigger story | N/A | Direct repository correction request, not caused by a specific story failure. |
| 1.2 Core problem | Done | Submodule location conflicted with current repository policy. |
| 1.3 Evidence | Done | `.gitmodules`, build config, solution paths, docs, and planning text used root-level paths. |
| 2.1 Current epic | Done | Epic 1 remains valid after wording/path correction. |
| 2.2 Epic changes | Done | No new epic. Story 1.1b wording updated. |
| 2.3 Future epics | Done | No sequencing changes. Epic 10 dependency wording updated. |
| 2.4 Invalidated epics | N/A | No epic invalidated. |
| 2.5 Priority changes | N/A | No priority change. |
| 3.1 PRD conflict | N/A | No PRD scope conflict. |
| 3.2 Architecture conflict | Done | Submodule policy and file tree updated. |
| 3.3 UX conflict | N/A | No UX impact. |
| 3.4 Other artifacts | Done | `.gitmodules`, setup docs, build config, solution paths, tests, ADRs updated. |
| 4.1 Direct adjustment | Viable | Implemented. |
| 4.2 Rollback | Not viable | Rollback not needed. |
| 4.3 MVP review | Not viable | MVP unchanged. |
| 4.4 Recommended path | Done | Direct Adjustment. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Path rationale | Done | Included above. |
| 5.4 MVP impact | Done | No MVP scope impact. |
| 5.5 Handoff | Done | Developer implementation completed. |
| 6.1 Checklist review | Done | All applicable sections addressed. |
| 6.2 Proposal accuracy | Done | Proposal reflects implemented changes and validation. |
| 6.3 User approval | Done | User explicitly requested `$bmad-correct-course move submodules to /references/`. |
| 6.4 Sprint status | N/A | No epic/story status changes required. |
| 6.5 Next steps | Done | Resolve unrelated sibling restore blockers before treating solution restore as green. |
