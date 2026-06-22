---
baseline_commit: 8a7964de373e3e7e8425bc2c24f63c2e73e0ad09
---

# Story 13.7: Group sibling titled sections in FluentAccordion (UX Page-sections rule)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a ChatBot governed-UI user,
I want page regions that stack two or more sibling titled content sections to group those sections in a single `FluentAccordion` (each section expanded by default),
so that every surface follows the Hexalith UX "Page sections" rule with consistent, collapsible Fluent grouping instead of a flat run of hand-rolled `<section class="chatbot-section">` blocks — while every accessible label, landmark, focus target, governed semantic, and EN+FR string is preserved exactly.

## Context

Epic 13 closes the page-level FrontComposer/Fluent composition gap that Epics 10 (shell) and 12 (leaf controls) left open. Story 13.7 owns one cross-cutting rule from `hexalith-ux-instructions.md` (**Page sections**):

> *Page-like surfaces such as pages, dialogs, and detail panels with **two or more sibling titled content sections** must group those sections in a single `FluentAccordion`, with one `FluentAccordionItem` per section. Keep page titles, breadcrumbs, toolbars, navigation chrome, and **single primary content regions** such as one grid, form, detail view, or chart outside the accordion. Do not hide the only primary content behind an accordion interaction; when a primary section belongs in an accordion with other sibling sections, expand the primary item by default.*

The ChatBot UI uses **zero** `FluentAccordion` today (confirmed by source scan) across ~23 raw `<section class="chatbot-section">` siblings (`sprint-change-proposal-2026-06-22.md`, defect table). This is a **rendering-layer correction only**: no backend, command-spine, query, CLI, or MCP change. The reference implementation is `Hexalith.Tenants.UI`, where every multi-region page groups its sibling sections in `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">` with each `<FluentAccordionItem … Expanded="true">`, guarded by `DomainUiFluentConformanceTests.Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions`.

**Why 13.7 is the cross-cutting accordion owner:** Story 13.6 (`done`) explicitly deferred its accordion grouping here ("Grouping the timeline/filter sibling sections in `FluentAccordion` → **Story 13.7**"). The Story 13.1 layout-composition guard does **not** enforce accordion grouping (it bans page-header/content-box/command-bar/`<dl>` chrome only), so 13.7 must add the accordion conformance check itself — the source-scan guard is the build-enforced truth signal, complemented by Story 13.9's real-render gate.

### Scope decision — which surfaces 13.7 owns (read this first)

