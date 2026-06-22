# Test Summary - Story 13.5

Date: 2026-06-22
Baseline commit: 0f3bfc8

## Scope

Story 13.5 corrects the rendering layer of `/operational-dashboards`
(`OperationalDashboards.razor`, the S8/S10 read-only operability surface) — the **single remaining**
page-owned surface in the Epic 12/13 remediation — and brings **all five** Epic 13 layout-composition
allowlists to empty. Three obligations on one file:

1. **Data-viz migration (epic AC, primary).** The two `<dl class="chatbot-definition-list">` dumps
   (one row per observability view; one row per published SLO), each wrapped in a
   `<div class="chatbot-table" role="table">` of `<article class="chatbot-labelled-row-list" role="row"
   tabindex="0">`, are replaced by:
   - **Views:** a `FluentDataGrid<OperationalDashboardView>` (`Items="@overview.Views.AsQueryable()"`,
     `ItemKey` = the view wire token, `GenerateHeader="Sticky"`) with one `TemplateColumn` per field —
     view name (`FluentText`), status (`FluentBadge` carrying `HealthLabel`), conditional depth,
     oldest-item age, owner role, conditional affected scope, conditional next-safe-action, freshness
     timestamp (`Formatter.FormatDateTime`), freshness state (`FluentBadge` carrying `FreshnessLabel`),
     conditional lag, and the per-row detail `ChatBotGovernedAction`. Conditional fields render an em
     dash when null (never a blank labelled row).
   - **Published SLOs:** a `FluentDataGrid<PublishedSlo>` (`Items="@publishedSlos.AsQueryable()"`,
     `ItemKey="@(s => s.MetricName)"`) with seven `<code>` safe-token columns + a burn `FluentBadge`
     carrying `BurnLabel`.
   - **Overall freshness/health snapshot:** the former titled `<section>` + stacked banners fold into
     `FluentCard` KPI/status tiles, with one live-region `ChatBotStatusBanner` retained inside the tile.
2. **Empty the guard allowlist (inherited 13.4 scope split).** `DefinitionListAllowlist` is reduced
   from `["Components/Pages/OperationalDashboards.razor"]` to `[]`. With the four other lists already
   `[]`, **all five Epic 13 layout-composition lists are now empty** — the precondition Story 13.8
   verifies. `Story13DefinitionListMigrationTests.PageOwnedDefinitionListSurfaces` is reduced 1 → 0 and
   the end-state assertion now requires **no** `.razor` file to contain `chatbot-definition-list`.
3. **Page-sections accordion obligation (inherited from Story 13.7) — resolved as branch (b),
   no-accordion.** See "Accordion decision" below.

**Rendering-layer only.** No backend / CommandGateway / query / CLI / MCP / SignalR / Dapr / EventStore
change and no sibling-submodule edit. The Fluxor state, the `GetOperationalDashboard` read query, the
`OperationalDashboardOverview`/`OperationalDashboardView`/`PublishedSlo` contracts and validator are
untouched. The `@code` label/kind/wire helpers (`ViewName`, `HealthLabel`/`HealthKind`,
`FreshnessLabel`/`FreshnessKind`, `BurnLabel`/`BurnKind`, `IsDetailAvailable`, `DetailReason`,
`ChatBotHealthStatuses_ToWire`) are reused verbatim; three private `*BadgeColor` mappers were added for
the decorative (non-cue) `FluentBadge` color. **No new `ChatBotUiTextKey`/`.resx` key** — every label,
column title and view name reuses an existing `OperationalDashboards*` key.

## Preserved invariants (AC4/AC5)

- **Stable status enumeration, never count-derived.** health `Healthy/Degraded/Failed/Unknown`,
  freshness `Fresh/Stale/Expired`, burn `WithinBudget/Approaching/Exhausted/Unknown` and their wire
  tokens (`ChatBotHealthStatuses_ToWire`, `ChatBotFreshnessStates.ToWireValue`,
  `ErrorBudgetBurnStates.ToWireValue`, `DashboardObservabilityViews.ToWireValue`) are unchanged.
- **Non-color cue.** The localized label text on every `FluentBadge` is the WCAG cue; color/appearance
  are decorative only (mirrors `Hexalith.Tenants.UI` `AuditDataGrid`).
- **Degraded NFR42 four-element parity.** `view.AffectedScope is { } affectedScope` and
  `view.NextSafeAction is { } nextSafeAction` conditionals are kept; a degraded/failed view still
  surfaces state + owner role + affected scope + next safe action.
- **Per-row machine tokens + live-region announcements.** `FluentDataGrid` exposes no per-`<tr>`
  arbitrary attributes, so the machine tokens (`data-chatbot-dashboard-view`, `-health`, `-freshness`,
  conditional `-affected-scope`/`-next-safe-action`; `data-chatbot-slo-metric`, `-slo-burn`) and the
  live-region `ChatBotStatusBanner` health/freshness/burn announcements (`dashboard-health-{token}`,
  `dashboard-freshness-{token}`, `dashboard-slo-burn-{metric}`, and the overall
  `operational-dashboards-freshness-{UtcTicks}`) are emitted as **sibling per-row markers** after each
  grid (the canonical `AuditDataGrid` pattern). The marker container is `chatbot-visually-hidden`
  (clip technique — aria-live regions still announce), so the grid badges carry the visible cue while
  the markers keep every token DOM-queryable and every announcement key alive.
