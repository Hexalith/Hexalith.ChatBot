---
baseline_commit: 82a0a36
---

# Story 12.9: Cross-surface a11y / visual re-verification

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a quality owner,
I want the Fluent-migrated ChatBot UI surfaces re-verified for accessibility, visual conformance, localization, and parity,
so that Epic 12 closes with proof that the component migration did not regress the governed user experience or non-UI adapters.

## Acceptance Criteria

1. **Fluent-migrated surfaces pass the governed accessibility floor.** Given Stories 12.1-12.8 are done, when verification runs, then Project Workspace, Project Conversation, S1 conversation stream, governed composer, S2 association review, S3 approval/governed action surfaces, S8/S10 operational queues/dashboards, S9 audit investigation, policy/notification/escalation editors, tenant policy editor, and streaming stop primitive retain WCAG 2.2 AA-oriented contracts: unique landmarks/region names, keyboard-reachable actions, visible focus, reachable disabled reasons, correct live-region behavior, stable focus after load/send/validation/project switch, no duplicate shell/provider/store ownership, touch targets, and no unauthorized detail leakage. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.9`; `_bmad-output/implementation-artifacts/10-7-cross-surface-a11y-visual-parity-reverification.md`; `Hexalith.AI.Tools/hexalith-ux-instructions.md`]

2. **Light, dark, and forced-colors visual conformance is proved against Fluent v5, not a ChatBot design system.** Given all Epic 12 surfaces now render through Fluent/FrontComposer components and `chatbot.tokens.css` was retired to semantic aliases plus layout CSS, when the visual gate runs, then no raw hex/rgb/hsl product palette, legacy v4/FAST tokens, custom type/radius/font aliases, `.chatbot-button`, or native-control CSS selectors return; non-color status survives through text/icon/border cues; focus indicators and touch targets meet the product floor; and fixtures cover light, dark, forced-colors/high-contrast, reduced-motion, desktop/tablet/phone, and EN/FR text expansion for status chips, badges, banners, composer, conversation items, editors, queue rows, dashboard cards/rows, and audit rows. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/implementation-artifacts/12-8-retire-chatbot-tokens-css-custom-design-system.md`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`]

3. **EN+FR localization parity remains intact after the Fluent migration.** Given the migrated components no longer use custom ChatBot primitive classes, when resource and fixture checks run under English and French cultures, then every `ChatBotUiTextKey` used by migrated Epic 12 surfaces resolves in both resources, French text expansion wraps without hiding critical state/action/recovery words, and stable machine identifiers, reason codes, command names, correlation IDs, task IDs, tenant/project IDs, policy tokens, and data markers remain byte-stable and untranslated. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]

4. **The Fluent-only governance guard is empty and build-blocking.** Given Story 12.8 removed the last CSS primitive backlog, when governance tests and source scans run, then `RawControlMigrationBacklog` is empty, `PrimitiveMigrationBacklog` is empty, raw lowercase `<button>/<input>/<select>/<textarea>` are absent from `src/Hexalith.ChatBot.UI/**/*.razor`, no legacy v4/FAST tokens remain, and no stale allowlist/backlog entry exists. [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`]

5. **Playwright E2E a11y/visual gate is green and non-vacuous.** Given Story 12.8 exposed the risk of inaccurate browser results, when the E2E verification is run, then the full `Hexalith.ChatBot.UI.E2E.Tests` suite executes through the real browser path when Chrome is available and records pass/fail counts honestly; if browser startup is unavailable, every fallback must assert source/fixture contracts and the test summary must state the limitation explicitly. Do not mark Story 12.9 done on string-only fallbacks when Chrome is available. [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`; `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs`]

6. **CLI/MCP parity remains unaffected.** Given Epic 12 is rendering-layer remediation only, when parity checks run, then CLI and MCP command/read adapters still use the shared client/contracts, no CLI/MCP tool gains a UI-only bypass affordance, and no backend, CommandGateway, EventStore, Dapr, projection, package pin, or submodule content changes are required to satisfy UI visual/a11y verification. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/implementation-artifacts/10-7-cross-surface-a11y-visual-parity-reverification.md`]

7. **Release build and default lanes are clean with reproducible evidence.** Given the story completes, when reviewing the Dev Agent Record and test summaries, then they list exact commands, pass/fail/skip counts, Chrome availability, any browser fallback mode, touched files, and any residual caveats. `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` must be clean with TreatWarningsAsErrors, the default/focused test lanes must be green, and `git diff --check` must pass. [Source: `.agents/skills/bmad-create-story/checklist.md`; `_bmad-output/implementation-artifacts/tests/test-summary-story-10.7.md`; `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`]

