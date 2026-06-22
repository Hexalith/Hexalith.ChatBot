# Test Summary - Story 13.7

Date: 2026-06-22
Baseline commit: 8a7964d

## Scope

Story 13.7 owns one cross-cutting rule from `hexalith-ux-instructions.md` (**Page sections**):
page-like surfaces with **two or more sibling titled content sections** in a single shell region
must group those sections in a single `FluentAccordion` (one `FluentAccordionItem` per section,
every item expanded by default), mirroring `Hexalith.Tenants.UI`
(`Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions`). Single-primary content
regions stay outside the accordion.

**Rendering-layer correction only.** No backend / command-spine / query / CLI / MCP / SignalR /
Dapr / EventStore change and no sibling-submodule edit. Governed read/decision semantics, the "no
fake/freeform textbox" safety model, NFR6 landmarks/labels, UX-DR4 non-color cues, UX-DR34 focus,
and EN+FR i18n are preserved exactly. **No new localization key** — every accordion `Header` reuses
the section's existing `ChatBotUiTextKey.*` title key.

### Surfaces grouped (4 in-scope, one `FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true"` each)

| File | Region | Grouped sibling titled sections (one `FluentAccordionItem … Expanded="true"` each) |
| --- | --- | --- |
| `Components/Pages/GovernedOperations.razor` | `MainContent` | `operational-queue` + `<ChatBotApprovalQueuePriorityView ShowHeading="false"/>` + conditional `operation-outcome` |
| `Components/Pages/ProjectWorkspace.razor` | `ComplementaryPanel` | `safe-guidance` + `operations-link` |
| `Components/Governed/ChatBotProjectConversationWorkspace.razor` | `ComplementaryPanel` (`@if (Conversation is { } conversation)`) | `context-panel` + `files-panel` |
| `Components/Pages/AssociationReview.razor` | `ComplementaryPanel` (`@if (Review is { } review)`) | `<ChatBotAssociationEvidenceComparison ShowHeading="false"/>` (`association-comparison`) + `association-source` |

Each `FluentAccordionItem` carries `HeadingLevel="2"` (preserving the former `<h2 class="chatbot-section-title">` rank);
the now-duplicate inner `<h2>` title is removed and the accessible name moves to the kept `<section>`
landmark via `aria-label`. The two single-use child components
(`ChatBotApprovalQueuePriorityView`, `ChatBotAssociationEvidenceComparison`) gained a backward-compatible
`ShowHeading` parameter (default `true`) that suppresses their internal `<h2>` when grouped and switches the
`<section>` to `aria-label`.

### A11y / marker preservation (AC3)

- `id="operation-outcome-title"` survives as the focus-landing target — re-homed onto the kept
  `<section id="operation-outcome-title">` inside its `FluentAccordionItem`; asserted green by
  `ChatBotAccessibilityFocusContractTests` (`LoadedContentLandingTargetId: "operation-outcome-title"`
  busy-region contract).
- GovernedOperations queue keeps `data-chatbot-operational-queue="true"`, `role="table"`/`role="row"`,
  `data-chatbot-queue-family/-ref/-item-ref/-source-version`, and `data-chatbot-responsive-fixture`.
- No motion-only cue introduced; UX-DR4 non-color status cues intact.

## Scope fences honored (NOT this story)

- `class="chatbot-page"` / `class="chatbot-section"` content boxes → **Story 13.3** (the kept
  `<section class="chatbot-section">` wrappers become the accordion-item bodies, exactly as Tenants keeps
  `<section class="tenant-audit__controls">` inside its `FluentAccordionItem`; `PageContentBoxAllowlist`
  left as-is).
- Deleting any `chatbot.tokens.css` rule (incl. `.chatbot-section`) → **Story 13.8** (no CSS edited).
- **`OperationalDashboards.razor` accordion → Story 13.5.** Its two titled sections
  (`operational-dashboards-freshness`, conditional `operational-dashboards-slos`) are interleaved with the
  page's single primary `chatbot-table`/data-viz region; grouping them requires the layout reshape that
  Story 13.5 (data-viz owner) carries, so 13.7 leaves the file untouched (0 `<FluentAccordion>`). Story 13.5
  must add the accordion + the guard's accordion-required entry when it reshapes the page.
- **`ComplianceAuditInvestigation.razor` → out of scope (single primary surface).** Post-13.6 the page is one
  primary timeline `<section>` whose filter form is a *nested* `<h3>` sub-region (not a sibling), plus a single
  phone-fallback section in the complementary. No region has ≥2 sibling titled sections, so the
  "single primary content stays outside the accordion" exemption applies; the file is not modified by this
  story (0 `<FluentAccordion>`).