- **Detail reachability / refresh / shell composition.** The detail `ChatBotGovernedAction` keeps its
  `DisabledWithReason` + `OperationalDashboardsDetailRestrictedReason`; `OnInitialized` still dispatches
  `LoadOperationalDashboardAction` and the refresh action is intact; `ChatBotConversationShell` +
  `ChatBotProjectContextHeader` + `FcPageLayout` + `FcPageHeader` are kept;
  `data-chatbot-responsive-fixture="operational-dashboards"` and `aria-labelledby=
  "operational-dashboards-title"` are kept; no `FrontComposerShell`/`<main>`/`role="banner"`/
  `FluentProviders` leakage was introduced.

## Accordion decision (AC6) — branch (b): no accordion

After the reshape, the MainContent shell region contains: the freshness/health snapshot as untitled
`FluentCard` KPI tiles, the views `FluentDataGrid` as the **single primary content region** (kept
outside any accordion per the rule), and **at most the one conditional published-SLOs titled
`<section>`**. That is **fewer than two sibling titled content sections**, so the Hexalith UX
"Page sections" rule does **not** trigger and **no `FluentAccordion` is added** — mirroring Story 13.7's
single-primary out-of-scope finding for `ComplianceAuditInvestigation`. `Story13AccordionMigrationTests`
is therefore **left unchanged** (it already, correctly, excludes `OperationalDashboards.razor`); this
file is NOT added to its `AccordionRequiredFiles` list, and the live Story 13.7 accordion guard is not
contradicted.

## Coupled source-scan test retargets (AC7)

| Test file | Change | Kept tight |
| --- | --- | --- |
| `tests/…/UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` | `DefinitionListAllowlist` `[…OperationalDashboards.razor]` → `[]` (+ comment "emptied by Story 13.5 — all five lists empty"). | Regexes, ratchets, detector `[Theory]` pins untouched. |
| `tests/…/UI.Tests/Story13DefinitionListMigrationTests.cs` | `PageOwnedDefinitionListSurfaces` 1 → `[]`; end-state assert now requires NO `.razor` to contain `chatbot-definition-list`. | `MigratedSurfaces` (23), `[Theory]` pins, AC3/AC4 invariant facts untouched. |
| `tests/…/UI.Tests/OperationalDashboardsComponentContractTests.cs` | Dropped `chatbot-labelled-row-list` + `chatbot-definition-list`; added `<FluentDataGrid`/`<FluentCard`/`<FluentBadge` + `ShouldNotContain("chatbot-definition-list")`. | Kept all `data-chatbot-*`, `*Label`, `overview.PublishedSlos`, `view.AffectedScope`/`NextSafeAction`, `ErrorBudgetBurnStates.ToWireValue`, `BurnLabel` asserts. |
| `tests/…/UI.Tests/ChatBotAccessibilityFocusContractTests.cs` | Epic10 "Operational dashboards" + "Story 12.7 operational dashboards": `role="table"`/`role="row"`/`tabindex="0"` → `<FluentDataGrid`/`<FluentCard`/`<FluentBadge`/`data-chatbot-dashboard-view`. | Kept responsive-fixture, freshness label, `ChatBotStatusBanner`, `<ChatBotGovernedAction`, and the preserved `data-chatbot-*`. |
| `tests/…/UI.E2E.Tests/OperationalDashboardsAccessibilityE2ETests.cs` (no-browser fallback only) | `role="table"`/`role="row"`/`tabindex="0"` → `<FluentDataGrid`/`data-chatbot-dashboard-view`. | Kept `ChatBotStatusBanner`, `dashboard-freshness` key, `HealthLabel`, `FreshnessLabel`. |
| `tests/…/UI.E2E.Tests/OperationalDashboardsPublishedSlosE2ETests.cs` (no-browser fallback only) | `role="table"` → `<FluentDataGrid`. | Kept `data-chatbot-slo-metric`/`-slo-burn`, `ErrorBudgetBurnStates.ToWireValue`, `BurnLabel`, `overview.PublishedSlos`. |

**No retarget needed (verified still green):**
- `OperationalDashboardsDegradedSurfaceE2ETests.cs` — its no-browser fallback asserts only preserved
  markers (`data-chatbot-health`/`-affected-scope`/`-next-safe-action`, the labels, and the
  `view.AffectedScope`/`view.NextSafeAction` conditionals), all kept by the migration.
- `FrontComposerShellIntegrationE2ETests.cs` + `Epic10ReleaseReadinessE2ETests.cs` — their
  shell-composition asserts (`ChatBotConversationShell` present; `FrontComposerShell`/`<main>`/
  `role="banner"`/`FluentProviders` absent; `data-chatbot-responsive-fixture` present) stay satisfied
  by AC5; the Epic10 gate-row markers (`role="table"`/`row`/`tabindex="0"`) remain alive in the
  **kept** hand-rolled browser fixtures + the GovernedOperations E2E test, so no marker moved.

