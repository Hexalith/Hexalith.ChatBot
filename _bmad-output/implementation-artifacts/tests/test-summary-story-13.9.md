# Test Summary — Story 13.9: Real-render cross-surface re-verification

**Date:** 2026-06-22 (updated 2026-06-23 after senior review auto-fix)
**Scope:** rendering / test / evidence — plus **one root-cause production fix surfaced by this very lane** (see "Senior review fix" below). No backend, command-spine, CLI, MCP, localization-resource, sibling-submodule pointer, or server-logic change.

## ⚠️ Senior review fix (2026-06-23) — the real-render lane did its job

The first pass captured 42 screenshots and went green, but **the screenshots showed a broken layout** (shell header overlapping the page content, hard-bordered/cramped components) and the coarse DOM/geometry assertions passed over it. Root cause: `src/Hexalith.ChatBot.UI/Components/App.razor` linked **no scoped CSS bundle**, so the Fluent UI + FrontComposer.Shell + app scoped stylesheets — including the `FluentLayout` CSS grid — never loaded in the real app (the reference `Hexalith.Tenants.UI` links it; ChatBot did not). A live-host probe confirmed `.fluent-layout` had no grid and only the non-scoped sheets loaded.

**Fixed:** `App.razor` now links `Hexalith.ChatBot.UI.styles.css` (it `@import`s the RCL bundles via their fingerprinted/servable paths; the non-fingerprinted `_content/*.bundle.scp.css` aliases 404 under this app's `UseStaticFiles` pipeline, so the app bundle is the correct single link). **Gate hardened:** the lane now asserts `.fluent-layout` computes `display:grid` per surface, so a collapsed/un-styled render fails instead of passing. **Evidence corrected:** all 42 screenshots were regenerated against the fixed render and now show the correctly-composed shell (48px header band, content below, real two-column composition, footer). All gates re-run green (Release 0/0, real-render 3/0-skip real Chromium, full UI E2E 139/0-skip, UI.Tests 227/0-skip, `git diff --check` clean).

## What this story proves

Story 12.9 verified the ChatBot UI against hand-authored HTML strings (`Page.SetContentAsync(...)`), which is why it missed the FrontComposer shell overlap. Story 13.9 is the closing gate: it boots the **actual `Hexalith.ChatBot.UI` Blazor Server app** on a loopback Kestrel socket, drives a **real Chromium browser** with `Page.GotoAsync(...)` across the six routable surfaces, and asserts — against the live FrontComposer shell + real routable Razor components + real Fluent components + real CSS cascade — that the Epic 13 layout-composition fixes hold.

## Host mechanism (AC1)

- **`LiveChatBotUiHost`** (`tests/Hexalith.ChatBot.UI.E2E.Tests/LiveChatBotUiHost.cs`) extends `WebApplicationFactory<Program>` over the real UI entry point (`src/Hexalith.ChatBot.UI/Program.cs`, `public partial class Program`).
- Because Playwright cannot reach `WebApplicationFactory`'s in-memory `TestServer` through a browser socket, the factory stands up a **second, real Kestrel host** on `http://127.0.0.1:0` (OS-assigned free port) using the documented minimal-hosting double-host pattern, and exposes that loopback address via `BaseUri`. No `Page.SetContentAsync(...)`, no `TestServer.BaseAddress`, no static HTML.
- Environment is `Development` with `UseStaticWebAssets()` so Fluent UI `_content/*` assets and the UI `wwwroot` are served exactly as at runtime.
- **Determinism seam:** the *only* overridden service is `IChatBotClient`, replaced by `FakeChatBotClient` (`FakeChatBotClient.cs`) returning metadata-only safe-token DTOs for governed operations, project conversation, association review, and compliance audit. Operational dashboards reach no client method — its service assembles a fail-safe Unknown overview at the UI boundary (no fabricated health). No projection, store, Dapr, gateway, or EventStore internal is touched.
- **Browser discipline:** `RealRenderFixture` mirrors the existing `BrowserHarness` Chrome resolution/fallback. When no browser is present the tests **`Assert.SkipWhen`** with an explicit reason rather than taking a silent string-only fallback (`chatbot-e2e-nobrowser-fallback-trap`). This evidence run used the real Chromium path: **`Skipped: 0`**.

## Route matrix (AC2)

| Surface | Route | Heading id |
| --- | --- | --- |
| ProjectWorkspace | `/` | `project-workspace-title` |
| ProjectConversation | `/projects/project-alpha/conversation` | `project-conversation-title` |
| GovernedOperations | `/governed-operations` | `governed-operations-title` |
| OperationalDashboards | `/operational-dashboards` | `operational-dashboards-title` |
| ComplianceAuditInvestigation | `/compliance-audit-investigation` | `compliance-audit-title` |
| AssociationReview | `/association-review/01ARZ3NDEKTSV4RRFFQ69G5FAW` | `association-review-title` |

## Culture / theme / forced-colors coverage (AC5)

- **Cultures:** `en`, `fr`. Culture is driven through request localization. Critically, the Blazor **Server** interactive circuit carries culture only via the `.AspNetCore.Culture` cookie (the query string sets the prerender, but the `_blazor` WebSocket re-render has no query string), so the harness sets the standard cookie before each navigation. Verified by asserting each surface's **live FR heading differs from its EN heading** (localized resource output), e.g. `Project conversation` → `Conversation projet`, `Governed operations` → `Opérations gouvernées`.
- **Color modes:** `light`, `dark`, and `forced-colors` (high-contrast emulation via `EmulateMediaAsync` ForcedColors=Active).
- **Viewports:** desktop `1280×900` for the full matrix, plus a narrow/mobile `390×844` capture per surface.

## Screenshots (AC2)

42 screenshots under `_bmad-output/implementation-artifacts/tests/screenshots/story-13.9/`, named `{surface}.{culture}.{mode}.{width}.png`:

- 6 surfaces × {en, fr} × {light, dark, forced-colors} desktop = **36**
- 6 surfaces × en.light.mobile = **6**

EN vs FR captures are byte-distinct (e.g. `project-workspace.en.light.desktop.png` 106,348 B vs `project-workspace.fr.light.desktop.png` 111,666 B), confirming the culture genuinely flips in the live interactive render — not the Story 12.9-style identical static fixture.

## Live-DOM invariants asserted (AC3)

For every live route (real interactive DOM, not fixtures):

- **No legacy page chrome** (whole-class-token match, mirroring `ChatBotLayoutCompositionConformanceTests` so prefixed tokens like `chatbot-project-workspace` are not false positives): `.chatbot-page-header`, `.chatbot-page`, `.chatbot-command-bar`, `.chatbot-definition-list`, `.chatbot-skip-link` → **0 hits each**.
- **No primary-content `<dl>` dumps** in `#fc-main-content` → **0**.
- **Real composed structures present:** FrontComposer shell skip link `a.fc-skip-link[href="#fc-main-content"]` (×1), single `#fc-main-content[role="main"]` landmark (×1), the `FcPageHeader` `h1#{heading-id}` (×1, non-empty text), and genuine Fluent components inside the content region (`fluent-card/stack/text/data-grid/grid/accordion/badge/button` count > 0).
- **FcPageLayout output** (added in the QA pass): `#fc-main-content` carries the `fc-page-layout` marker class (×1) and the `data-fc-page-layout` measure attribute (×1) for every route — closing AC3's "real DOM contains FcPageLayout **and** FcPageHeader output" for both halves, not just the header.
- **Specific Fluent composition** (added in the QA pass): the Story 13.7 grouped surfaces — `project-workspace` and `governed-operations` — render a real `fluent-accordion` inside `#fc-main-content` (Task 3 "FluentAccordion on 13.7 grouped surfaces"), proven beyond the generic any-Fluent check.
- **Conformance authority:** the five layout-composition allowlists and the not-yet-composed backlog (`PageHeaderChromeAllowlist`, `PageContentBoxAllowlist`, `CommandBarAllowlist`, `DefinitionListAllowlist`, `NotYetComposedPageBacklog`) remain **empty** (source-scanned).

## Geometry — shell overlap is gone (AC4)

- The shell header band is a 48px `FluentLayoutItem`. For each route, the `FcPageHeader` heading bounding-box top is asserted **`>= ShellHeaderBandBottom`** (anchored by the shell app-title) **and `>= 48px`**, proving the heading renders below the shell header band.
- **Control-anchored non-intersection** (added in the QA pass): rather than relying only on the abstract 48px line, the heading box top is also asserted **`>= bottom of the real `[data-testid="fc-account-menu"]` header action control`** — a direct overlap check against the right-most action cluster member (theme toggle / palette / settings / account all live inside the same band), making Task 3's "must not intersect the header action area" explicit against a live control box.
- **No hard 1px bordered content box:** computed-style check on `#fc-main-content` asserts it does not carry a solid ≥1px top+left border (the legacy `.chatbot-page` boxed look).

## A11y behavior (AC5)

- Exactly one `main` landmark (`#fc-main-content[role="main"]`) per route; at least one `<h1>` inside main; route heading text survives across every culture/mode.
- Under forced-colors: first Tab stop is the `fc-skip-link` (visible focus not lost, focused skip link laid out on-screen), `#fc-main-content` is a programmatic focus target (`tabindex=-1`) that receives focus, and content retains non-color text cues.
- No new accessibility package introduced — Playwright + DOM/ARIA assertions only.

## Build / test results (AC6)

| Gate | Command | Result |
| --- | --- | --- |
| Release build | `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` | **0 warnings / 0 errors** |
| UI Governance + UI.Tests | `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/...` | **227 passed, 0 failed, 0 skipped** |
| Real-render E2E lane | `dotnet test ... --filter RealRenderCrossSurfaceE2ETests` | **3 passed, 0 failed, Skipped: 0** |
| Full UI E2E project | `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/...` | **139 passed, 0 failed, 0 skipped** |
| Whitespace / conflict markers | `git diff --check` | **clean** |

> Real-browser confirmation: the `RealRenderFixture` no-browser fallback did **not** run — Chromium resolved at `/usr/bin/google-chrome` and every real-render test executed the live browser path (`Skipped: 0`).

## QA augmentation — `bmad-qa-generate-e2e-tests` (2026-06-23)

A QA gap-analysis pass was run over the as-built real-render lane against AC1–AC6 and the Task 1–5 subtasks. The infrastructure (host, browser discipline, six routes, cultures/modes, screenshots, skip-link a11y) was already complete; the pass added the following **probe-verified** assertions to `RealRenderCrossSurfaceE2ETests.AllSixSurfaces_ComposeFrontComposerLayout_WithoutLegacyChrome_BelowShellBand` (the EN/light per-surface loop):

| # | Gap (AC / Task) | Assertion added |
| --- | --- | --- |
| 1 | AC3 / Task 3 — "real DOM contains **FcPageLayout** output" was implied but unasserted (only FcPageHeader was checked) | `#fc-main-content.fc-page-layout` (×1) and `#fc-main-content[data-fc-page-layout]` (×1) per route |
| 2 | AC3 / Task 3 — "**FluentAccordion** on 13.7 grouped surfaces" was covered only by a generic any-Fluent OR | `#fc-main-content fluent-accordion` count > 0 for `project-workspace` and `governed-operations` |
| 3 | AC4 / Task 3 — heading "must not intersect the **header action area**" was covered only by the abstract 48px band line | heading top `>=` bottom of the live `[data-testid="fc-account-menu"]` action control |

**Ground-truth method:** a throwaway discovery probe booted the live host and dumped per-surface live-DOM tag counts + `#fc-main-content` attributes before any assertion was written, then was deleted. This confirmed each added assertion holds against the real fake-data render, avoiding false negatives.

**Gaps deliberately NOT added (documented to show the analysis was exhaustive, not skipped):**

- **Strict per-surface `FluentDataGrid`/`FluentGrid` assertions.** The probe showed `fluent-data-grid` = 0 and `fluent-grid` = 0 in the live DOM on the operational-dashboards and compliance-audit surfaces: those grids only materialise once seeded with query results, and the metadata-only `IChatBotClient` seam intentionally holds an Unknown/empty posture (operational dashboards never even call the client). A strict data-grid assertion would be a false negative of the **test seam**, not of the live composition — which is why the dev used a generic Fluent-composition OR plus the no-`<dl>` assertion to prove those surfaces are Fluent-composed rather than dumped. Task 3's wording is "FluentDataGrid … **where present**"; under this seam they are correctly not present.
- **"`main` landmark **named by** the route heading" via `aria-labelledby`.** The ChatBot `MainLayout.razor` instantiates `<FrontComposerShell>` without wiring `ContentLabelledBy`, and no page declares `<FcContentLabel>`, so `#fc-main-content` carries neither `aria-labelledby` nor `aria-label` (probe-confirmed `<null>`/`<null>`) and uses the implicit "main" accessible name — the shell's documented zero-config default. Asserting an aria-name tie to the heading id would false-fail the as-built app. AC5's intent is met by the existing single-`main`-landmark + present-route-heading assertions.

**QA re-run (this pass, real Chromium path):**

| Gate | Command | Result |
| --- | --- | --- |
| Release build of the E2E project | `dotnet build tests/Hexalith.ChatBot.UI.E2E.Tests/...csproj -c Release` | **0 warnings / 0 errors** |
| Real-render lane (with added gaps) | `dotnet test ... --filter RealRenderCrossSurfaceE2ETests` | **3 passed, 0 failed, Skipped: 0** |
| Full UI E2E project (regression) | `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.E2E.Tests/...` | **139 passed, 0 failed, 0 skipped** |
| Screenshots | — | **42** present, unchanged |
| Whitespace / conflict markers | `git diff --check` | **clean** |

**Post-QA gate refresh (2026-06-22T22:16:12Z):** after the QA assertion additions, the orchestrator re-ran `dotnet build Hexalith.ChatBot.slnx -c Release -m:1 -nodeReuse:false` (**0 warnings / 0 errors**) and `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj -c Release --no-build` (**227 passed, 0 failed, 0 skipped**).

Scope of the QA pass remained test-only: the sole touched files are within `tests/Hexalith.ChatBot.UI.E2E.Tests/` (`RealRenderCrossSurfaceE2ETests.cs`). No production, backend, localization-resource, or sibling-submodule change.