## Tasks / Subtasks

- [x] Inventory and pin the Epic 12 verification scope (AC: 1, 2, 5)
  - [x] Read the completed Story 12.2-12.8 files and map their migrated surfaces to current source and test coverage.
  - [x] Extend or add a Story 12.9 release-readiness matrix so a future surface removal cannot silently reduce coverage.
  - [x] Include all high-risk surfaces: project workspace/conversation, governed composer, conversation items, association review, approval/governed action, policy/notification/escalation editors, tenant policy editor, operational dashboards, governed operations, compliance audit investigation, streaming stop, and shared badges/chips/status banners.

- [x] Re-run and strengthen accessibility/focus contracts after Fluent migration (AC: 1)
  - [x] Extend `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs` and adjacent contract tests for unique landmarks, focus order, focus return, validation summaries, disabled reason reachability, live-region deduplication, metadata-only off-surface affordances, and no hover-only critical actions.
  - [x] Extend `tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs` for primary and dense touch target coverage across the migrated Fluent buttons and editor controls.
  - [x] Preserve `role`, `aria-*`, ids, `data-chatbot-*`, `data-compliance-*`, and stable machine markers while adjusting any source assertions.

- [x] Prove Fluent visual conformance across modes and viewports (AC: 2, 4)
  - [x] Run/extend `ChatBotFluentConformanceTests`, `ChatBotSemanticTokenContractTests`, and `ChatBotGovernedPrimitiveContractTests` to keep raw controls, theme redefinition, primitive CSS debt, raw colors, legacy v4/FAST tokens, and stale backlog entries at zero.
  - [x] Extend browser fixtures for light, dark, forced-colors, reduced-motion, desktop/tablet/phone, and no horizontal overflow across the migrated surfaces.
  - [x] Use Fluent component parameters and existing semantic data markers for any fix; do not recreate ChatBot-owned typography, button, radius, foreground, or input styling.

- [x] Re-verify EN+FR localization and French expansion (AC: 3)
  - [x] Extend `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs` so every migrated surface key resolves in English and French.
  - [x] Add or extend E2E/browser fixtures for French critical labels on composer, association review, approval, governed operations, operational dashboards, audit investigation, and policy/notification/escalation editors.
  - [x] Assert machine identifiers and reason codes remain invariant under French culture.

- [x] Consolidate the Story 12.9 E2E a11y/visual gate (AC: 1, 2, 5)
  - [x] Extend `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs` or add `Story12FluentReleaseReadinessE2ETests.cs` that composes the existing fixtures without duplicating large HTML unnecessarily.
  - [x] Re-run the full `tests/Hexalith.ChatBot.UI.E2E.Tests` browser-capable suite, not only the Story 12.8 affected classes.
  - [x] If Chrome is available, verify the browser path is actually exercised; do not accept a no-browser fallback as final evidence.
  - [x] Keep fallback helpers non-vacuous by requiring dedicated `WithoutBrowser` source assertions that fail on missing Fluent/ARIA/status/localization/forced-colors contracts.

- [x] Prove CLI/MCP and architecture parity are unchanged (AC: 6)
  - [x] Run conformance, CLI, MCP, and architecture lanes from the compiled xUnit v3 runner if VSTest socket permissions fail.
  - [x] If a real parity gap is discovered, fix it through existing client/contract patterns and broaden tests; otherwise avoid touching non-UI production code.
  - [x] Confirm no dependency on Server, Dapr, EventStore internals, projection stores, gateway stages, or sibling submodule source is added to UI/CLI/MCP.

- [x] Record reproducible evidence (AC: 5, 7)
  - [x] Create `_bmad-output/implementation-artifacts/tests/test-summary-story-12.9.md` with exact commands, pass/fail/skip counts, browser mode, Chrome path/version when available, and any limitations.
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`.
  - [x] Run `DiffEngine_Disabled=true` UI, UI.E2E, Conformance, CLI, MCP, and Architecture test lanes through the compiled xUnit v3 runner if `dotnet test` is blocked by VSTest sockets.
  - [x] Run source scans for raw controls and forbidden CSS primitives.
  - [x] Run `git diff --check`.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, document language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-9-cross-surface-a11y-visual-reverification` was `backlog`; `epic-12` is `in-progress`; Stories 12.1-12.8 are `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; relevant sections are Epic 12 and Story 12.9.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`; relevant sections are Frontend Architecture, ChatBot UI Fluent-only conformance, architectural boundaries, and test/project structure.
