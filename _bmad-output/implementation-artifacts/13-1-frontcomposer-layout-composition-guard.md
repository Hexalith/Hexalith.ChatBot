---
baseline_commit: 21be905
---

# Story 13.1: FrontComposer layout-composition governance guard

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a frontend engineer,
I want a build-blocking guard that bans hand-rolled `.chatbot-*` page chrome and `<dl>` data dumps and requires every routable page to compose through FrontComposer `FcPageLayout` + `FcPageHeader` in `Hexalith.ChatBot.UI`,
so that the page-level FrontComposer composition gap (the shell-overlap bug, the bordered content box, and the monospace data dumps) is enforced and Epic 13 migration progress is measurable and build-gated.

## Context

Epic 12 migrated **leaf controls** to Fluent v5 (genuinely `done`: 0 raw `<button>/<input>/<select>/<textarea>`, ~160 Fluent usages), but **page-level composition was out of its scope**. Pages still hand-roll their chrome with `.chatbot-*` CSS that fights the FrontComposer shell: the page-title band renders **inside** the shell `@Body` and **overlaps** the shell top bar on every route; content sits in a hard 1px-bordered box; and primary data is rendered as monospace `<dl>` dumps. The Epic 12.1 guard (`ChatBotFluentConformanceTests`) only bans raw form controls + legacy v4 tokens — it does **not** check page composition — and Story 12.9 verified hand-authored static fixtures, **not** the live render, so the broken layout was never observed. Epic 13 closes this forward (no Epic 10/12 story is reopened). This story is the **guard-first** step: it lands the layout-composition guard with a shrink-only allowlist seeded to today's offenders, so Stories 13.2–13.8 burn the allowlist down to empty as they migrate. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md`]

This story adds **governance tests only**. It performs **no** page migration, **no** CSS deletion, **no** component substitution, and **no** backend/CLI/MCP change — those belong to Stories 13.2–13.9.

## Acceptance Criteria

1. **A layout-composition guard exists, is build-blocking, and scans non-vacuously.** Given the ChatBot UI, when the Governance test lane runs, then a guard in `tests/Hexalith.ChatBot.UI.Tests` (a new sibling class `ChatBotLayoutCompositionConformanceTests`, or new `[Fact]`s added to the existing `ChatBotFluentConformanceTests` — either is acceptable) carries `[Trait("Category", "Governance")]`, uses xUnit v3 + Shouldly, walks up to `Hexalith.ChatBot.slnx` to locate the repo root, recursively scans `src/Hexalith.ChatBot.UI/**/*.razor` excluding `bin/`/`obj/`/hidden/reparse-point files, and asserts the scan found `.razor` files before evaluating offenders. It needs no `Directory.Packages.props` / package-version changes. It mirrors the regex + ratcheting-backlog style of `Hexalith.Tenants.UI` `DomainUiFluentConformanceTests` and the existing `ChatBotFluentConformanceTests`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 13.1`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture` (lines ~405, ~411); `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`]

