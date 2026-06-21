---
baseline_commit: 245075e
---

# Story 12.1: Fluent-only + no-theme-redefinition governance guard

Status: ready-for-dev

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a frontend engineer,
I want a build-blocking guard that bans raw interactive HTML controls and Fluent-primitive-recreating CSS in `Hexalith.ChatBot.UI`,
so that the Fluent v5 conformance gap is enforced and migration progress is measurable.

## Acceptance Criteria

1. **Raw interactive controls are build-blocked with a non-vacuous scan.** Given the UI project, when the Governance test lane runs, then `ChatBotFluentConformanceTests` scans `src/Hexalith.ChatBot.UI/**/*.razor`, asserts the scan found `.razor` files, and fails on any raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` tag outside the temporary Epic 12 migration backlog. Raw `<a>` navigation links are allowed. The guard mirrors the regex and test style of `Hexalith.FrontComposer` `FluentConformanceTests`, `Hexalith.Tenants.UI` `DomainUiFluentConformanceTests`, and `Hexalith.EventStore` `AdminUiFluentConformanceTests`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.1`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`; `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`; `Hexalith.EventStore/tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs`]

2. **The raw-control migration backlog is seeded exactly and can only shrink.** Given the current divergence, when the guard ships, then the temporary raw-control backlog is seeded with exactly these 12 relative `.razor` files and no others: `Components/Governed/ChatBotActorBadge.razor`, `Components/Governed/ChatBotApprovalConversationItem.razor`, `Components/Governed/ChatBotAssociationCandidateRow.razor`, `Components/Governed/ChatBotAssociationReviewActions.razor`, `Components/Governed/ChatBotEscalationPolicyEditor.razor`, `Components/Governed/ChatBotEvidenceChip.razor`, `Components/Governed/ChatBotGovernedComposer.razor`, `Components/Governed/ChatBotNotificationRoutingEditor.razor`, `Components/Governed/ChatBotTaskIntentReviewPanel.razor`, `Components/Governed/ChatBotTenantPolicyEditor.razor`, `Components/Governed/ChatBotWhyProjectPanel.razor`, and `Components/Pages/ComplianceAuditInvestigation.razor`. A stale-entry assertion fails if any backlog file no longer contains a raw interactive control, forcing later migration stories to delete its entry. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.1`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md#Section 4`; source scan of `src/Hexalith.ChatBot.UI/**/*.razor` on 2026-06-21]

3. **No-theme-redefinition governance is enforced without blocking the pre-migration baseline.** Given the no-theme-redefinition rule, when the CSS/style guard runs, then it scans `src/Hexalith.ChatBot.UI/**/*.css` and `.razor` style content, asserts files were found, and fails on new uses of legacy Fluent v4/FAST tokens (`--type-ramp-*`, `--neutral-*`, `--accent-*`, `--palette-*`, `--design-unit`) and on new hand-authored recreations of Fluent-provided primitives such as button styling, heading type ramps (`font-size`/`font-weight`/`line-height`), foreground roles (`color:`), and custom ChatBot primitive aliases (`--chatbot-type-*`, `.chatbot-button`). The current `wwwroot/css/chatbot.tokens.css` primitive backlog is tracked as temporary migration debt with stale-entry assertions and must be emptied by Story 12.8; it is not a permanent carve-out. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`; `_bmad-output/planning-artifacts/epics.md#UX-DR2`; `_bmad-output/planning-artifacts/epics.md#Story 12.8`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`; `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`]