- Loaded `ux_content` from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, `EXPERIENCE.md`, review reports, and Epic 10 UX elaboration by targeted discovery. Binding chain: Fluent UI v5 -> FrontComposer -> DESIGN.md -> EXPERIENCE.md.
- Loaded previous story intelligence from `_bmad-output/implementation-artifacts/12-8-retire-chatbot-tokens-css-custom-design-system.md` and `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`.
- Loaded Story 10.7 because Story 12.9 explicitly re-runs that verification against Fluent-migrated surfaces.
- Loaded persistent project-context facts from sibling `**/project-context.md` files. Relevant rules: .NET 10, `.slnx`, central package management, warnings-as-errors, xUnit v3 + Shouldly, `DiffEngine_Disabled=true`, root-level submodule-only policy, no generated-output edits, no casual package upgrades, metadata-only diagnostics, and FrontComposer/Fluent-only UI rules.

### Epic 12 Context

Epic 12 closes the component-level Fluent v5 conformance gap left after Epic 10 adopted the FrontComposer shell while interior surfaces still used raw HTML and a custom CSS token layer. Stories 12.2-12.7 migrated controls/components; Story 12.8 retired `chatbot.tokens.css` primitive debt; Story 12.9 is the final cross-surface verification pass. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`]

This story is verification-first and UI-scoped. The dev agent may add or strengthen tests and may fix discovered UI regressions, but it must not implement unrelated backend behavior, change the command spine, touch EventStore/Dapr topology, alter CLI/MCP tools, upgrade packages, or modify sibling submodule contents. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/epics.md#Epic 12`]

### Current State of Files to Update

- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` currently has an empty `RawControlMigrationBacklog` and an empty `PrimitiveMigrationBacklog`. This is the build-blocking governance proof Story 12.9 must keep green.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs` and `ChatBotGovernedPrimitiveContractTests.cs` now validate semantic Fluent aliases, forced-colors, non-color cues, reduced-motion, and no custom primitive layer.
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`, `ChatBotResponsiveTouchContractTests.cs`, and `ChatBotLocalizationContractTests.cs` hold the main source-level a11y, touch, and EN/FR contracts.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` and `GovernedOperationsVisualFoundationE2ETests.cs` contain the broadest browser fixtures for forced-colors, reduced-motion, phone layout, metadata-only redaction, French expansion, and touch targets.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs` is a release-readiness matrix and anti-vacuous fallback guard for Story 10.7. Extend or mirror it for Story 12.9.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Story12CssRetirementE2ETests.cs` guards retired presentation hooks and forbidden CSS primitives from Story 12.8.
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.9.md` should be created for final evidence.

### Previous Story Intelligence

Story 12.8 completed CSS retirement and then senior review found two real regressions caused by the removal:

- `[hidden]` panels stayed visible because grouped CSS display declarations overrode the user-agent hidden rule. The fix was a global `[hidden] { display: none !important; }` reset.
- Touch targets collapsed below the 44px / 24px minimums after native-control sizing selectors were removed. The fix applied `.chatbot-touch-target-primary` and `.chatbot-touch-target-dense-secondary` utility classes to Fluent buttons/components and fixtures.

Story 12.8 also documented an evidence-quality trap: initial E2E lanes were recorded as "0 failed" despite genuine browser failures. Story 12.9 must require real browser-path evidence when available and must record failures/skips honestly. [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`]

Story 10.7 provides the closest prior pattern. It added a cross-surface matrix, source contract tests, visual/token forced-colors checks, EN+FR expansion checks, CLI/MCP parity assertions, and full UI/E2E/conformance/CLI/MCP/architecture evidence. Story 12.9 should reuse that structure but update the scope to the Fluent-migrated Epic 12 surfaces and the now-empty Fluent guard. [Source: `_bmad-output/implementation-artifacts/10-7-cross-surface-a11y-visual-parity-reverification.md`]

### Git Intelligence

Recent relevant commits show the Epic 12 sequence:

- `82a0a36 feat(story-12.8): Retire ChatBot CSS primitives`
- `17975d9 feat(story-12.7): Migrate operational and audit pages to Fluent`
- `5d618e3 feat(story-12.6): Migrate policy notification escalation editors to Fluent`
- `1a623e9 feat(story-12.5): Migrate approval and governed action surfaces to Fluent`
- `c3232b5 feat(story-12.4): Migrate association review surface to Fluent`
- `6336421 feat(story-12.3): Migrate conversation stream and items to Fluent`