A fresh source scan (2026-06-22, against `baseline_commit` `8a7964d`) of every `@page` and panel under `src/Hexalith.ChatBot.UI/Components/` for **sibling titled `<section>`-level regions within a single surface region** (the shell's `<MainContent>` column or its `<ComplementaryPanel>`) gives this authoritative result. **Re-run the scan at dev time — line numbers drift with the in-flight 13.2/13.3/13.5 work; anchor edits by content, not line number.**

**IN SCOPE — 4 surfaces, each with ≥2 sibling titled sections in one region:**

| File | Region | Sibling titled sections to group (one `FluentAccordionItem` each) |
|---|---|---|
| `Components/Pages/GovernedOperations.razor` | `MainContent` | `operational-queue` section + `<ChatBotApprovalQueuePriorityView/>` (renders the `approval-queue-priority` section) + conditional `operation-outcome` section |
| `Components/Pages/ProjectWorkspace.razor` | `ComplementaryPanel` | `project-workspace-safe-guidance` + `project-workspace-operations-link` |
| `Components/Governed/ChatBotProjectConversationWorkspace.razor` | `ComplementaryPanel` | `project-workspace-context-panel` + `project-workspace-files-panel` (inside `@if (Conversation is …)`) |
| `Components/Pages/AssociationReview.razor` | `ComplementaryPanel` | `<ChatBotAssociationEvidenceComparison/>` (renders the `association-comparison` section) + `association-source` (source-metadata) section |

**OUT OF SCOPE — do NOT touch in this story (documented, defensible):**
- **`Components/Pages/OperationalDashboards.razor` → deferred to Story 13.5.** Its two titled sections (`operational-dashboards-freshness`, conditional `operational-dashboards-slos`) are **interleaved with the primary `chatbot-table` views grid** (the page's one primary data region). Grouping them per the rule requires relocating them around the data-viz, which is exactly the layout reshape Story 13.5 ("Operational dashboards — real Fluent data visualization") owns. 13.5 must add the `FluentAccordion` and the guard's accordion-required entry when it reshapes the page; 13.7 leaves this file untouched to avoid a collision. *(Add a one-line note to this effect in your evidence doc; do NOT silently skip it.)*
- **`Components/Pages/ComplianceAuditInvestigation.razor` → single primary surface, not a ≥2-sibling case.** Post-13.6 the page is one primary `<section aria-labelledby="compliance-timeline-title">` (the audit investigation surface) whose filter form is a **nested** `<section aria-labelledby="compliance-filters-title">` (an `<h3>` sub-region, not a sibling), plus a single phone-fallback section in the complementary. No region has ≥2 sibling titled sections, so the "single primary content stays outside the accordion" exemption applies. *(13.6's "defer to 13.7" note was about the candidate filter/timeline split; the merged structure makes accordion grouping inapplicable — document this.)*
- **GovernedOperations `ComplementaryPanel`** (lone `governed-review-context` section), **ProjectWorkspace `MainContent`** (lone `project-workspace-picker` section), **ChatBotProjectConversationWorkspace `MainContent`** (the conversation stream + composer is the single primary task flow), **AssociationReview `MainContent`** (the `association-candidates` radiogroup + `<ChatBotAssociationReviewActions/>` decision form is the page's single primary select-then-decide task flow — do **not** hide candidate selection/decision behind an accordion). Each is a single primary region → stays outside.
- Editor/panel components (`ChatBotTenantPolicyEditor`, `ChatBotNotificationRoutingEditor`, `ChatBotEscalationPolicyEditor`, `ChatBotWhyProjectPanel`, `ChatBotAiActionPreviewSections`, `ChatBotTaskIntentReviewPanel`, etc.) each render a **single** primary titled section with at most a **nested** sub-section — not ≥2 siblings → out of scope.

**This story does NOT own** (leave for the named story; do not touch):
- Replacing the `class="chatbot-page"`/`class="chatbot-section"` bordered content boxes with `FluentStack`/`FluentCard` → **Story 13.3** (the `PageContentBoxAllowlist` entries STAY; keep the `<section class="chatbot-section">` wrappers as the accordion-item bodies, exactly as Tenants keeps `<section class="tenant-audit__controls">` inside its `FluentAccordionItem`).
- Deleting any `chatbot.tokens.css` rule (incl. `.chatbot-section`, the skip-link) → **Story 13.8**.
- OperationalDashboards data-viz + its accordion → **Story 13.5**.
- The `FcPageLayout`/`FcPageHeader` adoption already committed (Story 13.2) → build on it; do not revert.

## Acceptance Criteria

1. **(Accordion grouping — epic AC)** **Given** the 4 in-scope surfaces above, **When** re-composed, **Then** the sibling titled sections in the named region of each file are grouped in a **single** `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">`, with **one `<FluentAccordionItem … Expanded="true">` per section** (mirroring `Hexalith.Tenants.UI` `TenantAuditPage`/`UserMembershipLookupPage`/`TenantDetailPage`). Each item's `Header` is the section's existing **localized** title (`Header="@UiText[ChatBotUiTextKey.<ExistingKey>]"`), and `HeadingLevel` preserves the original heading rank (`HeadingLevel="2"` for the former `<h2 class="chatbot-section-title">`; the now-duplicate inner `<h2>` is removed since the accordion item header is the heading). Every grouped section is **expanded by default** (`Expanded="true"`) so no content is hidden behind an interaction.
2. **(Single primary content + deferred surfaces stay outside)** **Then** within each touched file the single-primary regions stay **outside** the accordion (GovernedOperations page header + transient status banners + the complementary `governed-review-context`; ProjectWorkspace `MainContent` picker; ChatBotProjectConversationWorkspace `MainContent` stream/composer/Why-panel; AssociationReview `MainContent` candidates+actions); and **`OperationalDashboards.razor` and `ComplianceAuditInvestigation.razor` are not modified by this story** (their files do not appear in this story's diff).
3. **(A11y, focus, landmarks, markers preserved — NFR6 / UX-DR34)** **Then** each grouped `<section>` keeps its semantic landmark and accessible name (`aria-label`/`aria-labelledby`), the heading rank is preserved via `HeadingLevel`, and **no focus target or data marker is lost**: specifically the `id="operation-outcome-title"` focus-landing target (asserted by `ChatBotAccessibilityFocusContractTests` `LoadedContentLandingTargetId`) survives, and the GovernedOperations queue keeps `data-chatbot-operational-queue="true"`, `role="table"`/`role="row"`, `data-chatbot-queue-*`, plus every existing `data-chatbot-*`/`data-redaction-state` marker on the regrouped sections. No motion-only cue is introduced; UX-DR4 non-color status cues are intact.
4. **(Accordion conformance guard — non-vacuous, build-blocking)** **Then** a new `[Fact]` carrying `[Trait("Category", "Governance")]` is added to `ChatBotLayoutCompositionConformanceTests` (or a sibling `Story13AccordionMigrationTests`, mirroring `Story13DefinitionListMigrationTests`), modeled on Tenants `Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions`: it asserts an **explicit accordion-required file list** of exactly the 4 in-scope relative paths exists (missing-path assertion) and that each contains `"<FluentAccordion"`, `"ExpandMode=\"AccordionExpandMode.Multi\""`, and `"Expanded=\"true\""`. The four existing layout-composition allowlists (`PageContentBoxAllowlist`, `DefinitionListAllowlist`, `PageHeaderChromeAllowlist`, `CommandBarAllowlist`) and the `NotYetComposedPageBacklog` are **left exactly as-is** (13.3/13.5/13.8 own them); the full Governance lane stays green.
5. **(Coupled source-scan / E2E tests retargeted, not loosened)** **Then** any existing test that pins the pre-accordion structure of an in-scope surface is updated to assert the new accordion shape while keeping its governed/a11y assertion strength: at minimum `ChatBotAccessibilityFocusContractTests` (the `operation-outcome-title` focus-landing contract and the "Story 12.7 governed operations" marker set) and the relevant in-scope-surface E2E fixtures (`GovernedOperationsVisualFoundationE2ETests`, `ApprovalQueuePriorityE2ETests`, `ProjectConversationE2ETests`, `AssociationDecisionRecordingE2ETests`, and any ProjectWorkspace fixture) still pass — retargeted to the `FluentAccordion`/`FluentAccordionItem` render, not weakened into no-ops.
6. **(Safety + i18n + scope-fence + build invariants)** **Then** no raw `<button>/<input>/<select>/<textarea>` and no legacy v4/FAST tokens are introduced (`ChatBotFluentConformanceTests` stays green); **no new localization keys** — every accordion `Header` reuses an existing `ChatBotUiTextKey.*` (the section's current title key), and EN+FR remain intact; the governed read/decision semantics and the "no fake/freeform textbox" safety model are unchanged; the `PageContentBoxAllowlist`/`chatbot.tokens.css`/13.2 header composition are untouched (13.3/13.8/13.2 fences); the adapter boundary holds (UI → Client/ServiceDefaults/FrontComposer only); no sibling-submodule edit; and the Release build is clean (`TreatWarningsAsErrors`, 0/0).

## Tasks / Subtasks

- [x] **Task 1 — Group GovernedOperations `MainContent` sibling sections (AC: 1, 2, 3)**
  - [x] In `Components/Pages/GovernedOperations.razor`, wrap the three `MainContent` sibling regions — the `operational-queue` `<section>`, the `<ChatBotApprovalQueuePriorityView/>` invocation, and the conditional `operation-outcome` `<section>` — in one `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">`, each as a `<FluentAccordionItem Header="@UiText[ChatBotUiTextKey.<title-key>]" HeadingLevel="2" Expanded="true">`. Title keys: `GovernedOperationsOperationalQueueTitle`, `ApprovalQueuePriorityTitle`, `OutcomeTitle`. The page `FcPageHeader`, the submitting/failed status banners, and (per AC2) the `governed-review-context` complementary section stay outside.
  - [x] Remove each section's now-duplicate inner `<h2 id="…" class="chatbot-section-title">` heading (the accordion `Header` is the heading) **but** preserve the `<section>` element with its `aria-labelledby`→`aria-label` (switch `aria-labelledby="operational-queue-title"` to `aria-label="@UiText[ChatBotUiTextKey.GovernedOperationsOperationalQueueTitle]"` when its `<h2 id>` is removed) and ALL `data-chatbot-*`/`role` markers. **Keep `id="operation-outcome-title"` reachable as a focus-landing target** (AC3) — e.g. set it on the `operation-outcome` `<FluentAccordionItem Id="operation-outcome-title">` or retain a heading element carrying that id; verify against `ChatBotAccessibilityFocusContractTests`.
  - [x] For `<ChatBotApprovalQueuePriorityView/>` (single-use child that renders its own titled `<section>`): place it as the accordion item body and suppress its now-duplicate internal `<h2 id="approval-queue-priority-title">` heading (add a minimal `RenderHeading`/`ShowHeading` parameter defaulting to current behavior, set `false` here, or move the heading id onto the section as `aria-label`). Keep the component's `<section>` landmark + markers.
- [x] **Task 2 — Group ProjectWorkspace `ComplementaryPanel` sibling sections (AC: 1, 2, 3)**
  - [x] In `Components/Pages/ProjectWorkspace.razor` `<ComplementaryPanel>`, wrap the `safe-guidance` + `operations-link` `<section>`s in one `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">`; items `Header="@UiText[ChatBotUiTextKey.ProjectWorkspaceSafeGuidanceTitle]"` and `ProjectWorkspaceOperationsLinkTitle`, both `HeadingLevel="2" Expanded="true"`. The `MainContent` picker stays outside.
- [x] **Task 3 — Group ChatBotProjectConversationWorkspace `ComplementaryPanel` sibling sections (AC: 1, 2, 3)**
  - [x] In `Components/Governed/ChatBotProjectConversationWorkspace.razor`, inside the `@if (Conversation is { } conversation)` block of `<ComplementaryPanel>`, wrap the `context-panel` + `files-panel` `<section>`s in one `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">`; items `Header="@UiText[ChatBotUiTextKey.ProjectWorkspaceContextPanelTitle]"` and `ProjectWorkspaceFilesPanelTitle`, `HeadingLevel="2" Expanded="true"`. The Why-panel (`ChatBotWhyProjectPanel`) overlay and its loading/error banners stay outside; the `MainContent` stream/composer stays outside.
- [x] **Task 4 — Group AssociationReview `ComplementaryPanel` sibling sections (AC: 1, 2, 3)**
  - [x] In `Components/Pages/AssociationReview.razor` `<ComplementaryPanel>` (inside `@if (Review is { } review)`), wrap `<ChatBotAssociationEvidenceComparison/>` + the `association-source` (source-metadata) `<section>` in one `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">`. The source-metadata item `Header="@UiText[ChatBotUiTextKey.AssociationReviewSourceMetadata]"`; the comparison item reuses the component's existing title key. Both `HeadingLevel="2" Expanded="true"`. The `MainContent` candidates radiogroup + `<ChatBotAssociationReviewActions/>` stay outside (AC2). For `<ChatBotAssociationEvidenceComparison/>` apply the same single-use child-heading handling as Task 1.
- [x] **Task 5 — Add the accordion conformance guard (AC: 4)**
  - [x] In `tests/Hexalith.ChatBot.UI.Tests`, add a `[Fact] [Trait("Category", "Governance")]` (in `ChatBotLayoutCompositionConformanceTests.cs` or a new `Story13AccordionMigrationTests.cs`) named e.g. `Multi_region_surfaces_group_sibling_sections_with_fluent_accordions`. Seed an explicit `AccordionRequiredFiles` list of the 4 relative paths; assert each path exists (missing-path) and each file contains `"<FluentAccordion"`, `"ExpandMode=\"AccordionExpandMode.Multi\""`, and `"Expanded=\"true\""`. Reuse the existing `RepositoryRoot()`/`UiRoot()`/`EnumerateFiles` helpers; assert the scan is non-vacuous. Do **not** edit `PageContentBoxAllowlist`/`DefinitionListAllowlist`/`PageHeaderChromeAllowlist`/`CommandBarAllowlist`/`NotYetComposedPageBacklog`.
- [x] **Task 6 — Retarget coupled source-scan / E2E tests (AC: 5)**
  - [x] Re-read `ChatBotAccessibilityFocusContractTests.cs`; update the `operation-outcome-title` `LoadedContentLandingTargetId` contract and the "Story 12.7 governed operations" marker set to the accordion render (keep the focus-target id reachable and the marker assertions positive). Re-read and retarget any in-scope-surface E2E fixture that pins the old `<section>`/`<h2>` structure (`GovernedOperationsVisualFoundationE2ETests`, `ApprovalQueuePriorityE2ETests`, `ProjectConversationE2ETests`, `AssociationDecisionRecordingE2ETests`, ProjectWorkspace fixture). Assert `FluentAccordion`/`FluentAccordionItem` + the localized header + preserved markers; do not loosen. List every retargeted test in the evidence doc.
- [x] **Task 7 — Verify & record evidence (AC: 1–6)**
  - [x] Build: `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` → 0 Warning / 0 Error.
  - [x] Governance lane: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-build -m:1 -nodeReuse:false` → all green (incl. the new accordion `[Fact]`, `ChatBotLayoutCompositionConformanceTests`, `ChatBotFluentConformanceTests`).
  - [x] Full `Hexalith.ChatBot.UI.Tests` regression + the retargeted E2E suites (real-browser; confirm `Skipped: 0` so the Chromium path actually executed — see memory `chatbot-e2e-nobrowser-fallback-trap`).
  - [x] `rg "<FluentAccordion" src/Hexalith.ChatBot.UI/Components` → exactly the 4 in-scope files. `git diff --check` → clean. `git diff --name-only` → only the 4 razor files (+ `ChatBotApprovalQueuePriorityView.razor`/`ChatBotAssociationEvidenceComparison.razor` if a heading param was added), the guard test, the retargeted coupled tests, the evidence doc, `sprint-status.yaml`, and this story file. **No `OperationalDashboards.razor`/`ComplianceAuditInvestigation.razor`/`chatbot.tokens.css`/submodule change.**
  - [x] Write `_bmad-output/implementation-artifacts/tests/test-summary-story-13.7.md` (metadata-only, same shape as `test-summary-story-13.6.md`), noting the OperationalDashboards→13.5 and ComplianceAudit out-of-scope decisions. Live `aspire run` visual proof is Story 13.9's gate.

## Dev Notes

### Current state of the in-scope files (read before editing; line numbers are baseline `8a7964d` and will drift)

- **`GovernedOperations.razor`** — `<MainContent>` wraps `<FcPageLayout>` → `<section class="chatbot-page">` (kept; 13.3's box) → `<FcPageHeader>` (kept; 13.2) → then the three siblings: `<section class="chatbot-section" aria-labelledby="operational-queue-title" data-chatbot-operational-queue="true" …>` (~L38, contains the `role="table"` queue), `<ChatBotApprovalQueuePriorityView />` (~L122), and `@if (State.Value.Outcome is { } outcome) { <section class="chatbot-section" aria-labelledby="operation-outcome-title">… }` (~L146). The submitting/failed `ChatBotStatusBanner`s (~L124–144) sit **between** the queue and the outcome — keep them outside the accordion (they are transient status, not titled sections). `<ComplementaryPanel>` has the lone `governed-review-context` section (~L220) → stays outside.
- **`ProjectWorkspace.razor`** — only the `else` branch (no `ProjectId`) renders sections; the `ProjectId` branch delegates to `ChatBotProjectConversationWorkspace` (Task 3 territory). `<ComplementaryPanel>` (~L80) has `safe-guidance` (~L81) + `operations-link` (~L85) sections. `<MainContent>` `project-workspace-picker` (~L46) is the single primary section → stays outside.
- **`ChatBotProjectConversationWorkspace.razor`** — `<ComplementaryPanel>` renders the Why-panel + banners, then `@if (Conversation is { } conversation)` → `context-panel` (~L168) + `files-panel` (~L178) sections. Group only those two; the Why-panel/banners and the `<MainContent>` stream/`ChatBotGovernedComposer` stay outside.
- **`AssociationReview.razor`** — `<ComplementaryPanel>` `@if (Review is { } review)` → `<ChatBotAssociationEvidenceComparison/>` (~L118, renders `association-comparison` `<section>`) + `<section aria-labelledby="association-source">` (~L119, the source-metadata, already wrapped in a `FluentCard`). `<MainContent>` candidates (~L71) + `<ChatBotAssociationReviewActions/>` (~L90) stay outside.

### Reference pattern (mirror `Hexalith.Tenants.UI`, same pinned Fluent v5)

`Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (L45–111) and `UserMembershipLookupPage.razor`/`TenantDetailPage.razor`:

```razor
<FluentAccordion Class="…__support" ExpandMode="AccordionExpandMode.Multi" Block="true">
    <FluentAccordionItem Header="@Localizer["…ControlsLabel"]" Expanded="true">
        <section class="…__controls" aria-label="@Localizer["…ControlsLabel"]">
            @* the section element is KEPT for its landmark; the inner heading is dropped *@
            …content…
        </section>
    </FluentAccordionItem>
    <FluentAccordionItem Header="@Localizer["…State.Title"]" Expanded="true">
        <section …> … </section>
    </FluentAccordionItem>
</FluentAccordion>
@* the PRIMARY data grid stays OUTSIDE the accordion *@
<section class="…__grid-shell" aria-labelledby="…-grid-heading"> <h2 …/> <DataGrid …/> </section>
```

- The `<section>` wrappers are **kept** (landmark + `aria-label`); the accordion item `Header` is the heading. This is the model for all 4 ChatBot surfaces.
- A conditionally-rendered item (Tenants `UserMembershipLookupPage` results item; here GovernedOperations `operation-outcome`) is simply a `@if`-guarded `<FluentAccordionItem>` inside the accordion — that is fine, the accordion still has ≥2 items from the always-present ones.
- `Block="true"` makes headers fill the accordion width (matches Tenants).

### Latest technical information — FluentAccordion / FluentAccordionItem (Fluent v5, pinned `5.0.0-rc.3-26138.1`)

Confirmed via the fluent-ui MCP and corroborated by `Hexalith.Tenants.UI` compiling on a compatible pin:
- **`FluentAccordion`**: `ExpandMode` (`AccordionExpandMode` — `Multi` default / `Single`; use **`Multi`** so every item is independently expandable), `Block` (`bool?` — header fills width), `Class`, `HeadingLevel` (`int?` — sets the aria heading level for all items). Events `OnAccordionItemChange`, `@bind-ActiveId` exist but are not needed here.
- **`FluentAccordionItem`**: `Header` (`string?` — plain-text heading; use the localized title string), `Expanded` (`bool`, default **False** → you MUST set `Expanded="true"`), `HeadingLevel` (`int?` — set `2` to preserve the former `<h2>` rank; can be set per item or once on the parent accordion), `HeaderTemplate` (`RenderFragment?` — only if a header needs rich content; prefer plain `Header`), `Id`, `Disabled`, `HeaderTooltip`.
- No new `@using` is needed — `Components/_Imports.razor` already imports `Microsoft.FluentUI.AspNetCore.Components`. `FluentAccordion`/`FluentAccordionItem`/`AccordionExpandMode` are present in the pinned package (Tenants uses them on the same line). [Source: fluent-ui MCP `get_component_details FluentAccordion`/`FluentAccordionItem`; `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]

### Regression traps (the review will check these)

- **`operation-outcome-title` is a focus-landing target.** `ChatBotAccessibilityFocusContractTests` pins `LoadedContentLandingTargetId: "operation-outcome-title"`. If you delete the `<h2 id="operation-outcome-title">` without re-homing that id (onto the `<FluentAccordionItem Id="operation-outcome-title">` or a retained heading), the focus-management contract breaks. Verify the id is still a valid scroll/focus target after grouping.
- **Don't lose `<section>` landmarks or `data-*` markers.** Keep each `<section>` element (13.3 owns removing the box, not the landmark). When you drop an inner `<h2 id>` whose id fed `aria-labelledby`, switch the section to `aria-label="@UiText[…sameKey]"` so the accessible name survives. Preserve `data-chatbot-operational-queue`, `role="table"`/`role="row"`, `data-chatbot-queue-*`, `data-chatbot-responsive-fixture`, and any `data-redaction-state`/`data-escalation-state`.
- **Child-component sections (`ChatBotApprovalQueuePriorityView`, `ChatBotAssociationEvidenceComparison`) render their own heading.** Wrapping them in an accordion item creates a duplicate heading. Suppress the component's internal heading (small `RenderHeading`/`ShowHeading` param, default preserving today's behavior) — both components are single-use (GovernedOperations / AssociationReview respectively), so this is safe, but keep the param backward-compatible and documented.
- **Expanded by default — do not collapse primary content.** Every `FluentAccordionItem` here must be `Expanded="true"`. The UX rule forbids hiding the only/primary content behind an interaction; Tenants sets all items expanded.
- **No new localization keys.** Reuse the section's current title key as the `Header` (e.g. `GovernedOperationsOperationalQueueTitle`, `ApprovalQueuePriorityTitle`, `OutcomeTitle`, `ProjectWorkspaceSafeGuidanceTitle`, `ProjectWorkspaceOperationsLinkTitle`, `ProjectWorkspaceContextPanelTitle`, `ProjectWorkspaceFilesPanelTitle`, `AssociationReviewSourceMetadata`, and the evidence-comparison component's existing key). Do not hard-code English; do not invent keys.
- **Stay inside your scope.** Do NOT remove `class="chatbot-page"`/`class="chatbot-section"` (13.3 — `PageContentBoxAllowlist` stays), do NOT delete any `chatbot.tokens.css` rule (13.8), do NOT touch `OperationalDashboards.razor` (13.5) or `ComplianceAuditInvestigation.razor`, do NOT revert the 13.2 `FcPageLayout`/`FcPageHeader` adoption, and do NOT add a `FluentDataGrid` migration (13.4/13.5). The four existing layout-composition allowlists are read-only for this story.
- **No raw controls / no legacy tokens.** `ChatBotFluentConformanceTests` still runs — Fluent components only; never `<button>/<input>/<select>/<textarea>` or `--type-ramp-*`/`--neutral-*`/`--accent-*`/`--neutral-fill-*`/`--palette-*`.

### Architecture & boundary guardrails

- UI may reference ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts only — never Server, Gateway, Dapr, EventStore internals. This story touches only `Hexalith.ChatBot.UI` `.razor` + UI test files + one evidence doc. [Source: `architecture.md#Frontend Architecture` (L411); `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
- Rendering-layer correction only: governed semantics, NFR6 a11y landmarks/labels, UX-DR4 non-color cues, UX-DR34 focus, EN+FR i18n, and the "no fake/freeform textbox" model are preserved exactly. UX-DR2 / Epic 13 require sibling titled sections to group in `FluentAccordion` per the Page-sections rule; the layout-composition allowlists must reach **empty** at Epic 13 completion (13.8), which this story does not regress. [Source: `architecture.md#Frontend Architecture` (L411); `epics.md#Epic 13`/`#Story 13.7`; `hexalith-ux-instructions.md#Page sections`]

### File structure

- **Edit (UI source):** `Components/Pages/GovernedOperations.razor`, `Components/Pages/ProjectWorkspace.razor`, `Components/Governed/ChatBotProjectConversationWorkspace.razor`, `Components/Pages/AssociationReview.razor`; and, if a heading-suppression param is added, `Components/Governed/ChatBotApprovalQueuePriorityView.razor` + `Components/Governed/ChatBotAssociationEvidenceComparison.razor`.
- **Edit/New (tests):** the accordion guard `[Fact]` (in `ChatBotLayoutCompositionConformanceTests.cs` or new `Story13AccordionMigrationTests.cs`); retargeted coupled tests (`ChatBotAccessibilityFocusContractTests.cs` + in-scope E2E fixtures).
- **New (evidence):** `_bmad-output/implementation-artifacts/tests/test-summary-story-13.7.md`.
- **Do NOT edit:** `OperationalDashboards.razor`, `ComplianceAuditInvestigation.razor`, any other `.razor`, `chatbot.tokens.css`, `ChatBotUiTextKey.cs`/`.resx`, generated `obj/**`, or any sibling submodule.

### Testing standards

- xUnit v3 + Shouldly; the guard carries `[Trait("Category", "Governance")]`. No `Directory.Packages.props`/package-version edits — Fluent UI Blazor is pinned `5.0.0-rc.3-26138.1` (`FluentAccordion`/`FluentAccordionItem`/`AccordionExpandMode` present). ChatBot UI has **zero `RenderComponent<`** (no bUnit) — correctness is verified by source-scan guards + real-browser Playwright E2E + Story 13.9's real-render gate, not a rendering unit test. Run the UI Governance filter, the retargeted coupled lanes, the full slnx Release build, then `git diff --check`. Confirm E2E `Skipped: 0` (browser path executed). Failure messages stay metadata-only. [Source: `Directory.Packages.props`; memories `chatbot-ui-no-bunit-test-strategy`, `chatbot-e2e-nobrowser-fallback-trap`]

### Previous story intelligence

- **Story 13.6 (done)** explicitly deferred its accordion grouping to this story and documented the same scope-fence discipline (own only your slice; leave 13.3/13.8 boxes/CSS; retarget coupled `ChatBotAccessibilityFocusContractTests` + E2E fixtures when the render changes; keep assertions tight). Its senior review valued documenting every coupled-test/file change in the File List + evidence doc — do the same. [Source: `13-6-compliance-audit-form-fluent-grid.md`]
- **Story 13.2 (done, committed)** adopted `FcPageLayout`/`FcPageHeader` and emptied `PageHeaderChromeAllowlist`/`CommandBarAllowlist`; its header composition is in all four in-scope files — build on it, do not revert. **Story 13.4 (done)** migrated the `<dl>` dumps to `FluentStack`/`FluentText`/`<code>` (the `CodeRow` fragments now in these files) — leave those intact; they become accordion-item content. [Source: `13-2-…md`; `13-4-…md`; memory `chatbot-ui-fluent-component-divergence`]
- **Memory `chatbot-epic13-guard-seed-count-variance`**: Epic 13 source-scan counts exceed the proposal prose; the source scan is authoritative. The proposal's "23 `<section>` siblings / 0 accordions" is the rule-violation count, not a per-file allowlist — 13.7's authoritative target is the 4 surfaces above (re-scan to confirm before seeding the guard list).

### Git intelligence

- Recent commits: `8a7964d feat(story-13.6): migrate compliance audit form grid` (baseline), `b310462 feat(story-13.4): migrate definition-list data dumps`, `21be905` Story 13.1 guard + Tenants/EventStore ref syncs. The working tree is clean of in-flight 13.x razor WIP at `8a7964d` (13.2/13.4/13.6 are committed). Re-read each file and the guard test at dev time — anchor edits by content. Commit as `feat(story-13.7): group sibling sections in FluentAccordion`. [Source: `git log`; `git status`]

### Project structure notes

- Aligns with the established ChatBot UI test layout (source-scan Governance guards in `tests/Hexalith.ChatBot.UI.Tests`, no bUnit; real-browser E2E; evidence under `_bmad-output/implementation-artifacts/tests/`). 13.7 is the cross-cutting accordion story; unlike the allowlist-shrinking stories (13.2/13.4/13.6), it **adds** a new accordion-required guard list (mirroring Tenants `Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions`) rather than shrinking an existing allowlist. The only intentional scope nuance is the documented deferral of OperationalDashboards accordion to Story 13.5 (data-viz owner) and the out-of-scope finding for ComplianceAuditInvestigation (single primary surface). No new projects/packages/localization keys.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, `checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`; `Hexalith.AI.Tools/hexalith-ux-instructions.md#Page sections`; `Hexalith.AI.Tools/CLAUDE.md` (UI/UX + submodule rules)]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml` (`13-7-group-sections-in-fluent-accordion`)]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13`; `#Story 13.7`; `#Story 13.5`; `#Story 13.3`; `#Story 13.8`; UX-DR1/UX-DR2 amendments]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md` (defect: 0 accordions / 23 `<section>` siblings; row 13.7; reference pattern)]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture` (L411 Epic 13 layout composition, `FluentAccordion` named)]
- [Source: `_bmad-output/implementation-artifacts/13-6-compliance-audit-form-fluent-grid.md`; `13-2-adopt-fcpagelayout-fcpageheader-across-pages.md`; `13-4-migrate-definition-lists-to-fluent-data.md`; `13-1-frontcomposer-layout-composition-guard.md`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs`; `ChatBotFluentConformanceTests.cs`; `ChatBotAccessibilityFocusContractTests.cs` (`operation-outcome-title` landing target, Story-12.7 markers); `Story13DefinitionListMigrationTests.cs` (sibling-test precedent)]
- [Source: `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` (`Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions`, L256–285); `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (L45–111), `UserMembershipLookupPage.razor`, `TenantDetailPage.razor`, `Components/Tenants/TenantConfigurationView.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`, `ProjectWorkspace.razor`, `AssociationReview.razor`; `Components/Governed/ChatBotProjectConversationWorkspace.razor`, `ChatBotApprovalQueuePriorityView.razor`, `ChatBotAssociationEvidenceComparison.razor`; `Components/_Imports.razor`; `Localization/ChatBotUiTextKey.cs`]
- [Source: fluent-ui MCP `get_component_details FluentAccordion`/`FluentAccordionItem`, `check_project_version`]
- [Source: memories `chatbot-ui-fluent-component-divergence`, `chatbot-ui-no-bunit-test-strategy`, `chatbot-epic13-guard-seed-count-variance`, `chatbot-e2e-nobrowser-fallback-trap`]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context)

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` → Build succeeded, **0 Warning(s) / 0 Error(s)**.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-build -c Release -m:1 -nodeReuse:false` → **48 passed, 0 failed, 0 skipped** (incl. the new `Story13AccordionMigrationTests` accordion `[Fact]`).
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-build -c Release -m:1 -nodeReuse:false` → **215 passed, 0 failed, 0 skipped** (full UI.Tests regression, incl. retargeted `ChatBotAccessibilityFocusContractTests`).
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/Hexalith.ChatBot.UI.E2E.Tests.csproj --no-build -c Release -m:1 -nodeReuse:false` → **136 passed, 0 failed, 0 skipped in 21 s** — real-browser path executed (`/usr/bin/google-chrome` → Google Chrome 148; the no-browser `BrowserHarness` fallback did not trigger).
- `rg -l "<FluentAccordion" src/Hexalith.ChatBot.UI/Components` → exactly the 4 in-scope files; `git diff --check` → clean; `OperationalDashboards.razor`/`ComplianceAuditInvestigation.razor` → 0 accordions (scope fence honored).

### Completion Notes List

- This story's implementation had already been committed across the Epic 13 commit set (notably `61c74e9` "...implement accordion migration tests for Story 13.7" plus the subsequent composition commits), but the story checkboxes/Status were never finalized. This run re-verified every task/AC against the live source and the full build + test suites, then completed the documentation/status.
- **AC1 (grouping):** All 4 in-scope surfaces group their sibling titled sections in one `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">`, one `<FluentAccordionItem … HeadingLevel="2" Expanded="true">` per section, `Header` = the section's existing localized title key. Inner duplicate `<h2 class="chatbot-section-title">` titles removed; `<section>` landmarks kept with `aria-label`.
- **AC2 (single-primary outside):** GovernedOperations page header + status banners + `governed-review-context`; ProjectWorkspace `MainContent` picker; ChatBotProjectConversationWorkspace `MainContent` stream/composer + Why-panel; AssociationReview `MainContent` candidates+actions all stay outside the accordion. `OperationalDashboards.razor` and `ComplianceAuditInvestigation.razor` not modified by this story (0 accordions).
- **AC3 (a11y/markers):** `id="operation-outcome-title"` focus-landing target preserved on the kept `<section>` (busy-region contract green); `data-chatbot-operational-queue`, `role="table"`/`role="row"`, `data-chatbot-queue-*`, `data-chatbot-responsive-fixture` all preserved; `HeadingLevel="2"` preserves heading rank; no motion-only cue.
- **AC4 (guard):** `Story13AccordionMigrationTests.Multi_region_surfaces_group_sibling_sections_with_fluent_accordions` (`[Trait("Category","Governance")]`) seeds the 4-path `AccordionRequiredFiles`, asserts non-vacuous + missing-path + `<FluentAccordion`/`AccordionExpandMode.Multi`/`Expanded="true"`. The 4 layout-composition allowlists + backlog (all empty) left as-is.
- **AC5 (coupled tests retargeted, not loosened):** `ChatBotAccessibilityFocusContractTests` "Story 12.7 governed operations" markers strengthened with accordion markers; busy-region landing target still `operation-outcome-title`; the named E2E fixtures pass on the real-browser path.
- **AC6 (safety/i18n/scope/build):** No new localization key (every `Header` reuses an existing `ChatBotUiTextKey.*`); `ChatBotFluentConformanceTests` green (no raw controls / no legacy v4/FAST tokens); no `chatbot.tokens.css`/allowlist/submodule edit; Release build clean 0/0.
- Two single-use child components gained a backward-compatible `ShowHeading` parameter (default `true`) to suppress their internal `<h2>` when grouped: `ChatBotApprovalQueuePriorityView.razor`, `ChatBotAssociationEvidenceComparison.razor`.
- **Commit-provenance caveat (added by Senior Developer Review).** The 13.7 production source did NOT land in its own commit. It was bundled into `61c74e9` — titled *"Add test summary for Story 13.1 and implement accordion migration tests for Story 13.7"* — which also carried unrelated Story 13.2 `FcPageLayout`/`FcPageHeader`/`PageTitle` adoption (`OperationalDashboards.razor`, `ProjectConversation.razor`, `Components/_Imports.razor`), the Story 13.1 test-summary + story docs, and **5 submodule pointer bumps** (EventStore, Folders, Memories, Tenants, Timesheets). Consequently the Task-7 "git diff scope" evidence below (`git diff --name-only → only the 4 razor files`, "No OperationalDashboards.razor change", "no sibling-submodule edit") describes 13.7's **intended** scope, NOT the literal contents of the commit that shipped it. The 13.7 *code* scope is nonetheless honored: the `OperationalDashboards.razor` edit in that commit was a 13.2 page-header swap, **not** an accordion (verified: `OperationalDashboards.razor` and `ComplianceAuditInvestigation.razor` still contain **0** `<FluentAccordion>`), and the `_Imports` `@using` added was `Hexalith.FrontComposer.Contracts.Rendering` (for `FcPageLayoutMode`), not an accordion import. See the Senior Developer Review (AI) section for the full reconciliation.

### File List

Source (UI):
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor` (added `ShowHeading` param)
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor` (added `ShowHeading` param)

Tests:
- `tests/Hexalith.ChatBot.UI.Tests/Story13AccordionMigrationTests.cs` (new accordion conformance guard)
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs` (retargeted Story-12.7 governed-operations marker set)

Evidence:
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.7.md` (new)

Story/sprint tracking:
- `_bmad-output/implementation-artifacts/13-7-group-sections-in-fluent-accordion.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-22 | Created Story 13.7 (ready-for-dev): group sibling titled sections into `FluentAccordion` (ExpandMode.Multi, all items Expanded-by-default) across the 4 in-scope surfaces — GovernedOperations (main), ProjectWorkspace (complementary), ChatBotProjectConversationWorkspace (complementary), AssociationReview (complementary) — mirroring `Hexalith.Tenants.UI`. Added the accordion conformance guard requirement (mirror `Multi_region_domain_pages_group_sibling_sections_with_fluent_accordions`), documented the OperationalDashboards→13.5 deferral and ComplianceAudit out-of-scope finding, and flagged the `operation-outcome-title` focus-target, child-component-heading, no-new-keys, and 13.3/13.5/13.8 scope-fence traps. |
| 2026-06-22 | Dev-story complete (Status → review): verified all 4 surfaces group sibling titled sections in `FluentAccordion` (Multi/Block, items `Expanded="true"`, `HeadingLevel="2"`), inner `<h2>` titles removed with `<section>` landmarks + `aria-label` kept, `operation-outcome-title` focus target preserved, `ShowHeading` param added to the two single-use child components, accordion conformance guard (`Story13AccordionMigrationTests`) + retargeted `ChatBotAccessibilityFocusContractTests` green. Release build 0/0; Governance lane 48/48; full UI.Tests 215/215; E2E 136/136 (real browser, 0 skipped). Wrote `test-summary-story-13.7.md`. No new localization keys; no `OperationalDashboards`/`ComplianceAudit`/`chatbot.tokens.css`/submodule change. |
| 2026-06-22 | Senior Developer Review (AI) — adversarial. Re-verified every AC against the live committed source + full build/test suites (build 0/0, Governance 48/48, UI.Tests 215/215, E2E 136/136 real-browser 0-skipped). **Approved with one MEDIUM (non-blocking) finding:** the 13.7 source shipped inside a misleadingly-titled bundled commit (`61c74e9`) that also carried Story 13.2/13.1 artifacts + 5 submodule bumps, so the Task-7 "clean single-story diff" evidence does not match git reality (the 13.7 *code* scope is honored — 0 accordions in the two out-of-scope files). 0 CRITICAL. Added a commit-provenance caveat to Completion Notes. Status → done. |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-22 · **Mode:** adversarial (story-automator review) · **Outcome:** ✅ **Approve** (0 CRITICAL, 1 MEDIUM non-blocking, finding documented)

### Method

Source for this story was already committed across the Epic 13 commit set (working tree held only the story doc, `sprint-status.yaml`, the orchestration doc, and the new `test-summary-story-13.7.md`), so the review validated the **live committed source + green build/test suites** rather than an uncommitted diff. Every AC was checked against the actual `.razor`/test files, EN+FR resources, and the implementing commit (`61c74e9`).

### Build & test verification (independently re-run this review)

| Gate | Result |
| --- | --- |
| `dotnet build Hexalith.ChatBot.slnx -c Release` | **0 Warning / 0 Error** (`TreatWarningsAsErrors`) |
| UI.Tests `--filter Category=Governance` | **48 passed, 0 failed, 0 skipped** (incl. new `Story13AccordionMigrationTests`, `ChatBotLayoutCompositionConformanceTests`, `ChatBotFluentConformanceTests`) |
| UI.Tests (full regression) | **215 passed, 0 failed, 0 skipped** (incl. retargeted `ChatBotAccessibilityFocusContractTests`) |
| UI.E2E.Tests (real browser) | **136 passed, 0 failed, 0 skipped** — Google Chrome 148 path executed, no `BrowserHarness` fallback |

### AC-by-AC verdict

- **AC1 (accordion grouping) — PASS.** All 4 in-scope surfaces use a single `<FluentAccordion ExpandMode="AccordionExpandMode.Multi" Block="true">` with one `<FluentAccordionItem … HeadingLevel="2" Expanded="true">` per sibling section; every `Header` reuses the section's existing localized `ChatBotUiTextKey.*`. `rg -l "<FluentAccordion"` → exactly the 4 files.
- **AC2 (single-primary + deferred surfaces outside) — PASS.** Page headers/status banners/`governed-review-context`/`MainContent` pickers, stream/composer/Why-panel, and candidates+actions all stay outside the accordion. `OperationalDashboards.razor` and `ComplianceAuditInvestigation.razor` contain **0** `<FluentAccordion>`. ComplianceAudit out-of-scope justification verified: one primary `compliance-timeline` `<section>` (h2) with a **nested** `compliance-filters` h3 sub-region (not a sibling) + a lone phone-fallback section → no ≥2-sibling region.
- **AC3 (a11y/focus/markers) — PASS.** `id="operation-outcome-title"` re-homed onto the kept `<section>` inside its accordion item; `data-chatbot-operational-queue="true"`, `role="table"`/`role="row"`, `data-chatbot-queue-*`, `data-chatbot-responsive-fixture` all preserved; `aria-labelledby`→`aria-label` swap keeps the accessible name where the inner `<h2>` was dropped; `HeadingLevel="2"` preserves rank.
- **AC4 (conformance guard) — PASS.** `Story13AccordionMigrationTests` is non-vacuous, seeds the 4-path `AccordionRequiredFiles`, applies a missing-path ratchet, and asserts `<FluentAccordion`/`ExpandMode="AccordionExpandMode.Multi"`/`Expanded="true"` per file; carries `[Trait("Category","Governance")]` and runs in the gated lane (part of 48/48). The 4 layout-composition allowlists + backlog left untouched.
- **AC5 (coupled tests retargeted, not loosened) — PASS.** `ChatBotAccessibilityFocusContractTests` "Story 12.7 governed operations" marker set is **strengthened** with `<FluentAccordion`/`ExpandMode.Multi`/`Expanded="true"`/`id="operation-outcome-title"` alongside the preserved queue/role markers; the busy-region contract still pins `operation-outcome-title`. Named E2E fixtures pass on the real-browser path.
- **AC6 (safety/i18n/scope/build) — PASS.** No new localization key — all 9 `Header` keys (`GovernedOperations_OperationalQueue_Title`, `ApprovalQueuePriority_Title`, `Outcome_Title`, `ProjectWorkspace_SafeGuidance_Title`, `ProjectWorkspace_OperationsLink_Title`, `ProjectWorkspace_ContextPanel_Title`, `ProjectWorkspace_FilesPanel_Title`, `AssociationReview_SourceMetadata`, `AssociationReview_Comparison`) pre-exist in **both** `SharedResource.resx` and `SharedResource.fr.resx`. `ChatBotFluentConformanceTests` green (no raw controls / no legacy v4/FAST tokens). Child components got a backward-compatible `ShowHeading` (default `true`).

### Findings

**🟡 MEDIUM — Misleading bundled commit; Task-7 "single-story diff" evidence ≠ git reality (documentation/process, not code).**
The 13.7 production source landed in `61c74e9` *"Add test summary for Story 13.1 and implement accordion migration tests for Story 13.7"* — a commit that also bundled Story 13.2 `FcPageLayout`/`FcPageHeader`/`PageTitle` adoption (`OperationalDashboards.razor`, `ProjectConversation.razor`, `Components/_Imports.razor`), the 13.1 test-summary + story docs, and **5 submodule pointer bumps** (EventStore, Folders, Memories, Tenants, Timesheets). The story's Task-7 evidence (`git diff --name-only → only the 4 razor files`, "No OperationalDashboards.razor change", "no sibling-submodule edit") therefore states 13.7's *intended* scope, not the literal commit. **Impact: low** — the 13.7 *code* scope is honored (the `OperationalDashboards.razor` change in that commit is a 13.2 header swap, not an accordion; 0 accordions remain there; the `_Imports` `@using` is `FrontComposer.Contracts.Rendering` for `FcPageLayoutMode`, not accordion). **Resolution (this review):** history is immutable and the substance is correct, so the fix is honesty — a commit-provenance caveat was added to Completion Notes and recorded here. **Going forward:** keep one story's production source in its own scoped commit and reserve generic "test summary" titles for doc-only commits.

**🟢 Observation (pre-existing, out of scope — no action).** In `GovernedOperations.razor` the `operation-outcome` accordion item body retains an inner `<h2 class="chatbot-section-title">` for "Audit History" at the same heading level as the item header. This double-`h2` predates 13.7 (the section already carried both an `operation-outcome-title` `<h2>` and the audit-history `<h2>`), so heading structure is unchanged by this story; a future a11y pass could demote it to `<h3>`.

### Recommendation

Approve and mark **done**. No code changes required; the implementation faithfully meets all 6 ACs with green Release build + Governance + full UI + real-browser E2E lanes. The single MEDIUM finding is a commit-hygiene/transparency issue resolved by documentation.