4. **The guard is build-blocking and discoverable in the existing test lane.** Given the ChatBot test layout, when the implementation completes, then `ChatBotFluentConformanceTests` lives in `tests/Hexalith.ChatBot.UI.Tests` with `[Trait("Category", "Governance")]`, uses xUnit v3 and Shouldly, needs no package-version changes, excludes `bin/` and `obj/`, and can run through the existing UI test project and category-filtered Governance lane. [Source: `tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj`; `Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `Hexalith.FrontComposer/_bmad-output/project-context.md#Testing Rules`]

5. **Story scope does not perform component migration.** Given Stories 12.2-12.8 own the actual Fluent component substitutions and CSS retirement, when Story 12.1 is implemented, then it adds governance only: no mass conversion from raw controls to `FluentButton`/`FluentTextArea`/`FluentSelect`, no snapshot churn from visual rewrites, no package upgrades, no backend/CommandGateway/CLI/MCP changes, and no edits inside `Hexalith.FrontComposer` or other sibling submodules. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md#Section 3`; `Hexalith.AI.Tools/hexalith-llm-instructions.md`; `Directory.Packages.props`]

## Tasks / Subtasks

- [ ] Add the ChatBot Fluent conformance guard (AC: 1, 2, 4)
  - [ ] Create `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` or `tests/Hexalith.ChatBot.UI.Tests/Governance/ChatBotFluentConformanceTests.cs` following local naming and namespace conventions.
  - [ ] Implement a case-sensitive regex equivalent to `"<(button|input|select|textarea)(\\s|/|>)"` so PascalCase Fluent components are not matched.
  - [ ] Locate the repository root by walking up to `Hexalith.ChatBot.slnx`, then scan `src/Hexalith.ChatBot.UI` recursively for `.razor` files, excluding `bin/`, `obj/`, hidden files, and reparse points.
  - [ ] Assert the scan is non-vacuous before evaluating offenders.
  - [ ] Report offenders by relative path plus distinct tag names so a migration story can remove the right backlog entry.

- [ ] Seed and ratchet the raw-control backlog exactly (AC: 2)
  - [ ] Add exactly the 12 backlog entries listed in AC2 using relative paths from `src/Hexalith.ChatBot.UI`.
  - [ ] Fail if any file outside that backlog contains a raw `<button>`, `<input>`, `<select>`, or `<textarea>`.
  - [ ] Fail if any backlog entry is stale because the file no longer contains a raw interactive control.
  - [ ] Fail if a backlog path does not exist, so renamed/deleted files cannot silently keep the list green.
  - [ ] Do not add permanent carve-outs; this is temporary migration debt and must only shrink.

- [ ] Add no-theme-redefinition/style governance (AC: 3, 4)
  - [ ] Reuse the FrontComposer `LegacyFluentToken` approach for legacy v4/FAST tokens and scan `.css` plus `.razor` source.
  - [ ] Add a primitive-recreation detector for new ChatBot-owned component primitives, at minimum `.chatbot-button`, `--chatbot-type-*`, source-authored heading ramp declarations (`font-size`, `font-weight`, `line-height`) used as a component primitive, source-authored `color:` foreground-role declarations, and native-control CSS selectors (`button`, `input`, `select`, `textarea`).
  - [ ] Track the existing `wwwroot/css/chatbot.tokens.css` primitive backlog as temporary debt with stale-entry assertions. Keep this backlog narrow enough that adding a second file or a new primitive declaration fails.
  - [ ] Ensure Story 12.8 can make the style backlog empty by deleting `.chatbot-button`, `--chatbot-type-*`, Fluent-provided radii/weights/type ramp aliases, and related primitive declarations.

- [ ] Keep implementation scope narrow (AC: 4, 5)
  - [ ] Do not change `Directory.Packages.props`, Fluent UI Blazor version, Fluxor, xUnit, Shouldly, or bUnit versions.
  - [ ] Do not migrate raw controls in this story; leave visual/component migration to Stories 12.2-12.7 and CSS retirement to 12.8.
  - [ ] Do not edit `Hexalith.FrontComposer`, `Hexalith.Tenants`, `Hexalith.EventStore`, or nested submodules.
  - [ ] Do not modify backend command, query, projection, CLI, MCP, or SignalR behavior.

- [ ] Verify and document the guard (AC: all)
  - [ ] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` or restore first if needed.
  - [ ] Run `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`.
  - [ ] If VSTest socket permissions fail in the sandbox, build the test project and run the compiled xUnit v3 executable test host from `bin/<Configuration>/net10.0`, recording the limitation honestly.
  - [ ] Run `git diff --check`.
  - [ ] Add or update a focused test summary under `_bmad-output/implementation-artifacts/tests/` with exact commands and pass/fail counts if that is the current story evidence convention.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, output language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-1-fluent-only-governance-guard` was `backlog`; `epic-12` was `backlog`. Per workflow, story creation advances Epic 12 to `in-progress` and Story 12.1 to `ready-for-dev`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 12 and Stories 12.1-12.9 are present as an approved remediation epic.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`; relevant section is `Frontend Architecture`, especially ChatBot UI Fluent-only conformance.
- No first-level `*prd*.md` or `*ux*.md` matched the create-story discovery table. A deeper UX search found `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/*.md`; relevant sources are `DESIGN.md`, `EXPERIENCE.md`, `epic10-chat-surface-elaboration.md`, `review-accessibility.md`, and `validation-report.md`.
- Loaded persistent project-context facts from eight sibling `**/project-context.md` files. Relevant cross-cutting rules: .NET 10, warnings-as-errors, central package versions, `.slnx`, xUnit v3 + Shouldly, root-level submodules only, no generated-output edits, no payload/secret leakage, and FrontComposer Fluent-only governance.

### Epic 12 Context

Epic 12 was added by `sprint-change-proposal-2026-06-19.md` to close a component-level Fluent v5 gap left after Epic 10. Epic 10 correctly adopted the FrontComposer Shell, but its acceptance criteria allowed the interior UI to remain raw HTML over `chatbot.tokens.css`; Story 10.7 then verified accessibility and visual contracts against that custom layer instead of build-enforcing Fluent component conformance. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md#Section 2`]