Working-tree note at story creation: pre-existing modified submodule pointers are present for `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Parties`, and `Hexalith.Tenants`, plus `_bmad-output/story-automator/orchestration-10-20260619-173555.md`. Do not revert or include those unrelated changes unless the user explicitly asks.

### Architecture and UX Guardrails

- UI may depend on ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts only. Do not reference Server, gateway internals, Dapr clients, EventStore internals, audit/idempotency/projection internals, CLI, or MCP. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Use FrontComposer or Fluent UI v5 components for UI primitives. Raw lowercase `<button>/<input>/<select>/<textarea>` remain prohibited; raw `<a>` navigation links are allowed. [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Express typography, color, and spacing through Fluent v5 component parameters or Fluent 2 design tokens. Hand-authored CSS must not recreate Fluent-provided button styling, heading ramps, foreground roles, or legacy v4/FAST tokens. [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`]
- Forced-colors and non-color status meaning must survive through icon/text labels/borders, not background fill alone. Do not remove labels/aria/data markers that make status meaning perceivable. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`]
- Keep the governed no-fake-freeform safety model. The composer is a governed write surface over the same CommandGateway/client spine, not a bypass. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Page-like surfaces with two or more sibling titled content sections should use `FluentAccordion`; do not do broad layout refactors unless a verification failure makes a focused fix necessary. [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`]

### Testing Standards

- xUnit v3 + Shouldly; avoid raw `Assert.*`.
- Keep `DiffEngine_Disabled=true` for Verify-backed lanes. Build with `.slnx`; never create/use `.sln`.
- VSTest may fail in this environment due socket permissions; use the compiled xUnit v3 in-process runner or compiled xUnit executable fallback and record the failed VSTest attempt honestly.
- Required verification commands should include:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`
  - `DiffEngine_Disabled=true tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/xunit-inproc-runner-101 tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests.dll -parallel none`
  - `DiffEngine_Disabled=true tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/xunit-inproc-runner-101 tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests.dll -parallel none`
  - equivalent runner invocations for `Hexalith.ChatBot.Conformance.Tests`, `Hexalith.ChatBot.Cli.Tests`, `Hexalith.ChatBot.Mcp.Tests`, and `Hexalith.ChatBot.Architecture.Tests`
  - `rg -n "<(button|input|select|textarea)(\\s|/|>)" src/Hexalith.ChatBot.UI/Components`
  - `rg -n -- "--chatbot-type-|--chatbot-font-|--chatbot-radius-|\\.chatbot-button|\\b(button|input|select|textarea)([:.#\\s,{>+~\\[]|$)" src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
  - `git diff --check`

### Latest Technical Information

No external upgrade research is needed for this story because the architecture makes the local pins binding: Fluent UI v5 remains `5.0.0-rc.3-26138.1`, Microsoft.Playwright remains `1.60.0`, xUnit v3 remains `3.2.2`, and bUnit remains `2.7.2`. Do not convert this verification story into a dependency upgrade. [Source: `Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

### Regression Traps to Avoid

- Do not claim browser E2E coverage if the real browser path did not run. Record fallback mode and limitations explicitly.
- Do not weaken or remove browser-unavailable fallback assertions; they must inspect source/fixture contracts and fail on real regressions.
- Do not reintroduce raw interactive controls, custom primitive CSS aliases, or legacy v4/FAST tokens under new names.
- Do not remove `[hidden]` behavior, forced-colors cues, reduced-motion suppression, touch target utilities, disabled reason reachability, validation `role="alert"`/`aria-describedby`, live-region dedup markers, or metadata-only redaction sentinels.
- Do not use text-only scans as a substitute for browser layout checks when Chrome is available.
- Do not modify `Directory.Packages.props`, sibling submodule contents, generated `obj/**/generated/HexalithFrontComposer/**`, backend command/admission code, CLI/MCP adapters, EventStore, Dapr/Aspire topology, or release infrastructure unless a verified Story 12.9 failure directly requires it and tests are broadened.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`]
- [Source: `Hexalith.AI.Tools/hexalith-ux-instructions.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: ChatBot UI Fluent v5 Component Conformance Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.9: Cross-surface a11y / visual re-verification`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]
- [Source: `_bmad-output/implementation-artifacts/10-7-cross-surface-a11y-visual-parity-reverification.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-10.7.md`]
- [Source: `_bmad-output/implementation-artifacts/12-8-retire-chatbot-tokens-css-custom-design-system.md`]
- [Source: `_bmad-output/implementation-artifacts/tests/test-summary-story-12.8.md`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/Story12CssRetirementE2ETests.cs`]
- [Source: `Directory.Packages.props`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.9.md`
- Chrome binary was found at `/usr/bin/google-chrome` and reported `Google Chrome 148.0.7778.215`. A bare `--headless=new` smoke command fails with `setsockopt: Operation not permitted (1)`, but the `BrowserHarness` launch args (`--single-process --no-zygote --disable-crashpad`) start Chrome successfully, so the full E2E suite ran through the real browser path (134 passed, 0 failed, 0 skipped) including a live compliance audit investigation render. (Code-review correction 2026-06-21: an earlier note claimed a browser-only skip; in this environment the harness browser path is actually exercised and the honest skip fires only when Chrome is genuinely absent.)

### Completion Notes List

- Added Epic 12 migrated-surface source contracts for accessibility, focus, stable ARIA/data markers, touch target hooks, French labels, and culture-invariant machine tokens.
- Added a Story 12.9 release-readiness E2E matrix that pins all Fluent-migrated surfaces, verifies non-vacuous browser-unavailable fallbacks, and ties Fluent-only governance plus CLI/MCP parity proofs to the release gate.
- Re-ran build, UI, E2E, conformance, CLI, MCP, architecture, source scans, and `git diff --check`; all passed. The full UI.E2E suite (134 tests) executed through the real browser path with 0 failures and 0 skips (code-review correction 2026-06-21: the earlier "browser-only skip" was an evidence error — the harness launches Chrome with `--single-process` and the live browser path is exercised here).
- No production code, backend code, CLI/MCP adapter code, package pins, or sibling submodule contents were changed.

### File List

- `_bmad-output/implementation-artifacts/12-9-cross-surface-a11y-visual-reverification.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.9.md`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Story12FluentReleaseReadinessE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotResponsiveTouchContractTests.cs`

### Change Log

- 2026-06-21: Implemented Story 12.9 release-readiness verification, added cross-surface contract coverage, recorded validation evidence, and moved story to review.
- 2026-06-21: Senior Developer Review (AI, Jerome) — auto-fixed the High browser-evidence inaccuracy and Medium File List gap; re-verified all lanes green through the real browser path; moved story to done.

## Senior Developer Review (AI)

**Reviewer:** Jerome — 2026-06-21
**Outcome:** Approve (after auto-fix). All acceptance criteria verified green; no CRITICAL findings remain.

### Independently verified evidence (this environment)

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` — clean, 0 warnings / 0 errors (TreatWarningsAsErrors).
- Compiled xUnit v3 runner: UI 173, Conformance 97, CLI 24, MCP 30, Architecture 63 — all 0 failed / 0 skipped (counts match the summary).
- **UI.E2E 134 total, 0 failed, 0 skipped through the real browser path** (58.6s wall-clock). Forcing `CHROME_EXECUTABLE_PATH=/nonexistent/chrome` produces the honest 1-skip fallback (0.07s), proving the skip branch is non-vacuous and that the default path genuinely launches Chrome.
- Source scans (raw `<button/input/select/textarea>` in Components, forbidden `--chatbot-*`/`.chatbot-button` selectors in `chatbot.tokens.css`) — no matches. `git diff --check` — clean.
- `RawControlMigrationBacklog` and `PrimitiveMigrationBacklog` confirmed empty (AC4); govern guard build-blocking and green.

### Findings and resolutions

- **HIGH (AC5/AC7) — RESOLVED:** The story Debug Log, Completion Notes, and `test-summary-story-12.9.md` recorded the E2E lane as "134 total, 0 failed, 1 skipped" with "the sandbox prevents Chrome from launching." That conclusion came from a bare `--headless=new` smoke command that omits the harness flags. The `BrowserHarness` launches with `--single-process --no-zygote --disable-crashpad`, which start Chrome successfully here, so the suite runs live with 0 skips. Evidence corrected to reflect the real green browser run; AC5's "honest counts / no string-only fallback when Chrome is available" requirement is now satisfied truthfully.
- **MEDIUM (AC7) — RESOLVED:** `_bmad-output/implementation-artifacts/tests/test-summary.md` (aggregate automation summary) was modified for Story 12.9 but missing from the File List; added.
- **LOW — Noted, not changed:** `Story12FluentReleaseReadinessE2ETests.BrowserUnavailableFallbacksShouldRemainNonVacuousOrVisibleSkips` only validates `if (harness is null)` branches; the compliance test's honest `Assert.Skip` lives in an `if (startedHarness is null)` block (terminating in `Assert.Skip`, not `return;`), so it is outside the meta-test's regex. The skip is already honest and the gate is green; reworking the regex was judged net-negative risk for marginal value.