2. **Hand-rolled page chrome is banned, seeded exactly to today's offenders, and the allowlist may only shrink.** Given the current divergence, when the guard ships, then it fails on the hand-rolled page-chrome classes and seeds a per-pattern shrink-only allowlist (with both a missing-path assertion and a stale-entry assertion, so renamed/migrated files cannot keep the guard green) seeded to **exactly** these source-scanned offenders and no others. **Path convention (applies to every allowlist/backlog in AC2–AC4):** entries are forward-slash paths relative to `src/Hexalith.ChatBot.UI` (e.g. `Components/Pages/GovernedOperations.razor`, `Components/Governed/ChatBotAiOutcomeConversationItem.razor`); where a grouped name list states a folder prefix once, every bare name in that group inherits it.
   - **`chatbot-page-header`** (the hand-rolled page-title `<header class="chatbot-page-header">` that causes the shell overlap) — **6 files:** `Components/Governed/ChatBotProjectConversationWorkspace.razor`, `Components/Pages/AssociationReview.razor`, `Components/Pages/ComplianceAuditInvestigation.razor`, `Components/Pages/GovernedOperations.razor`, `Components/Pages/OperationalDashboards.razor`, `Components/Pages/ProjectWorkspace.razor`.
   - **`chatbot-page`** content-box wrapper (`<section class="chatbot-page …">`, matched as a **whole class token** — must NOT match `chatbot-page-header` / `chatbot-page-title`) — **6 files:** the same six as above.
   - **`chatbot-command-bar`** (matched in both lowercase `class="…"` and Blazor `Class="…"` attributes) — **4 files:** `Components/Governed/ChatBotAssociationReviewActions.razor`, `Components/Pages/GovernedOperations.razor`, `Components/Pages/OperationalDashboards.razor`, `Components/Pages/ProjectWorkspace.razor`.

   [Source: source scan of `src/Hexalith.ChatBot.UI/**/*.razor` on 2026-06-22; `_bmad-output/planning-artifacts/epics.md#Story 13.1`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md#Section 4.A`]

3. **`<dl>` primary-data dumps are banned and seeded exactly.** Given the monospace data-dump defect, when the guard runs, then it fails on `<dl class="chatbot-definition-list …">` and seeds a shrink-only allowlist (with missing-path + stale-entry assertions) to **exactly** these **25** components and no others: `Components/Governed/ChatBotAiActionPreviewSections.razor`, `ChatBotAiOutcomeConversationItem.razor`, `ChatBotApprovalConversationItem.razor`, `ChatBotApprovalQueuePriorityView.razor`, `ChatBotAssociationEvidenceComparison.razor`, `ChatBotAssociationReviewActions.razor`, `ChatBotAttachmentConversationItem.razor`, `ChatBotConversationItemClassificationBadge.razor`, `ChatBotConversationItemReviewHistory.razor`, `ChatBotConversationItemStatusSummary.razor`, `ChatBotDecisionConversationItem.razor`, `ChatBotEmailConversationItem.razor`, `ChatBotEscalationPolicyEditor.razor`, `ChatBotFailureStateConversationItem.razor`, `ChatBotNotificationRoutingEditor.razor`, `ChatBotParticipantConversationItem.razor`, `ChatBotProjectConversationWorkspace.razor`, `ChatBotTaskIntentReviewPanel.razor`, `ChatBotTenantPolicyEditor.razor`, `ChatBotWhyProjectPanel.razor` (all under `Components/Governed/`); and `Components/Pages/AssociationReview.razor`, `ComplianceAuditInvestigation.razor`, `GovernedOperations.razor`, `OperationalDashboards.razor`, `ProjectWorkspace.razor`. The guard targets the `chatbot-definition-list` class specifically — bare semantic `<dl>`/`<dt>`/`<dd>` (with no `chatbot-definition-list` class) is **not** banned. [Source: source scan of `src/Hexalith.ChatBot.UI/**/*.razor` on 2026-06-22; `_bmad-output/planning-artifacts/epics.md#Story 13.1`; `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` (StructuralHtmlAllowlist keeps `<dl>` as a semantic landmark)]

4. **Every routable page is required to compose through `FcPageLayout` + `FcPageHeader`, with a shrink-only "not-yet-composed" backlog.** Given the page-composition rule, when the guard runs, then for each `.razor` file containing `@page`, it requires the file to contain both `<FcPageLayout` and `<FcPageHeader` (mirroring `DomainUiFluentConformanceTests.DeclaresLayoutMeasure` / `DeclaresFrontComposerHeader`) **unless** the page is in a shrink-only "not-yet-composed" backlog seeded to **exactly** today's **6** `@page` routes (none compose yet): `Components/Pages/AssociationReview.razor`, `ComplianceAuditInvestigation.razor`, `GovernedOperations.razor`, `OperationalDashboards.razor`, `ProjectConversation.razor`, `ProjectWorkspace.razor`. A stale-entry assertion fails when a backlogged page **does** start composing (forcing 13.2 to remove its entry), and a missing-path assertion fails on a renamed/deleted page. A newly added `@page` route that neither composes nor is backlogged fails the guard. [Source: `_bmad-output/planning-artifacts/epics.md#Story 13.1`; `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs#Domain_page_components_declare_frontcomposer_page_layout_modes`; `#Domain_route_pages_declare_frontcomposer_page_headers`; source scan of `@page` files on 2026-06-22]

5. **Detector logic is pinned by unit fixtures (no false positives/negatives).** Given the guard's regexes, when the test class runs, then crafted-fixture `[Fact]`/`[Theory]` tests assert: the `chatbot-page` whole-token matcher flags `class="chatbot-page"` and `class="chatbot-page chatbot-project-workspace"` but does **not** flag `class="chatbot-page-header"` or `class="chatbot-page-title"`; the command-bar matcher flags both `<div class="chatbot-command-bar">` and `<FluentStack Class="chatbot-command-bar …">`; the `chatbot-definition-list` matcher flags `<dl class="chatbot-definition-list …">` but not a bare `<dl>`; and the page-header matcher flags `<header class="chatbot-page-header">` but not other semantic `<header>`s (e.g. `chatbot-project-context-header`, `chatbot-task-intent-review-panel__header`). [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` (existing detector-fixture pattern, lines 215-330); `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` (Theory/InlineData detector pins)]

6. **Scope is governance-only — no migration.** Given Stories 13.2–13.9 own the actual composition substitution and CSS retirement, when Story 13.1 is implemented, then it adds the guard + BMAD tracking/evidence only: no `FcPageLayout`/`FcPageHeader` adoption, no `.chatbot-*` CSS deletion, no `<dl>`→`FluentDataGrid` migration, no snapshot churn, no package upgrades, no backend/CommandGateway/CLI/MCP/SignalR changes, and **no edits inside `Hexalith.FrontComposer`, `Hexalith.Tenants`, `Hexalith.EventStore`, or any other sibling submodule**. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13` (binding sequencing: 13.1 gates 13.2–13.8); `Hexalith.AI.Tools/hexalith-llm-instructions.md`; `Directory.Packages.props`]

## Tasks / Subtasks

- [x] Add the ChatBot layout-composition guard scaffold (AC: 1)
  - [x] Create `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (sibling to `ChatBotFluentConformanceTests.cs`; same namespace `Hexalith.ChatBot.UI.Tests`), or add the new `[Fact]`s to `ChatBotFluentConformanceTests.cs`. Carry `[Trait("Category", "Governance")]`.
  - [x] Reuse the existing helper pattern from `ChatBotFluentConformanceTests`: `RepositoryRoot()` (walk up to `Hexalith.ChatBot.slnx`), `EnumerateFiles(root, "*.razor")` with `bin/`/`obj/`/hidden/reparse-point exclusion, and `RelativePath(root, file)` returning forward-slash relative paths from `src/Hexalith.ChatBot.UI`.
  - [x] Assert `Directory.Exists(uiRoot)` and `razorFiles.ShouldNotBeEmpty()` before evaluating offenders (non-vacuous scan).

- [x] Ban hand-rolled page chrome with exact, shrink-only allowlists (AC: 2, 5)
  - [x] Add a `chatbot-page-header` token matcher; report offenders by relative path; seed the 6-file allowlist; add missing-path + stale-entry assertions.
  - [x] Add a `chatbot-page` **whole-class-token** matcher (boundary regex, e.g. `(?<=[""\s])chatbot-page(?=[""\s])`) so `chatbot-page-header`/`chatbot-page-title` are NOT matched; seed the 6-file allowlist; add missing-path + stale-entry assertions.
  - [x] Add a `chatbot-command-bar` matcher covering both `class="…"` and Blazor `Class="…"` attributes; seed the 4-file allowlist; add missing-path + stale-entry assertions.

- [x] Ban `<dl class="chatbot-definition-list">` primary-data dumps (AC: 3, 5)
  - [x] Add a `chatbot-definition-list` matcher (target the class, not bare `<dl>`); seed the exact 25-file allowlist; add missing-path + stale-entry assertions.

- [x] Require `@page` composition through FcPageLayout + FcPageHeader (AC: 4, 5)
  - [x] For each `.razor` containing `@page`, require both `<FcPageLayout` and `<FcPageHeader` unless the relative path is in the "not-yet-composed" backlog seeded to the exact 6 `@page` routes.
  - [x] Add a stale-entry assertion (a backlogged page that now composes both must be removed) and a missing-path assertion (renamed/deleted page).
  - [x] Ensure a newly added `@page` that neither composes nor is backlogged fails the guard.

- [x] Pin detector logic with crafted fixtures (AC: 5)
  - [x] Add `[Fact]`/`[Theory]` tests proving the whole-token `chatbot-page` boundary, the `class=`/`Class=` command-bar coverage, the `chatbot-definition-list`-vs-bare-`<dl>` distinction, and the page-header-vs-other-`<header>` distinction.

- [x] Verify and document the guard (AC: all)
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` (restore first if needed) → 0 warnings, 0 errors (TreatWarningsAsErrors).
  - [x] Run the Governance lane: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`. (VSTest socket was available in this sandbox, so `dotnet test` ran directly; no compiled-host fallback was needed. 26 total, 0 failed.)
  - [x] Run `git diff --check`.
  - [x] Add `_bmad-output/implementation-artifacts/tests/test-summary-story-13.1.md` with exact commands and pass/fail counts (existing evidence convention).
  - [x] Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: `epic-13` → `in-progress`, `13-1-frontcomposer-layout-composition-guard` → `review` (dev sets `review`; create-story already set `ready-for-dev`), `last_updated`.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, `checklist.md`.
- Loaded config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts `_bmad-output/planning-artifacts`, implementation artifacts `_bmad-output/implementation-artifacts`, English.
- Loaded `_bmad-output/implementation-artifacts/sprint-status.yaml`: `13-1-frontcomposer-layout-composition-guard` was `backlog`; `epic-13` was `backlog`. Story 13.1 is the first story in Epic 13 → epic advances to `in-progress`; story advances to `ready-for-dev`.
- Loaded `epics_content` (`epics.md`): Epic 13 + Stories 13.1–13.9 + amended UX-DR1/DR2 (page-composition + data-presentation conformance).
- Loaded `architecture_content` (`architecture.md`): the Frontend-Architecture "ChatBot UI FrontComposer layout composition" subsection (lines ~405 / ~411) names the extended `ChatBotFluentConformanceTests`, requires `FcPageLayout`+`FcPageHeader`, bans hand-rolled chrome + `<dl>` dumps, references `Hexalith.Tenants.UI`, and states the allowlist must reach **empty** at Epic 13 completion (carve-outs: none).
- Loaded the predecessor guard story `12-1-fluent-only-governance-guard.md` (`done`) and its implementation `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs` — the direct template for this story.
- Loaded the reference guard `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` and FrontComposer primitives `FcPageHeader.razor` / `FcPageLayout.razor`.
- Loaded persistent project-context facts from sibling `**/project-context.md` files: .NET 10, warnings-as-errors, central package versions, `.slnx`, xUnit v3 + Shouldly, root-level submodules only, no generated-output edits.

### Authoritative Offender Inventory (source scan, 2026-06-22)

Counts below are from a fresh `grep` over `src/Hexalith.ChatBot.UI/**/*.razor`. **The source scan is authoritative** — it supersedes the approximate prose counts in `sprint-change-proposal-2026-06-22.md` where they differ (same precedent as Story 12.1, whose backlog was seeded from a dated source scan). **Re-run the scan before seeding and confirm the exact lists; do not copy a count blindly.**

| Banned pattern | Files | Notes / variance from planning prose |
|---|---|---|
| `chatbot-page-header` (page-title header) | **6** | matches planning ("6 headers"). One offender (`ChatBotProjectConversationWorkspace.razor`) lives under `Components/Governed/`, not `Components/Pages/` — scan **all** of `src/Hexalith.ChatBot.UI`, not just `Pages/`. |
| `chatbot-page` content box (whole token) | **6** | **Planning said "2".** Planning counted only standalone `class="chatbot-page"` (GovernedOperations, OperationalDashboards). Four more use it in a multi-class list: `class="chatbot-page chatbot-project-workspace"` etc. (ProjectConversationWorkspace, AssociationReview, ComplianceAuditInvestigation, ProjectWorkspace). A guard matching only the literal `class="chatbot-page"` is bypassable by adding a second class — match the **whole token**. |
| `chatbot-command-bar` | **4** | **Planning said "3".** Planning counted the 3 pages with a top-level command bar; `ChatBotAssociationReviewActions.razor` also applies it via `Class="chatbot-command-bar chatbot-association-actions__bar"` on a `FluentStack`. Match both `class=` and `Class=`. |
| `chatbot-definition-list` (`<dl>` dumps) | **25** | matches planning ("25"). Many are multi-class (`chatbot-definition-list chatbot-labelled-row-list`) — match the token, not the exact attribute string. |
| `@page` not composing `FcPageLayout`+`FcPageHeader` | **6** | the 6 routable routes; none compose yet (`FcPageLayout`=0, `FcPageHeader`=0 across the project). |

Because of the two upward variances, the seeded allowlist is larger than the prose implied. State this plainly in the Dev Agent Record / test summary so the count is not mistaken for scope creep. The Epic-13 end state is unchanged: every allowlist reaches **empty** by Story 13.8.

### Guard Design — mirror Story 12.1 + the Tenants reference

Reuse the proven structure from `ChatBotFluentConformanceTests` (the directly adjacent, `done` guard):

- **Repo-root walk** to `Hexalith.ChatBot.slnx`, recursive `*.razor` scan, `bin/`/`obj/`/hidden/reparse-point exclusion, non-vacuous assertion, forward-slash relative paths. (Copy these helpers or factor a small shared base — but do **not** modify the 12.1 behavior.)
- **Per-pattern shrink-only allowlists** with the **same three ratchets** 12.1 uses: (a) a missing-path assertion (seeded path must exist), (b) an offender-outside-allowlist failure, (c) a stale-entry assertion (allowlisted file that no longer offends must be removed). This is what forces 13.2–13.8 to delete entries and guarantees the allowlist can only shrink to empty.
- **Detector-fixture `[Fact]`s** crafted-markup style (12.1 lines 215-330) so the regex logic itself is pinned and a future edit cannot silently reopen a bypass.

For the **require-compose** check, mirror `DomainUiFluentConformanceTests`:
- `Domain_route_pages_declare_frontcomposer_page_headers` iterates `@page` files and requires `<FcPageHeader` (via a `DeclaresFrontComposerHeader` helper).
- `Domain_page_components_declare_frontcomposer_page_layout_modes` requires `<FcPageLayout` (via `DeclaresLayoutMeasure`).
- The ChatBot variant adds the shrink-only "not-yet-composed" backlog around that positive check, because ChatBot is **pre-migration** (Tenants is post-migration with no backlog). A page is compliant iff `(contains <FcPageLayout> AND <FcPageHeader>)` **OR** `(relative path ∈ backlog)`.

### Regex / Implementation Traps (the review will check these)

- **`chatbot-page` is a prefix of `chatbot-page-header` and `chatbot-page-title`.** A naive `Contains("chatbot-page")` over-matches and would report 6+ phantom hits and never go green. Match it as a **class token** bounded by quote/whitespace, e.g. `(?<=[""\s])chatbot-page(?=[""\s])`. Verify with the AC5 fixtures.
- **Blazor `Class=` vs HTML `class=`.** `ChatBotAssociationReviewActions.razor` uses PascalCase `Class="chatbot-command-bar …"`. The `chatbot-command-bar` matcher must catch both (case-insensitive attribute name, or just match the bespoke token `chatbot-command-bar` which only ever appears as a class).
- **Do NOT ban bare `<header>` / `<dl>`.** Other components use `<header>` legitimately (`chatbot-project-context-header`, `chatbot-task-intent-review-panel__header`, `chatbot-why-project-panel__header`) and bare `<dl>` is a valid semantic landmark (Tenants allowlists it). Ban only the `chatbot-page-header` and `chatbot-definition-list` classes respectively.
- **`<h1 class="chatbot-page-title">` lives inside the banned `<header class="chatbot-page-header">` block.** Banning the header class is sufficient; no separate `chatbot-page-title` ban is required (and `FcPageHeader` supplies the route `h1` via `HeadingId`).
- **`ProjectConversation.razor` delegates its chrome to `<ChatBotProjectConversationWorkspace>`** (it has no header markup of its own; the workspace component holds the `chatbot-page-header`/`chatbot-page`/`chatbot-definition-list` offenders). For Story 13.1 this is clean: `ProjectConversation.razor` is in the **require-compose** backlog (it's an `@page` that composes neither primitive yet), and `ChatBotProjectConversationWorkspace.razor` is in the **chrome** backlogs. **Decision deferred to 13.2:** when 13.2 migrates this route, it must decide whether `FcPageLayout`/`FcPageHeader` lands in `ProjectConversation.razor` or in the delegated workspace, and the require-compose check may then need a delegation-aware helper (as Tenants' `DeclaresFrontComposerHeader` accepts aggregate-page wrappers). Do **not** over-engineer that here — keep the page backlogged for 13.1.
- **`ProjectConversation.razor` declares `<PageTitle>` directly** (and other pages may too). The Tenants guard bans direct `<PageTitle>`/`<h1>` on route pages, but **that ban is NOT in this story's AC** — do not add it (it would expand 13.1's scope and pre-empt 13.2's migration). Note it only as an out-of-scope observation.

### FrontComposer Primitive Shape (reference for the require-compose check and for 13.2)

- `FcPageHeader.razor` renders `<PageTitle>` + a `<header role="presentation">` (deliberately not a `banner` — the shell owns the single banner) containing the `Eyebrow`, the route `h1` (`FluentText Size700/Semibold`, with `HeadingId`/`tabindex` for the focus target), and `Actions` / `Description` / `Metadata` slots. Adopting it **removes the overlap by construction** (it renders inside the shell's single `#fc-main-content` landmark). [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor`]
- `FcPageLayout.razor` carries no markup; it registers a layout measure with the cascaded `FcPageLayoutCoordinator` and renders `ChildContent`. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor`]
- Reference composition: `<FcPageLayout Mode="…"><FluentStack Orientation="Orientation.Vertical"><FcPageHeader …><Metadata/><Actions/></FcPageHeader> … Fluent content …</FluentStack></FcPageLayout>`. [Source: `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`, `TenantAuditPage.razor`]

### Architecture & Boundary Guardrails

- The UI may reference ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts only. It must not reference Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, or projection internals. This guard is test-only and touches none of that. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
- Rendering-layer governance only: preserve governed semantics, accessibility labels/landmarks (NFR6), non-color status cues (UX-DR4), EN+FR localization, focus management (UX-DR34), and the "no fake/freeform textbox" safety model — this story does not touch any of them. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13`]
- UX-DR1/DR2 now require page-composition-level conformance (compose via `FcPageLayout`/`FcPageHeader`; primary data via Fluent data components, not `<dl>` dumps; sibling titled sections in `FluentAccordion`). This guard enforces the composition + `<dl>` parts; `FluentAccordion` grouping is Story 13.7's concern and is **not** part of 13.1's banned set. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`; `#UX-DR2`]

### File Structure

- New guard: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (sibling to `ChatBotFluentConformanceTests.cs`, same project root — the existing guard is at the project root, **not** in a `Governance/` subdir).
- Scan root: `src/Hexalith.ChatBot.UI` (recursive). Allowlist entries are forward-slash paths relative to that root (e.g. `Components/Pages/GovernedOperations.razor`).
- Evidence: `_bmad-output/implementation-artifacts/tests/test-summary-story-13.1.md`.
- Do **not** put the guard in `tests/Hexalith.ChatBot.Architecture.Tests` (this is UI-source governance). Do **not** edit generated files under `obj/**/generated/`. Do **not** edit sibling submodules.

### Testing Standards

- xUnit v3 + Shouldly; no raw `Assert.*`. `[Trait("Category", "Governance")]` so the guard runs as a blocking Governance lane.
- No package-version edits — central versions already provide xUnit v3, Shouldly, coverlet (`tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj` lists no inline versions).
- Failure messages are actionable and metadata-only: relative paths + tag/class names. Never dump full file contents or fixture payloads.
- Run the UI Governance filter first, then build, then `git diff --check`. The pinned Fluent UI / xUnit / Shouldly versions are binding — no upgrades. [Source: `Directory.Packages.props`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]

### Latest Technical Information

- Local repo pins are binding: Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3, Shouldly, FrontComposer — all pinned; Epic 13 explicitly states "Fluent UI v5 and FrontComposer stay pinned" (no version churn). Do not upgrade anything to add a test. [Source: `Directory.Packages.props`; `_bmad-output/planning-artifacts/epics.md#Epic 13`]

### Git Intelligence

Recent commits are EventStore Admin + submodule-reference syncs (`21be905`, `648e101`, `face7c7`) and Epic 12 completion — no overlapping `tests/Hexalith.ChatBot.UI.Tests` change is in flight. The relevant lesson, carried from Story 12.1: narrative/fixture verification without a build-blocking source guard let conformance drift (Epic 12.1 closed leaf-control drift; Story 12.9 then verified static fixtures and missed the live overlap). This story converts the page-composition lesson into a ratcheting test, not another prose-only check. [Source: `git log`; `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`; `chatbot-ui-fluent-component-divergence` memory]

### Regression Traps to Avoid

- Do **not** implement 13.2–13.8 early: no `FcPageLayout`/`FcPageHeader` adoption, no CSS deletion, no `<dl>`→`FluentDataGrid` migration in this story.
- Do **not** let any allowlist grow. Adding a file is either a regression or a scope change requiring planning approval.
- Do **not** let a broken path pass: assert the scan root exists and at least one `.razor` was scanned; assert every seeded allowlist path exists.
- Do **not** over-match `chatbot-page` (token-boundary regex) or under-match `chatbot-command-bar` (both `class=`/`Class=`).
- Do **not** ban bare `<header>`/`<dl>` or add a `<PageTitle>`/`<h1>` ban that the AC does not list.
- Do **not** emit failures that dump large source snippets or sensitive fixture content.
- Do **not** modify the `done` Story 12.1 guard's behavior; only add alongside it.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13: ChatBot UI FrontComposer Layout Composition Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 13.1: FrontComposer layout-composition governance guard`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture` (ChatBot UI FrontComposer layout composition, lines ~405 / ~411)]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md`]
- [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj`]
- [Source: `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`]
- [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor`]
- [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`]
- [Source: `Directory.Packages.props`]

### Project Structure Notes

- Aligns with the established ChatBot UI test layout: governance guards live in `tests/Hexalith.ChatBot.UI.Tests` with `[Trait("Category", "Governance")]`, source-scan based (no bUnit render — ChatBot UI has zero `RenderComponent<`). This story extends that proven approach; the **real-render** verification is Story 13.9's job, not this one.
- No conflicts with the unified project structure. The only variance is the deliberate count uplift in the seeded allowlists (6 / 6 / 4 / 25 / 6) over the proposal's prose (6 / 2 / 3 / 25), reconciled from the authoritative source scan above.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — BMAD dev-story workflow.

### Debug Log References

- Authoritative source scan re-run on 2026-06-22 over `src/Hexalith.ChatBot.UI/**/*.razor` before seeding. Confirmed exact offender lists: `chatbot-page-header` = 6, whole-token `chatbot-page` = 6, `chatbot-command-bar` (`class=`/`Class=`) = 4, `chatbot-definition-list` = 25, `@page` routes = 6 (all non-composing; `FcPageLayout`=0, `FcPageHeader`=0 project-wide). All five lists matched the story seeds exactly.
- Verified C# token-boundary regex parity with grep (`(?<=["'\s])…(?=["'\s])`): 6 / 4 / 25, and the whole-token `chatbot-page` matcher correctly does NOT match a header/title-only fragment.
- Mutation test (reverted): dropping `Components/Pages/ProjectConversation.razor` from the not-yet-composed backlog made `Route_pages_compose_frontcomposer_layout_and_header_except_not_yet_composed_backlog` fail with the expected offender, proving the require-compose ratchet bites.

### Completion Notes List

- Added new sibling guard `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (`[Trait("Category", "Governance")]`, namespace `Hexalith.ChatBot.UI.Tests`), copying the proven Story 12.1 helper pattern (`RepositoryRoot()` walk to `Hexalith.ChatBot.slnx`, recursive `*.razor` enumeration with `bin/`/`obj/`/hidden/reparse-point exclusion, forward-slash relative paths) without modifying the `done` Story 12.1 guard.
- Four chrome/`<dl>` bans (`chatbot-page-header`, whole-token `chatbot-page`, `chatbot-command-bar`, `chatbot-definition-list`) each enforce a per-pattern shrink-only allowlist with the three Story 12.1 ratchets: missing-path, offender-outside-allowlist, and stale-entry assertions.
- The require-compose check iterates `@page` files and requires both `<FcPageLayout` and `<FcPageHeader`, with a shrink-only not-yet-composed backlog (missing-path + stale-entry ratchets) and a new-route fail-closed path. Kept simple/no delegation-aware helper per the Dev Notes — `ProjectConversation.razor` stays backlogged for 13.1; the 13.2 delegation decision is deferred.
- Pinned the detector regex logic with four `[Theory]` fixtures (AC5): whole-token `chatbot-page` boundary (rejects `-header`/`-title`), `class=`/`Class=` command-bar coverage, `chatbot-definition-list`-vs-bare-`<dl>`, and page-header-vs-other-`<header>` (`chatbot-project-context-header`, `…__header`).
- Seeded counts (6 / 6 / 4 / 25 / 6) exceed the planning prose (6 / 2 / 3 / 25) for `chatbot-page` (2→6) and `chatbot-command-bar` (3→4); the authoritative source scan supersedes the prose — these are under-counted multi-class hits, not scope creep. Epic-13 end state (every allowlist empty by 13.8) is unchanged.
- Out-of-scope observation (not changed): `ProjectConversation.razor` declares `<PageTitle>` directly and delegates chrome to `<ChatBotProjectConversationWorkspace>`; the Tenants direct-`<PageTitle>`/`<h1>` ban is NOT in this story's AC and was intentionally not added.
- Governance-only: no page migration, no `FcPageLayout`/`FcPageHeader` adoption, no `.chatbot-*` CSS deletion, no `<dl>`→`FluentDataGrid` migration, no package upgrade, and no backend/CLI/MCP/SignalR or sibling-submodule edits.
- Verification: full `Hexalith.ChatBot.slnx` build 0 warnings / 0 errors (TreatWarningsAsErrors); Governance lane 26 total (6 existing 12.1 + 20 new 13.1), 0 failed, 0 skipped; `git diff --check` clean. Evidence: `_bmad-output/implementation-artifacts/tests/test-summary-story-13.1.md`.

### File List

- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (new)
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.1.md` (new)
- `_bmad-output/implementation-artifacts/13-1-frontcomposer-layout-composition-guard.md` (status, tasks, Dev Agent Record)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (13.1 → in-progress → review)

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-22 | Implemented Story 13.1: added the FrontComposer layout-composition governance guard (`ChatBotLayoutCompositionConformanceTests`) banning hand-rolled `chatbot-page-header`/`chatbot-page`/`chatbot-command-bar` chrome and `chatbot-definition-list` `<dl>` dumps via shrink-only allowlists, and requiring `@page` composition through `FcPageLayout`+`FcPageHeader` with a shrink-only backlog; pinned detector logic with crafted fixtures. Governance lane 26/26 green, full slnx build 0/0. Status → review. |
| 2026-06-22 | Senior Developer Review (AI) — auto-fix mode. Outcome **Approve**. All 6 ACs verified against the live guard; 12.1 regression intact (6/6). Auto-fixed one LOW detector-precision defect: the require-compose check matched routes via bare `content.Contains("@page")`, which mis-classified `ChatBotProjectConversationWorkspace.razor` (whose only `@page` is a line-218 code comment) as a route — replaced with a line-anchored `@page` directive matcher (`RoutePageDirective`) and pinned it with a 4-case AC5 fixture. Guard 20→24 tests, Governance lane 44→48, build 0/0, `git diff --check` clean. Status → done. |

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-06-22 · **Mode:** story-automator autonomous review (auto-fix) · **Outcome:** ✅ Approve

### Review context (repo is ahead of this story)

Story 13.1 is the guard-first gate for Epic 13. At review time the working tree is already at **Story 13.7** (HEAD `61c74e9`), so Stories 13.2/13.4/13.5/13.6 have legitimately **burned the seeded allowlists down**. The live guard therefore differs from the 13.1-time seeds — this is expected ratcheting, **not** drift, and must not be "restored" (doing so would un-migrate committed later work and break the build):

| Pattern / rule | 13.1 seed (this story) | Live allowlist now | Burned down by | Live source scan matches live allowlist? |
| --- | --- | --- | --- | --- |
| `chatbot-page-header` | 6 | 0 (`[]`) | 13.2 | ✅ 0 offenders |
| `chatbot-page` (whole token) | 6 | 6 | — | ✅ 6 offenders |
| `chatbot-command-bar` | 4 | 0 (`[]`) | 13.2 | ✅ 0 offenders |
| `chatbot-definition-list` | 25 | 1 (`OperationalDashboards.razor`) | 13.4, 13.6 | ✅ 1 offender |
| `@page` not-yet-composed backlog | 6 | 0 (`[]`) | 13.2 | ✅ all 6 routes compose/delegate |

The seeded counts (6/6/4/25/6) were correct for 2026-06-22 per the authoritative source scan (the `chatbot-page` 2→6 and `command-bar` 3→4 uplifts are under-counted multi-class hits, confirmed). The guard's three ratchets (missing-path, offender-outside-allowlist, stale-entry) are all live and currently green, which is positive proof the scan is non-vacuous.

### Acceptance Criteria — all met

- **AC1** ✅ `[Trait("Category","Governance")]`, xUnit v3 + Shouldly, `RepositoryRoot()` walks to `Hexalith.ChatBot.slnx`, recursive `*.razor` scan with `bin`/`obj`/hidden/reparse-point exclusion, `Directory.Exists` + `ShouldNotBeEmpty` non-vacuity guards. No package-version change.
- **AC2** ✅ Three token-bounded chrome bans, each with a shrink-only allowlist + all three ratchets. `chatbot-page` uses the whole-class-token boundary regex; `chatbot-command-bar` token covers both `class=` and Blazor `Class=`.
- **AC3** ✅ `chatbot-definition-list` class ban (not bare `<dl>`), shrink-only allowlist + ratchets.
- **AC4** ✅ Require-compose over `@page` routes (`FcPageLayout` **and** `FcPageHeader`, or delegation) with a shrink-only backlog, stale-entry + missing-path assertions, and a fail-closed path for a new uncomposed route.
- **AC5** ✅ Detector pins for whole-token `chatbot-page`, `class=`/`Class=` command-bar, `chatbot-definition-list`-vs-bare-`<dl>`, page-header-vs-other-`<header>` (now plus the route-directive pin added by this review).
- **AC6** ✅ Governance-only: the 13.1 deliverable is test-only; no migration, CSS deletion, package upgrade, or sibling-submodule edit.

### Findings

- 🟢 **LOW — detector precision (AUTO-FIXED).** The require-compose check classified routes with `content.Contains("@page")`, a bare substring that also matches `@page` inside prose/comments. `ChatBotProjectConversationWorkspace.razor` (a shared component, **not** a route) contains `@page` only in a line-218 code comment, so it was scanned as a "route" and passed solely because it coincidentally composes both primitives. Replaced with a line-anchored directive matcher `RoutePageDirective` = `^[ \t]*@page\b` (multiline), which resolves to exactly the 6 real routes, and added the `Route_page_directive_matcher_ignores_at_page_in_prose` 4-case `[Theory]` to pin it. No pass/fail outcome changed; guard hardened from 20→24 tests.
- 🟡 **MEDIUM — git traceability (NOTED, not actionable).** `git log --diff-filter=A` shows the guard file was first **committed** in `b310462` ("feat(story-13.4)"), already in 13.4-shrunk form. No standalone commit captures 13.1's pristine seed-state guard; it existed only as an intermediate uncommitted state absorbed by a later batch commit. This is an artifact of the orchestrated multi-story automation; it cannot be reverted without un-migrating 13.4/13.6, so it is recorded rather than "fixed".
- ⚪ **INFO — `test-summary-story-13.1.md` counts are 13.1-time.** It records `26 total` (6 + 20); the live lane is now 48 because 13.4/13.6/13.7 added their own governance tests, and the 13.1 guard itself is 24 after this review's pin. Left as a historical record of the 13.1 milestone.
- ⚪ **INFO — out-of-scope (correctly deferred).** `ProjectConversation.razor` declares `<PageTitle>` directly and delegates chrome to `<ChatBotProjectConversationWorkspace>`; the Tenants direct-`<PageTitle>`/`<h1>` ban is not in this story's AC and was rightly not added (it would pre-empt 13.2).

### Verification

| Command | Result |
| --- | --- |
| `dotnet build tests/Hexalith.ChatBot.UI.Tests/...csproj --no-restore` | ✅ 0 warnings, 0 errors (TreatWarningsAsErrors) |
| `dotnet test --filter "FullyQualifiedName~ChatBotLayoutCompositionConformanceTests"` | ✅ 24 passed, 0 failed (was 20 pre-review) |
| `dotnet test --filter "FullyQualifiedName~ChatBotFluentConformanceTests"` (12.1 regression) | ✅ 6 passed, 0 failed |
| `dotnet test --filter "Category=Governance"` (full lane) | ✅ 48 passed, 0 failed, 0 skipped |
| `git diff --check` | ✅ clean |

No CRITICAL issues. One LOW auto-fixed; one MEDIUM and two INFO recorded as accepted artifacts. → **done**.