The Epic 12 sequencing is binding: Story 12.1 lands the guard first; Stories 12.2-12.7 remove raw-control offenders by surface; Story 12.8 retires `chatbot.tokens.css` to layout-only CSS; Story 12.9 re-runs a11y/visual verification and confirms an empty guard backlog. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`]

### Current Implementation State

The current ChatBot UI project already references Fluent UI and FrontComposer correctly:

- `Directory.Packages.props` pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`.
- `src/Hexalith.ChatBot.UI/Program.cs` wires Fluent UI, FrontComposer quickstart, ChatBot domain registration, and EventStore in the intended order.
- `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor` owns the single `<FrontComposerShell>`.

The gap is inside the page/component markup and CSS:

- Source scan on 2026-06-21 found raw interactive controls in exactly the 12 files listed in AC2.
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` still carries the custom ChatBot type ramp (`--chatbot-type-*`), `.chatbot-button`, radii, weights, font sizing, foreground roles, and component-like primitive styling that Story 12.8 must retire.
- Existing `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs` currently asserts parts of the custom token layer; Story 12.1 should not rewrite that test wholesale, but the new guard must make future primitive growth impossible and set up Story 12.8 to reframe semantic-token tests.

### Existing Guard Patterns to Reuse

Use `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs` as the closest template:

- It uses file-system source scans rather than rendered markup.
- It resolves repo root by walking up to the solution file.
- It excludes build output and asserts non-vacuous file discovery.
- It uses case-sensitive raw-tag regex so `<FluentButton>` is not matched.
- It maintains shrink-only backlogs with stale-entry assertions.
- It carries `[Trait("Category", "Governance")]`.

Also inspect:

- `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` for stricter domain UI guard examples and Shouldly style.
- `Hexalith.EventStore/tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs` for the simple source-scan variant with documented allowlist reporting.

### Architecture and UX Guardrails

- The UI may reference ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts. It must not reference Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, or projection internals. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
- The guard story is rendering-layer governance only. Preserve governed semantics, accessibility labels, non-color status cues, EN+FR localization, focus management, and the no-ungoverned-chat safety model. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]
- UX-DR1 now requires component-level, build-enforced visual inheritance: every ChatBot `.razor` page/component uses FrontComposer or Fluent UI v5 components; raw `<button>/<input>/<select>/<textarea>` fail the build; raw `<a>` links are allowed. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- UX-DR2 now bans hand-authored recreation of Fluent-provided primitives and legacy v4/FAST tokens. Custom CSS is allowed only for layout the design system does not own. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- `DESIGN.md` says the authoritative visual chain is Fluent UI v5 -> FrontComposer -> DESIGN.md -> EXPERIENCE.md; ChatBot narrows product meaning but does not invent a second design system. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Visual Design System`]

### File Structure Requirements

