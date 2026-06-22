---
baseline_commit: b3104620212fe9d59744b78de9b530ae6bc5e4e6
---
# Story 13.6: Compliance audit search form — Fluent form grid

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a compliance investigator using the ChatBot governed UI,
I want the `/compliance-audit-investigation` filter fields laid out in an aligned Fluent form grid with the label sitting directly above each input,
so that I can scan and fill the FR56 query dimensions without the current inline-wrap jumble (labels detached from their inputs), while every filter, accessible label, and opaque-resource/escalation semantic is preserved exactly.

## Context

`/compliance-audit-investigation` (`ComplianceAuditInvestigation.razor`, the Story 9.3 / S9 read-and-escalate-only audit surface) is the second of the two page-owned surfaces the Epic 12/13 remediation deferred. Epic 12 migrated its leaf controls to Fluent, but its **filter form is hand-rolled**: the 12 fields live in `<div class="chatbot-form-grid">` — a class with **no CSS rule** (see `chatbot.tokens.css`) — so the fields fall back to default block flow and the separate `<FluentLabel>` + `<FluentTextInput>` pairs wrap/detach into a jumble (sprint-change-proposal-2026-06-22, defect #5: "a label with no input beside it"). The page also still renders its per-row audit metadata as a monospace `<dl class="chatbot-definition-list">` dump — the residual definition-list surface Story 13.4 explicitly deferred to this story.

This is a **rendering-layer correction only**: no backend, command-spine, query, CLI, or MCP behavior change. Reference implementation is `Hexalith.Tenants.UI` (`TenantAuditPage.razor` / `TenantsWorkspace.razor` filter grids), guarded by `DomainUiFluentConformanceTests`.

### Scope decision — this story owns TWO remediations on one file (read this first)

The epic AC for 13.6 names only the **filter form**. But the Story 13.1 layout-composition guard's own source comment is authoritative and binds 13.6 to a second obligation:

> `ChatBotLayoutCompositionConformanceTests.cs` lines 84–86: "Story 13.5 migrates OperationalDashboards.razor … and removes its entry; **Story 13.6 migrates ComplianceAuditInvestigation.razor and removes its entry**; Story 13.8 then verifies this list is empty."

Story 13.4 (done) migrated 23 `<dl>` surfaces and shrank `DefinitionListAllowlist` 25→2, deliberately leaving `ComplianceAuditInvestigation.razor` + `OperationalDashboards.razor` for the page-owner stories (13.6/13.5). No later story migrates this file's `<dl>` (13.7 = accordion, 13.8 = CSS deletion + "allowlist must be empty", 13.9 = real-render verification, which forbids `<dl>` primary-data dumps outright). **Therefore 13.6 must also migrate the audit-timeline `<dl>` and remove this file's `DefinitionListAllowlist` entry** — otherwise 13.8 cannot reach an empty allowlist and 13.9 fails.

**This story owns** (all on `ComplianceAuditInvestigation.razor` + its UI test):
1. The filter form → Fluent form grid (label-above-input). *(epic AC, primary)*
2. The audit-timeline `<dl class="chatbot-definition-list">` → Fluent data presentation, removing the `DefinitionListAllowlist` entry. *(inherited from 13.4 scope split + guard comment)*

**This story does NOT own** (leave for the named story; do not touch):
- `class="chatbot-page"` / `class="chatbot-section"` content boxes → **Story 13.3** (`PageContentBoxAllowlist` entry for this file STAYS).
- Deleting any `chatbot.tokens.css` rule → **Story 13.8**.
- Grouping the timeline/filter sibling sections in `FluentAccordion` → **Story 13.7**.
- `OperationalDashboards.razor` → **Story 13.5**.
- The `FcPageLayout`/`FcPageHeader` adoption already in the working tree → **Story 13.2** (in-progress). Build on it; do not revert it.

## Acceptance Criteria

1. **(Filter form — epic AC)** **Given** `/compliance-audit-investigation`, **When** re-composed, **Then** the 12 filter fields lay out in an aligned `FluentGrid` (`Spacing="3"`) of responsive `FluentGridItem`s (mirroring `TenantAuditPage`/`TenantsWorkspace`), the hand-rolled `<div class="chatbot-form-grid">` is gone, and each field shows its localized label **directly above** the input (Fluent v5 `Label`, `LabelPosition` defaults to `Above`) with no inline-wrap jumble / detached labels.
2. **(All filters preserved)** **Then** all 12 FR56 query dimensions remain present and functional with identical bindings — `Tenant, Actor, Command, Resource, Decision, Reason, Correlation, MessageId, Surface, From, To, Limit` — each keeping its stable `Id="compliance-filter-*"` and localized label key (`ChatBotUiTextKey.ComplianceAuditFilter*`); the `Limit` field stays a `FluentNumberInput TValue="int"`; the From/To fields keep their ISO-8601-UTC text contract (`Value="@_filters.FromUtcText"` + `ValueChanged="_filters.SetFromUtcText"`, and `To` likewise) — **no** switch to `type="datetime-local"`.
3. **(Actions + governed semantics preserved)** **Then** the search/investigation action row uses a `FluentStack` (or grid item) instead of `<div class="compliance-action-row">`; the two `FluentButton`s keep `data-chatbot-stable-id="compliance-search"` / `"compliance-trigger-investigation"` and `OnClick="SearchAsync"` / `OnClick="TriggerInvestigationAsync"`; the read/escalate-only model is untouched (operate control stays `aria-disabled="true"` with `aria-describedby="compliance-operate-denied"`; escalation uses the opaque `project-opaque-ref` target; no workflow-mutation handler appears).
4. **(Audit-timeline `<dl>` migrated — inherited obligation)** **Then** the per-row `<dl class="chatbot-definition-list">` metadata dump is migrated to structured Fluent data presentation (`FluentStack` + `FluentText`/`<code class="chatbot-code">`, mirroring Story 13.4), preserving: the row `aria-label`/`data-redaction-state`/`data-escalation-state` attributes, the safe-token values (`actor:@row.Actor`, `command:@row.Command`, `decision:@row.Decision`, `reason:@row.Reason`, `correlation:@row.Correlation`, `policy-snapshot:@row.PolicySnapshot`, `redaction:@row.Redaction`, `escalation:@row.Escalation`, `safe-next-action:@row.SafeNextAction`) as `<code>` tokens, and the localized `dt`-label text via `FluentText`. Zero `chatbot-definition-list` class tokens and zero `<dl>/<dt>/<dd>` markup remain in the file.
5. **(Guard ratchet)** **Then** `ChatBotLayoutCompositionConformanceTests.DefinitionListAllowlist` has the `"Components/Pages/ComplianceAuditInvestigation.razor"` entry **removed** (leaving only `OperationalDashboards.razor` for Story 13.5); the `PageContentBoxAllowlist` entry for this file is **left in place** (Story 13.3 owns it); no other allowlist is edited. The full Governance lane stays green.
6. **(Safety + a11y + i18n + build invariants)** **Then** no raw `<button>/<input>/<select>/<textarea>` and no legacy v4/FAST tokens are introduced (`ChatBotFluentConformanceTests` stays green); every visible string and accessible name still flows through `UiText[ChatBotUiTextKey.*]` with **no new localization keys** (reuse the existing `ComplianceAuditFilter*` keys); EN+FR localization, NFR6 landmarks/labels, UX-DR4 non-color cues, and UX-DR34 focus (`HeadingId="compliance-audit-title"`) are intact; and the Release build is clean (`TreatWarningsAsErrors`, 0/0).

## Tasks / Subtasks

- [x] **Task 1 — Migrate the filter form to a Fluent form grid (AC: 1, 2)**
  - [x] Replace `<div class="chatbot-form-grid">` (`ComplianceAuditInvestigation.razor` ~L47–121) with `<FluentGrid Spacing="3">`; put each field in a `<FluentGridItem Xs="12" Md="6" Lg="3">` (text fields) — choose `Lg` so the row aligns cleanly; the `Limit` numeric and the From/To text fields may use the same or a wider item.
  - [x] For each field, collapse the `<FluentLabel for="…" Class="chatbot-labelled-row">` + `<FluentTextInput Id="…" Class="chatbot-input" aria-label="@UiText[…]">` pair into a **single** `<FluentTextInput Id="compliance-filter-…" Label="@UiText[ChatBotUiTextKey.ComplianceAuditFilter…]" Value=… ValueChanged=… />`. Keep the `Id`; the native `Label` provides the accessible name (drop the now-redundant separate `<FluentLabel>`, `for=`, `Class="chatbot-labelled-row"`, `Class="chatbot-input"`, and the redundant `aria-label`).
  - [x] Preserve exact bindings: `Tenant/Actor/Command/Resource/Decision/Reason/Correlation/MessageId/Surface` → `Value="@_filters.X"` + `ValueChanged="@((string? value) => _filters.X = value)"`; `From/To` → `Value="@_filters.FromUtcText"/"@_filters.ToUtcText"` + `ValueChanged="_filters.SetFromUtcText"/"_filters.SetToUtcText"` (KEEP as text — do NOT add `type="datetime-local"`); `Limit` → `<FluentNumberInput TValue="int" Id="compliance-filter-limit" Label=… Value="@_filters.Limit" ValueChanged=…>`.
- [x] **Task 2 — Convert the action row to a FluentStack (AC: 3)**
  - [x] Replace `<div class="compliance-action-row">` (search/investigation row) with `<FluentStack Orientation="Orientation.Horizontal" HorizontalGap="12px" VerticalGap="12px" Wrap="true">`; keep both `FluentButton`s verbatim incl. `data-chatbot-stable-id` and `OnClick` handlers. (The two action rows inside the timeline `article` are part of Task 3.)
- [x] **Task 3 — Migrate the audit-timeline `<dl>` to Fluent data presentation (AC: 4)**
  - [x] Convert the per-row `<dl class="chatbot-definition-list">` (~L157–176) to a structured `FluentStack` of label/value rows mirroring Story 13.4's pattern (`FluentText` label + `<code class="chatbot-code">` token value); move the `<dl>`'s `aria-label` (`ComplianceAuditSafeMetadataLabel`) onto the new container; keep the `<article>` `aria-label`/`data-redaction-state`/`data-escalation-state` and the escalate/operate action row exactly.
  - [x] Verify zero residual `<dl>/<dt>/<dd>` and zero `chatbot-definition-list` tokens: `rg 'chatbot-definition-list|<dl|<dt|<dd' src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor` → no matches.
- [x] **Task 4 — Shrink the guard allowlist (AC: 5)**
  - [x] In `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs`, remove `"Components/Pages/ComplianceAuditInvestigation.razor"` from `DefinitionListAllowlist` (re-read the file first — line numbers shift with in-flight 13.2/13.5 work; edit by content). Do NOT touch `PageContentBoxAllowlist` (13.3) or any other list. The stale-entry ratchet will fail the build if the `<dl>` token is still present, so do this only after Task 3.
- [x] **Task 5 — Update the coupled surface test (AC: 1–4, 6)**
  - [x] Update `tests/Hexalith.ChatBot.UI.Tests/ComplianceAuditSurfaceTests.cs` method `SurfacePageShouldExposeAllFr56FiltersCriticalStatesAndMutationGuardrails` (L186–) so it asserts the NEW form structure: drop the `<FluentLabel` / `for="…"` / `aria-label="@UiText[…]"` assertions (L208–211); instead assert `<FluentGrid` and, per filter id, `Id="compliance-filter-…"` + `Label="@UiText[ChatBotUiTextKey.ComplianceAuditFilter…]"`. Keep all governed/mutation-guard assertions (`<FluentNumberInput TValue="int"`, `OnClick="SearchAsync"/"TriggerInvestigationAsync"`, `data-compliance-*`, `aria-disabled`, the `ShouldNotContain` mutation/`<button>`/`<input>` bans). In `SurfacePageShouldMatchBindingDomContractAndStayReadEscalateOnly` (L144–), the `actor:@row.Actor` / `safe-next-action:@row.SafeNextAction` token asserts must still pass — keep those token strings in the migrated rows (AC4).
  - [x] **(Coupled-test fallout, not in the original 4-file note)** Two further source-scan contracts hard-pinned the old form shape and had to be retargeted to keep the suite green: `ChatBotAccessibilityFocusContractTests.cs` ("Story 12.7 compliance audit investigation" markers: `<FluentLabel`/`aria-label="@UiText[…ComplianceAuditFilterTenant]"` → `<FluentGrid`/`Label="@UiText[…ComplianceAuditFilterTenant]"`) and `Story13DefinitionListMigrationTests.cs` (`PageOwnedDefinitionListSurfaces` end-state 2→1, per Dev Notes "if any assertion pins this page, update it consistently"). Both retargets stay tight (Fluent component set + localized accessible name still positively asserted), not loosened.
- [x] **Task 6 — Verify & record evidence (AC: 5, 6)**
  - [x] Build: `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` → 0 Warning / 0 Error.
  - [x] Governance lane: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-build -m:1 -nodeReuse:false` → all green, 43/0 (incl. `ChatBotLayoutCompositionConformanceTests`, `ChatBotFluentConformanceTests`, `Story13DefinitionListMigrationTests`).
  - [x] Run `ComplianceAuditSurfaceTests` → 10/0; full UI.Tests regression → 210/0.
  - [x] `git diff --check` → clean. Edited files: `ComplianceAuditInvestigation.razor`, `ChatBotLayoutCompositionConformanceTests.cs`, `ComplianceAuditSurfaceTests.cs`, plus the two coupled contracts above + the new `tests/test-summary-story-13.6.md` (the 4-file note under-enumerated the coupled contracts). Pre-existing 13.2 WIP files (`OperationalDashboards.razor`/`ProjectConversation.razor`/`_Imports.razor`) untouched.
  - [x] Write `_bmad-output/implementation-artifacts/tests/test-summary-story-13.6.md` (metadata-only evidence; same shape as `test-summary-story-13.4.md`). Live `aspire run` visual proof is deferred to Story 13.9's real-render gate; the build-enforced guard ratchet (allowlist 2→1) + retargeted surface test are this story's truth signal.

## Dev Notes

### Current state of `ComplianceAuditInvestigation.razor` (read before editing)

- **Filter form (~L43–130):** `<section class="chatbot-section compliance-audit-filters">` → `<div class="chatbot-form-grid">` holding 12 `<FluentLabel for=… Class="chatbot-labelled-row">` + `<FluentTextInput Id=… Class="chatbot-input" Value=… ValueChanged=… aria-label=…>` pairs (Limit is `FluentNumberInput TValue="int"`), then `<div class="compliance-action-row">` with the search + investigation `FluentButton`s. `chatbot-form-grid`, `chatbot-input`, `compliance-action-row`, `compliance-audit-filters` have **no CSS rule** (markers only) → today's "jumble" is literal default block flow.
- **Working tree already carries Story 13.2 (in-progress):** the `<PageTitle>` was removed and the page now wraps content in `<FcPageLayout Mode="FcPageLayoutMode.FullWidth">` with `<FcPageHeader PageTitle/Eyebrow/Heading/HeadingId="compliance-audit-title">` replacing the old `<header class="chatbot-page-header">`. **Keep this**; 13.6 edits only the inner form + timeline.
- **Audit timeline (~L145–192):** `<ol class="chatbot-audit-timeline">` → per row `<article aria-label data-redaction-state data-escalation-state>` containing an `<h3>` + the `<dl class="chatbot-definition-list">` (9 dt/dd token pairs) + a `compliance-action-row` with the escalate (`compliance-request-access`) and inert operate (`compliance-operate-disabled`) buttons. **Task 3 migrates only the `<dl>`**; keep the `<article>`, the action buttons, and the `chatbot-audit-timeline` `<ol>` (the `<ol>` is not a definition list and is out of scope).
- **`@code`:** `FilterForm` holds the 12 fields; `From/To` are `string` text (`FromUtcText`/`ToUtcText`) driven by `SetFromUtcText`/`SetToUtcText` which parse ISO-8601 as UTC (`DateTimeStyles.AssumeUniversal | AdjustToUniversal`). Do not change this contract. `SearchAsync`/`TriggerInvestigationAsync`/`RequestEscalationAsync`/`RequestPhoneEscalationAsync` and the `OpaqueEscalationTarget="project-opaque-ref"` / `InvestigationId="investigation-s9"` constants are unchanged.

### Reference pattern (mirror `Hexalith.Tenants.UI`, same pinned Fluent v5)

Form/filter grid — `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (L51–93) and `TenantsWorkspace.razor` (L46–74):

```razor
<FluentGrid Spacing="3" Class="compliance-audit-filters__layout">
    <FluentGridItem Xs="12" Md="6" Lg="3">
        <FluentTextInput Id="compliance-filter-tenant"
                         Label="@UiText[ChatBotUiTextKey.ComplianceAuditFilterTenant]"
                         Value="@_filters.Tenant"
                         ValueChanged="@((string? value) => _filters.Tenant = value)" />
    </FluentGridItem>
    @* …one FluentGridItem per filter… *@
    <FluentGridItem Xs="12" Md="6" Lg="3">
        <FluentNumberInput TValue="int"
                           Id="compliance-filter-limit"
                           Label="@UiText[ChatBotUiTextKey.ComplianceAuditFilterLimit]"
                           Value="@_filters.Limit"
                           ValueChanged="@((int value) => _filters.Limit = value)" />
    </FluentGridItem>
</FluentGrid>
```

- **Fluent v5 `Label` (verified via fluent-ui MCP for `FluentNumberInput`/`FluentTextInput`):** `Label` (string) renders the label text just above the input; `LabelPosition` defaults to `Above`. This IS the "label-above-input" the AC wants — a separate `<FluentLabel>` is unnecessary and was the source of the detach/jumble. `Id`, `AriaLabel`, `Placeholder`, `Required`, `Message` are all available if needed; you do not need them beyond `Id`+`Label`.
- A custom layout class (e.g. `compliance-audit-filters__layout` / `__actions`) on `FluentGrid`/`FluentStack` is allowed (layout is the one thing the design system doesn't own), but is optional — the Fluent params (`Spacing`, `Xs/Md/Lg`, `*Gap`) handle layout. Do NOT add grid CSS to `chatbot.tokens.css` (13.8 territory); if you add a class, it must carry no banned token and no theme redefinition.
- Timeline `<dl>` → Fluent rows: mirror Story 13.4's structured `FluentStack` (vertical stack of rows; each row `FluentText` label + `<code class="chatbot-code">token</code>` value). 13.4 deliberately used `FluentStack` (not `FluentDataGrid`) for fixed key-value metadata to preserve `data-*`/`aria` markers — do the same here.

### Regression traps (the review will check these)

- **The coupled surface test WILL fail if you only edit the razor.** `ComplianceAuditSurfaceTests.SurfacePageShouldExposeAllFr56FiltersCriticalStatesAndMutationGuardrails` (L206–212) hard-asserts `<FluentLabel`, `for="compliance-filter-*"`, and `aria-label="@UiText[…]"` per filter. Retarget it (Task 5) to the new `FluentGrid`+`Label=` shape. This is part of the story, not collateral damage — update it deliberately, don't loosen it into a no-op.
- **Don't game the guard.** Removing the `DefinitionListAllowlist` entry without actually migrating the `<dl>` (Task 3) fails the offender-outside-allowlist ratchet; migrating but forgetting the entry fails the stale-entry ratchet. Both Task 3 and Task 4 are required together. The guard bans the `chatbot-definition-list` **class token**; Story 13.9's real-render gate additionally forbids bare `<dl>` primary-data dumps — so fully migrate, don't just drop the class.
- **Preserve the From/To UTC text contract.** The Tenants reference uses `type="datetime-local"`; **do not copy that here** — `SetFromUtcText`/`SetToUtcText` expect ISO-8601 UTC strings and `datetime-local` emits a different, local-time format. Keep `FluentTextInput` text bindings unchanged.
- **Stay inside your scope.** Do NOT remove `class="chatbot-page"`/`class="chatbot-section"` (13.3 — leave the `PageContentBoxAllowlist` entry), do NOT delete any `chatbot.tokens.css` rule (13.8), do NOT add `FluentAccordion` (13.7), do NOT touch `OperationalDashboards.razor` (13.5), do NOT revert the 13.2 `FcPageLayout`/`FcPageHeader` working-tree edits. Keep the `data-chatbot-responsive-fixture="audit-investigation-s9"`, `data-chatbot-surface`, `data-compliance-dense-audit`, and `compliance-phone-fallback` markers (asserted by responsive/a11y contract + E2E fixtures).
- **No new localization keys.** Every `ComplianceAuditFilter*` key already exists (`ChatBotUiTextKey.cs` L864–879, and the all-keys enumeration L1719–1734). Reuse them as the `Label` value; do not invent keys or hard-code English.
- **No raw controls / no legacy tokens.** `ChatBotFluentConformanceTests` still runs — `FluentGrid`/`FluentGridItem`/`FluentStack`/`FluentText`/`FluentTextInput`/`FluentNumberInput`/`FluentButton` only; never `<input>/<button>/<select>/<textarea>` or `--type-ramp-*`/`--neutral-*`/`--accent-*`/`--neutral-fill-*`/`--palette-*`.

### Architecture & boundary guardrails

- UI may reference ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts only — never Server, Gateway, Dapr, EventStore internals, audit/idempotency/projection internals. This story touches only `Hexalith.ChatBot.UI` `.razor` + two UI test files + one evidence doc. [Source: `architecture.md#Frontend Architecture` L411; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
- Rendering-layer correction only: governed read/escalate-only semantics, NFR6 a11y, UX-DR4 non-color cues, UX-DR34 focus, EN+FR i18n, and the "no fake/freeform textbox" safety model are preserved exactly. UX-DR2 / Epic 13 require primary data via Fluent components and page composition via `FcPageLayout`+`FcPageHeader`; the allowlist must reach **empty** at Epic 13 completion. [Source: `architecture.md#Frontend Architecture` L411; `epics.md#Epic 13`]

### File structure

- **Edit (UI source):** `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor`.
- **Edit (tests):** `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (drop 1 `DefinitionListAllowlist` entry); `tests/Hexalith.ChatBot.UI.Tests/ComplianceAuditSurfaceTests.cs` (retarget the form-structure asserts).
- **New (evidence):** `_bmad-output/implementation-artifacts/tests/test-summary-story-13.6.md`.
- **Do NOT edit:** `OperationalDashboards.razor`, any other `.razor`, `chatbot.tokens.css`, `ChatBotUiTextKey.cs` / `.resx` localization, generated files under `obj/**`, the static E2E fixtures, or any sibling submodule.

### Testing standards

- xUnit v3 + Shouldly; guard carries `[Trait("Category", "Governance")]`. No package-version edits — central versions pin Fluent UI Blazor `5.0.0-rc.3-26138.1` (`FluentGrid`/`FluentGridItem`/`FluentStack`/`FluentText` all present), xUnit v3, Shouldly. ChatBot UI has **zero `RenderComponent<`** (no bUnit) — correctness is verified by source-scan guards (`ComplianceAuditSurfaceTests`, the layout guard) + Story 13.9's real-render gate, not by a rendering unit test. Run the UI Governance filter, then the `ComplianceAuditSurfaceTests` lane, then the full slnx build, then `git diff --check`. Failure messages stay metadata-only. [Source: `Directory.Packages.props`; memory `chatbot-ui-no-bunit-test-strategy`]

### Latest technical information

- `FluentNumberInput`/`FluentTextInput` (Fluent v5, `FluentInputImmediateBase`/`FluentInputBase`) expose `Label` (string, label text above the input), `LabelPosition` (default `Above`), `Id`, `AriaLabel`, `Placeholder`, `Required`, `Message`. Confirmed against the fluent-ui MCP (server `5.0.0.26139`; project pins `5.0.0-rc.3-26138.1` — same RC line, `Label`/`Id` are stable and are already used by `Hexalith.Tenants.UI` on the same pin). `FluentGrid`/`FluentGridItem` use `Spacing` (int) and `Xs/Sm/Md/Lg/Xl` (int 1–12) breakpoints. No new `@using` needed — `Components/_Imports.razor` already imports `Microsoft.FluentUI.AspNetCore.Components`. [Source: fluent-ui MCP `get_component_details FluentNumberInput`; `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]

### Previous story intelligence

- **Story 13.4 (done)** established the `<dl>` → `FluentStack`+`FluentText`/`<code>` migration: keep `<code class="chatbot-code">` only for genuine opaque tokens (ids/correlations/snapshots/decision codes), plain `FluentText` for prose/labels, drop monospace for non-code values, move `aria-label` onto the new container, keep `@if`/null guards. 13.4 deliberately did NOT touch this page's `<dl>` and shrank `DefinitionListAllowlist` 25→2 — this story finishes its half. 13.4's `Story13DefinitionListMigrationTests` scans the 23 migrated files; if any assertion pins this page, update it consistently, otherwise leave it.
- **Story 13.2 (in-progress)** adopted `FcPageLayout`/`FcPageHeader` across the pages (its edits to this file are in the working tree, uncommitted). It emptied `PageHeaderChromeAllowlist`/`CommandBarAllowlist` in the guard. Coordinate: 13.6's edits are strictly inside the inner form/timeline, no overlap with the header/command-bar region 13.2 owns. [Source: `13-4-migrate-definition-lists-to-fluent-data.md`; `13-2-adopt-fcpagelayout-fcpageheader-across-pages.md`; memory `chatbot-ui-fluent-component-divergence`]

### Git intelligence

- Recent commits: `b310462 feat(story-13.4): migrate definition-list data dumps` (the prior page-data work), `21be905`/`648e101`/`face7c7` EventStore-Admin + submodule-ref syncs + the Story 13.1 guard. The working tree carries Story 13.2 WIP on `ComplianceAuditInvestigation.razor`, `OperationalDashboards.razor`, `ProjectConversation.razor`, `_Imports.razor`. Re-read the guard test and the page at dev time — line numbers cited here will have drifted with in-flight 13.2/13.5 work; edit by content/anchor, not by line number. [Source: `git log`; `git status`]

### Project structure notes

- Aligns with the established ChatBot UI test layout (source-scan governance guards in `tests/Hexalith.ChatBot.UI.Tests`, no bUnit; evidence under `_bmad-output/implementation-artifacts/tests/`). 13.6 is the third Epic 13 story to shrink an allowlist (after 13.2 emptied two and 13.4 shrank the dl list 25→2). No new projects/packages; the only intentional scope nuance is the documented two-remediation ownership of one file (filter form + residual `<dl>`), inherited from 13.4's 23-vs-2 split.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, `checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`; `Hexalith.AI.Tools/hexalith-ux-instructions.md`; `Hexalith.AI.Tools/CLAUDE.md` (UI/UX + submodule rules)]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml` (`13-6-compliance-audit-form-fluent-grid: backlog`)]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13`; `#Story 13.6`; `#Story 13.4`; `#Story 13.5`; `#Story 13.8`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md` (defect #5 jumbled form; row 13.6; guard seed)]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture` (L381, L411 Epic 13 layout composition)]
- [Source: `_bmad-output/implementation-artifacts/13-4-migrate-definition-lists-to-fluent-data.md`; `13-2-adopt-fcpagelayout-fcpageheader-across-pages.md`; `13-1-frontcomposer-layout-composition-guard.md`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (L54–56 dl regex, L81–91 `DefinitionListAllowlist`); `ChatBotFluentConformanceTests.cs`; `ComplianceAuditSurfaceTests.cs` (L186–239); `Story13DefinitionListMigrationTests.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (L51–93); `TenantsWorkspace.razor` (L46–74); `UserMembershipLookupPage.razor` (L25–55)]
- [Source: `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor`; `Components/_Imports.razor`; `Localization/ChatBotUiTextKey.cs` (L864–879, L1719–1734); `wwwroot/css/chatbot.tokens.css`]
- [Source: fluent-ui MCP `get_component_details FluentNumberInput` (Label/LabelPosition/Id), `check_project_version`]
- [Source: memories `chatbot-ui-fluent-component-divergence`, `chatbot-ui-no-bunit-test-strategy`, `chatbot-epic13-guard-seed-count-variance`, `chatbot-e2e-nobrowser-fallback-trap`]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context)

### Debug Log References

- `dotnet build src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj -m:1 -nodeReuse:false` → 0/0.
- `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` → 0 Warning / 0 Error (TreatWarningsAsErrors).
- `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "Category=Governance" --no-build` → 43 passed / 0 failed.
- `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "FullyQualifiedName~ComplianceAuditSurfaceTests" --no-build` → 10 passed / 0 failed.
- `DiffEngine_Disabled=true dotnet test …UI.Tests… --no-build` (full regression) → 210 passed / 0 failed.
- `grep -nE 'chatbot-definition-list|<dl|<dt|<dd' …/ComplianceAuditInvestigation.razor` → no quote-bounded class token, no `<dl>/<dt>/<dd>` markup.
- `git diff --check` → clean.

### Completion Notes List

- **AC1/AC2 (filter form):** Replaced the hand-rolled `<div class="chatbot-form-grid">` with `<FluentGrid Spacing="3">` + 12 `<FluentGridItem Xs="12" Md="6" Lg="3">`. Each field collapsed to a single `FluentTextInput`/`FluentNumberInput` whose Fluent v5 native `Label` renders the localized label above the input (LabelPosition defaults to `Above`); dropped the separate `<FluentLabel for=…>`, `Class="chatbot-input"`, `Class="chatbot-labelled-row"`, and the redundant per-field `aria-label`. All 12 `Id="compliance-filter-*"` + label keys preserved; `Limit` stays `FluentNumberInput TValue="int"`; From/To keep the ISO-8601-UTC text contract (`FromUtcText`/`SetFromUtcText`) — no `datetime-local`.
- **AC3 (actions + governed semantics):** search/investigation row → `FluentStack` (Orientation.Horizontal, gaps, Wrap); both buttons verbatim (`data-chatbot-stable-id`, `OnClick`). Operate control stays `aria-disabled="true"` + `aria-describedby="compliance-operate-denied"`; escalation keeps the opaque `project-opaque-ref`; no mutation handler added. The timeline `article`'s own `compliance-action-row` (escalate/operate) kept exactly as required.
- **AC4 (audit-timeline `<dl>`):** per-row `<dl class="chatbot-definition-list">` → vertical `FluentStack`; each of the 9 rows is a horizontal `FluentStack` of `FluentText` label + `<code class="chatbot-code">` safe-token; all token strings preserved verbatim (`actor:@row.Actor` … `safe-next-action:@row.SafeNextAction`); `ComplianceAuditSafeMetadataLabel` `aria-label` moved onto the container; `<article>` markers untouched. Zero `<dl>/<dt>/<dd>` and zero quote-bounded `chatbot-definition-list` remain (the explanatory comment is worded to avoid re-tripping the guard).
- **AC5 (guard ratchet):** `DefinitionListAllowlist` 2→1 (only `OperationalDashboards.razor` left); `PageContentBoxAllowlist` untouched (Story 13.3 owns this file's entry).
- **AC6 (safety/a11y/i18n/build):** no raw `<button>/<input>/<select>/<textarea>`, no legacy tokens (`ChatBotFluentConformanceTests` green); all strings via `UiText[…]` reusing existing `ComplianceAuditFilter*` keys (no new key); `HeadingId="compliance-audit-title"`; Release build 0/0.
- **Coupled-test deviation:** beyond the 3 source files + evidence doc the story's Task 6 note listed, two further source-scan contracts hard-pinned the old form shape and had to be retargeted (else they fail Step 7): `ChatBotAccessibilityFocusContractTests.cs` (Story 12.7 markers) and `Story13DefinitionListMigrationTests.cs` (`PageOwnedDefinitionListSurfaces` end-state 2→1, explicitly authorized by Dev Notes). Both retargets stay tight. Detailed in the evidence doc's "Coupled-test retargets" table.
- **Scope fences honored:** did not touch `chatbot-page`/`chatbot-section` boxes (13.3), `chatbot.tokens.css` (13.8), `FluentAccordion` grouping (13.7), `OperationalDashboards.razor` (13.5), or revert the 13.2 `FcPageLayout`/`FcPageHeader` working-tree adoption.

### File List

- `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor` (modified)
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (modified)
- `tests/Hexalith.ChatBot.UI.Tests/ComplianceAuditSurfaceTests.cs` (modified)
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs` (modified — coupled contract retarget)
- `tests/Hexalith.ChatBot.UI.Tests/Story13DefinitionListMigrationTests.cs` (modified — coupled end-state retarget)
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs` (modified — real-browser E2E: 2 new Story-13.6 tests for the FluentGrid filter form + FluentStack timeline, plus the `BuildComplianceFixture` static fixture and `AssertAuditFilterFluentControlsAsync` helper retargeted to the migrated render)
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.6.md` (new — evidence)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — status tracking)
- `_bmad-output/implementation-artifacts/13-6-compliance-audit-form-fluent-grid.md` (modified — story tracking)

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-22 | Created Story 13.6 (ready-for-dev): migrate the `/compliance-audit-investigation` filter form to an aligned `FluentGrid`/`FluentGridItem` label-above-input grid (Fluent v5 `Label`), convert the action row to `FluentStack`, and — inheriting the 13.4 scope split + guard comment — migrate the residual audit-timeline `<dl class="chatbot-definition-list">` to structured `FluentStack`/`FluentText`/`<code>` and remove this file's `DefinitionListAllowlist` entry. Documented the two-remediation ownership, the coupled `ComplianceAuditSurfaceTests` retarget, the From/To UTC-text (no `datetime-local`) trap, and the 13.3/13.7/13.8 scope fences. |
| 2026-06-22 | Implemented Story 13.6 (→ review). Filter form → `FluentGrid` of 12 label-above-input `FluentGridItem`s; action row → `FluentStack`; audit-timeline `<dl>` → structured `FluentStack`/`FluentText`/`<code>` with all 9 safe tokens + container `aria-label` preserved. `DefinitionListAllowlist` 2→1. Retargeted `ComplianceAuditSurfaceTests` to the new grid/`Label=` shape, and (coupled fallout) retargeted `ChatBotAccessibilityFocusContractTests` Story-12.7 markers + `Story13DefinitionListMigrationTests` end-state 2→1. Release build 0/0; Governance 43/0; surface 10/0; full UI.Tests 210/0; `git diff --check` clean. From/To UTC-text contract kept (no `datetime-local`); no new localization key; 13.3/13.7/13.8 scope fences and 13.2 working-tree adoption preserved. |
| 2026-06-22 | Senior Developer Review (AI) — outcome **Approve**. All 6 ACs and Tasks 1–6 verified against the working tree; build + Governance/surface/full-UI/E2E suites re-run green (incl. real-browser E2E, `Skipped: 0`). Corrected a doc miscount (the filter form has **12** dimensions, not 13) across AC1/AC2/Dev Notes, and documented the previously-undocumented `ComplianceAdministrationE2ETests.cs` change in the File List + evidence doc (see review notes below). No code changes required. |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-22 · **Outcome:** Approve (status → done)

Adversarial source-and-test review of the working-tree implementation. Every claim in the Dev Agent Record was independently re-validated (not trusted): the page was read in full, the four UI-test retargets were diffed, the guard ratchet/regex were inspected, and the build plus four test scopes were re-run from a clean state.

### Re-validation evidence (all re-run this review)

| Check | Result |
| --- | --- |
| `dotnet build src/Hexalith.ChatBot.UI -c Release` | 0 Warning / 0 Error |
| Governance lane + `ComplianceAuditSurfaceTests` | 53 passed / 0 failed |
| Full `Hexalith.ChatBot.UI.Tests` regression | 210 passed / 0 failed |
| `ComplianceAdministrationE2ETests` (real browser) | **5 passed / 0 failed / 0 skipped** — `Assert.Skip` did **not** trigger, so the Chromium path actually executed |
| `rg 'chatbot-definition-list\|<dl\|<dt\|<dd'` on the razor | no matches |
| `DefinitionListAllowlist` stale-entry ratchet | `OperationalDashboards.razor` still carries the token (2 hits) → ratchet stays valid |

**AC verdicts:** AC1 ✅ (`FluentGrid Spacing="3"` + 12 `FluentGridItem Xs/Md/Lg`, native `Label` above input, `chatbot-form-grid` gone), AC2 ✅ (12 stable ids + label keys; `Limit` = `FluentNumberInput TValue="int"`; From/To keep the ISO-8601-UTC text contract — E2E confirms `type` is absent and `value` ends with `Z`, i.e. no `datetime-local`), AC3 ✅ (action row → `FluentStack`; buttons verbatim; operate control inert `aria-disabled` + `aria-describedby`; opaque `project-opaque-ref`; no mutation handler), AC4 ✅ (per-row `<dl>` → structured `FluentStack`; 9 safe tokens preserved; container `aria-label` moved; zero `<dl>/<dt>/<dd>`), AC5 ✅ (`DefinitionListAllowlist` 2→1; `PageContentBoxAllowlist` untouched), AC6 ✅ (no raw controls/legacy tokens; all strings localized; no new key; `HeadingId` intact; Release 0/0).

### Findings & resolutions

- **[MEDIUM · Transparency — fixed in docs, code kept] Undocumented coupled change to `tests/Hexalith.ChatBot.UI.E2E.Tests/ComplianceAdministrationE2ETests.cs`** (+235/−50): two new Story-13.6 real-browser tests (`ComplianceAuditFilterFormShouldLayOutLabelAboveInputFluentGridWithoutDetachedLabels`, `ComplianceAuditTimelineShouldRenderSafeMetadataAsFluentStackWithoutDefinitionList`) plus the `BuildComplianceFixture` static fixture and `AssertAuditFilterFluentControlsAsync` helper retargeted to the migrated render. The change was absent from the File List, the test-summary "Changed Files", and Task 6's git-diff note, and it runs against the Dev Notes "Do NOT edit … the static E2E fixtures" fence with no deviation note. **Disposition:** the change is correct, beneficial (real-browser coverage of the migration; the fixture would otherwise be stale-showing the old `<dl>`), and passing — so it is **kept, not reverted**, and now documented in the File List + evidence doc. The fence is superseded here because reverting would re-introduce a misleading fixture and lose coverage; Story 13.9 still owns the full fixture→real-render replacement.
- **[LOW · Doc accuracy — fixed] "13 filter fields/dimensions" miscount** in AC1, AC2, and Dev Notes. The surface enumerates exactly **12** dimensions (Tenant, Actor, Command, Resource, Decision, Reason, Correlation, MessageId, Surface, From, To, Limit) — matching the 12 `FluentGridItem`s, the 12-entry surface-test dictionary, and the 12-arg `ComplianceAuditQueryModel`. Corrected to 12.
- **No CRITICAL or HIGH findings.** No task marked `[x]` was found unimplemented; no AC was missing or partial; no security/governance regression (read/escalate-only model intact, opaque escalation target preserved, no raw controls, no new localization keys, no `chatbot.tokens.css` or sibling-submodule edits).