- `FcPageLayout`/`FcPageHeader` adoption → **Story 13.2** (built on, not reverted).
- `<dl>` data dumps already migrated to `FluentStack`/`FluentText`/`<code>` → **Story 13.4** (the `CodeRow`
  fragments are kept as accordion-item content).

## Commands

| Command | Result |
| --- | --- |
| `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` | Passed: **0 warnings, 0 errors** (Release, TreatWarningsAsErrors). |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "Category=Governance" --no-build -c Release -m:1 -nodeReuse:false` | Passed: **48** total, 0 failed, 0 skipped (incl. the new `Story13AccordionMigrationTests.Multi_region_surfaces_group_sibling_sections_with_fluent_accordions`, `ChatBotLayoutCompositionConformanceTests`, `ChatBotFluentConformanceTests`). |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --no-build -c Release -m:1 -nodeReuse:false` (full project, regression) | Passed: **215** total, 0 failed, 0 skipped (incl. the retargeted `ChatBotAccessibilityFocusContractTests`). |
| `DiffEngine_Disabled=true dotnet test …UI.E2E.Tests… --no-build -c Release -m:1 -nodeReuse:false` (real browser) | Passed: **136** total, 0 failed, **0 skipped** in **21 s** — Chromium present (`/usr/bin/google-chrome` → Google Chrome 148), so the `BrowserHarness` no-browser fallback did **not** trigger; the real-browser path executed (covers `GovernedOperationsVisualFoundationE2ETests`, `ApprovalQueuePriorityE2ETests`, `ProjectConversationE2ETests`, `AssociationDecisionRecordingE2ETests`, `ProjectWorkspaceE2ETests`). |
| `rg -l "<FluentAccordion" src/Hexalith.ChatBot.UI/Components` | Exactly the **4** in-scope files (GovernedOperations, ProjectWorkspace, AssociationReview, ChatBotProjectConversationWorkspace). |
| `rg -c "<FluentAccordion" …/OperationalDashboards.razor …/ComplianceAuditInvestigation.razor` | **0** in both (scope fence honored). |
| `git diff --check` | Passed: clean. |

`ChatBotFluentConformanceTests` stays green, confirming no raw `<button>/<input>/<select>/<textarea>`
and no legacy v4/FAST tokens were introduced — the grouping uses `FluentAccordion`/`FluentAccordionItem`
only and keeps the existing Fluent content. No `Directory.Packages.props` / package-version edit (Fluent UI
Blazor pinned `5.0.0-rc.3-26138.1`, which ships `FluentAccordion`/`FluentAccordionItem`/`AccordionExpandMode`).

## Conformance guard (AC4)

`Story13AccordionMigrationTests.Multi_region_surfaces_group_sibling_sections_with_fluent_accordions`
(`[Trait("Category", "Governance")]`) seeds an explicit `AccordionRequiredFiles` list of the **4**
in-scope relative paths and asserts: (a) the list is non-vacuous, (b) every path exists (missing-path
ratchet), and (c) each file contains `"<FluentAccordion"`, `"ExpandMode=\"AccordionExpandMode.Multi\""`,
and `"Expanded=\"true\""`. The four layout-composition allowlists
(`PageContentBoxAllowlist`, `DefinitionListAllowlist`, `PageHeaderChromeAllowlist`, `CommandBarAllowlist`)
and `NotYetComposedPageBacklog` — all currently empty — are left exactly as-is (13.3/13.5/13.8 own them).

## Coupled tests retargeted (AC5, not loosened)

- `ChatBotAccessibilityFocusContractTests` — the "Story 12.7 governed operations" marker set is
  **strengthened** with `<FluentAccordion`, `ExpandMode="AccordionExpandMode.Multi"`, `Expanded="true"`,
  and `id="operation-outcome-title"` (alongside the preserved `data-chatbot-operational-queue="true"`,
  `role="table"`, `role="row"`, `data-chatbot-queue-family` markers); the busy-region contract still pins
  `operation-outcome-title` as the loaded-content landing target.
- The in-scope-surface E2E fixtures (`GovernedOperationsVisualFoundationE2ETests`,
  `ApprovalQueuePriorityE2ETests`, `ProjectConversationE2ETests`, `AssociationDecisionRecordingE2ETests`,
  `ProjectWorkspaceE2ETests`) pass on the real-browser path with their governed/a11y assertions intact.

## Notes

Story 13.9 owns the live `aspire run` real-render cross-surface re-verification gate; this story's truth
signal is the build-blocking source-scan guard above plus the real-browser E2E lane.