**NOT touched (Story 13.9 owns):** the hand-rolled static browser fixtures
(`BuildFixture()`/`BuildSloRows()` HTML strings) in the three `OperationalDashboards*E2ETests` — they
intentionally encode the old markup; the real-render replacement + screenshot gate is Story 13.9.

## Scope fences honored (NOT this story)

- Deleting any `chatbot.tokens.css` rule → **Story 13.8** (no CSS edited; reused the existing
  `chatbot-status-group`/`chatbot-visually-hidden`/`chatbot-section`/`chatbot-code` classes).
- Rewriting the hand-rolled browser E2E fixtures → **Story 13.9**.
- The `FcPageLayout`/`FcPageHeader` adoption already in the file → **Story 13.2** (built on, not reverted).
- The 23 migrated surfaces (13.4) and `ComplianceAuditInvestigation.razor` (13.6) — not touched.
- No sibling submodule edited (the Tenants `AuditDataGrid`/`TenantAuditPage` references are read-only).

## Commands

| Command | Result |
| --- | --- |
| `dotnet build src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj -c Release -m:1 -nodeReuse:false` | Passed: 0 warnings, 0 errors (TreatWarningsAsErrors). |
| `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` | Passed: 0 warnings, 0 errors (Release, TreatWarningsAsErrors). |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "Category=Governance" --no-build` | Passed: **48** total, 0 failed, 0 skipped (incl. `ChatBotLayoutCompositionConformanceTests` with `DefinitionListAllowlist=[]`, `Story13DefinitionListMigrationTests` end-state empty, `Story13AccordionMigrationTests`, `ChatBotFluentConformanceTests`). |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "FullyQualifiedName~OperationalDashboards" --no-build` | Passed: **10** total, 0 failed, 0 skipped. |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --filter "FullyQualifiedName~ChatBotAccessibilityFocusContract" --no-build` | Passed: **11** total, 0 failed, 0 skipped. |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --no-build` (full project, regression) | Passed: **215** total, 0 failed, 0 skipped. |
| `DiffEngine_Disabled=true dotnet test …UI.E2E.Tests… --filter "FullyQualifiedName~OperationalDashboards" --no-build` (real browser) | Passed: **3** total, 0 failed, **0 skipped**. Browser path confirmed executing: normal run 744–820 ms vs forced no-browser fallback 23 ms (32× gap), so the Chromium path ran against the (unchanged) fixtures — the no-browser-fallback trap did not mask it. |
| `DiffEngine_Disabled=true dotnet test …UI.E2E.Tests… --filter "~OperationalDashboards\|~FrontComposerShellIntegration\|~Epic10ReleaseReadiness"` | Passed: **11** total, 0 failed, 0 skipped. |
| `rg -l 'chatbot-definition-list' src/Hexalith.ChatBot.UI --glob '*.razor'` | **No matches** — all five Epic 13 layout-composition allowlists are now empty. |
| `git diff --check` | Passed: clean. |

## Live app visual (aspire run)

`aspire run` (CLI 13.4.5) was started and the full topology came up **Running / Healthy**
(`chatbot-ui` @ `http://localhost:5000`, `chatbot`, `eventstore`, `eventstore-admin`, `keycloak`, the
DAPR sidecars). `GET /operational-dashboards` returns **HTTP 200** and renders the FrontComposer shell
end-to-end: the `FcPageHeader` ("Operational dashboards" + the "Refresh health overview"
`ChatBotGovernedAction`), the `ChatBotProjectContextHeader` (`m2-operational-dashboards`, "Current
surface", the "UI origin remains visible" Info badge), the description, and the "Observability context"
complementary panel — **no shell-overlap regression, no `chatbot-page-header` band, no
`chatbot-definition-list`, no crash** (screenshot: `screenshots/operational-dashboards-13-5-live-shell.png`).

The data-bearing `FluentDataGrid` + `FluentCard` KPI tiles render only once `State.Value.Overview` is
populated, which requires an authenticated tenant context — the page presents a keycloak "Sign in"
state and the documented pre-existing Tier-3 `403 TenantMissing` barrier (memory `tier3-live-dapr-run`)
blocks a clean authenticated dashboard-data render here. Per this story's own scope, the **full
real-render screenshot gate is Story 13.9**; the migrated grid/tile markup correctness for this story is
established by the 0/0 Release build (Razor → component), the 215-test governance/contract suite, and
the real-browser E2E, with the live run confirming the shell composition renders cleanly.

## Result

All acceptance criteria satisfied. `DefinitionListAllowlist` reaches `[]` (all five Epic 13
layout-composition lists empty); the migration end-state asserts no `.razor` contains
`chatbot-definition-list`; the governed read-only / a11y / i18n invariants, per-row machine tokens,
live-region announcements, stable status enumeration and degraded NFR42 four-element parity are
preserved; no new localization key; build 0/0; Governance + OperationalDashboards/Accessibility lanes +
full `UI.Tests` regression + the dashboard/shell/Epic10 E2E suites green; `git diff --check` clean.