Likely implementation locations:

- New guard: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` or `tests/Hexalith.ChatBot.UI.Tests/Governance/ChatBotFluentConformanceTests.cs`.
- Source scan root: `src/Hexalith.ChatBot.UI`.
- Raw-control backlog entries are relative to `src/Hexalith.ChatBot.UI`.
- CSS/style backlog starts at `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` and must be written so it can shrink to empty in Story 12.8.

Avoid these locations:

- Do not put the guard in `tests/Hexalith.ChatBot.Architecture.Tests` unless there is a strong reason; this is UI-source governance matching existing UI test conventions.
- Do not edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not edit sibling submodules as part of this story.

### Testing Standards

- Use xUnit v3 and Shouldly; avoid raw `Assert.*`.
- Add `[Trait("Category", "Governance")]` so the guard can run as a blocking Governance lane.
- Do not add package versions to `.csproj`; central versions already include xUnit v3 and Shouldly.
- Keep failure messages actionable and metadata-only: relative file paths and tag/property names are okay; do not dump full file contents.
- Run narrow checks first (`UI.Tests` Governance filter), then build and `git diff --check`.

### Latest Technical Information

- The local repo pins are binding for this story. Do not upgrade Fluent UI, xUnit, Shouldly, bUnit, Fluxor, or Playwright while adding this guard.
- NuGet was checked on 2026-06-21 for test-library context: `xunit.v3` `3.2.2` targets .NET 8 and is compatible with computed `net10.0`; NuGet shows newer prerelease `4.0.0-pre.*` builds, but that does not change this story's pin. `Shouldly` `4.3.0` targets .NET 8/.NET Standard 2.0 and also has newer prerelease versions. Keep both local pins unchanged. [Source: `Directory.Packages.props`; `https://www.nuget.org/packages/xunit.v3/3.2.2`; `https://www.nuget.org/packages/Shouldly/4.3.0`]
- The Fluent UI Blazor version is pinned locally to `5.0.0-rc.3-26138.1` and architecture explicitly says no version churn for Epic 12. [Source: `Directory.Packages.props`; `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

### Git Intelligence

Recent commits show Epic 10 completion and retrospective follow-up, including AI-response transport and FrontComposer Shell-related documentation. The relevant lesson for Story 12.1 is that narrative verification without a build-blocking source guard let component-level Fluent conformance drift. This story should convert the lesson into a ratcheting test, not another prose-only check.

Recent commit titles:

- `245075e chore: sync Epic 10 retrospective follow-up`
- `75b07eb feat(story-10.6b): Streaming AI response and Stop/Cancel`
- `23615f8 chore: Update subproject commit reference and lastUpdated timestamp in orchestration document`
- `7059acc chore: Update subproject references and finalize story 10.6b implementation status`
- `e7d22fb feat: Implement ChatBot-owned SignalR hub for project-conversation change notifications`

### Regression Traps to Avoid

- Do not implement Stories 12.2-12.8 early by migrating controls or deleting CSS primitives in this guard story.
- Do not let the allowlist grow. If a developer needs to add a file, that is either a failed regression or a scope change requiring planning approval.
- Do not let a broken path pass. Every scan must assert root exists and at least one matching source file was scanned.
- Do not make the regex case-insensitive; that would flag Fluent component tags and create noisy false positives.
- Do not treat raw `<a>` links as violations; the planning artifacts explicitly allow raw nav links.
- Do not add a "documented carve-out" for ChatBot. Epic 12 says documented carve-outs are none; the backlog is temporary migration debt with stale-entry checks.
- Do not produce test failures that dump large source snippets or sensitive fixture content.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: ChatBot UI Fluent v5 Component Conformance Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.1: Fluent-only + no-theme-redefinition governance guard`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]
- [Source: `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`]
- [Source: `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`]
- [Source: `Hexalith.EventStore/tests/Hexalith.EventStore.Admin.UI.Tests/Governance/AdminUiFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`]
- [Source: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`]
- [Source: `Directory.Packages.props`]
- [Source: `https://www.nuget.org/packages/xunit.v3/3.2.2`]
- [Source: `https://www.nuget.org/packages/Shouldly/4.3.0`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List
