---
baseline_commit: bfed90a
---

# Story 13.3: Replace .chatbot-page content boxes with Fluent composition

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a frontend engineer,
I want every routable `Hexalith.ChatBot.UI` page to compose its content through a Fluent layout container (`FluentStack` / `FluentCard`) inside `FcPageLayout` instead of the hand-rolled `<section class="chatbot-page …">` content-box wrapper,
so that the bordered/boxed page-content chrome that fights the Fluent → FrontComposer visual chain is gone, the page reads as a clean full-width Fluent business surface, and the Story 13.1 guard list this story owns (`PageContentBoxAllowlist`, the `chatbot-page` whole-token box) shrinks to **empty**.

## Context

Epic 10 adopted the `FrontComposerShell`, Epic 12 migrated **leaf controls** to Fluent v5, and Story 13.2 adopted `FcPageLayout` + `FcPageHeader` across all 6 routable pages (fixing the shell-overlap bug). But the page content still sits inside a **hand-rolled `<section class="chatbot-page …">` content box** — the per-page wrapper that the proposal screenshots flagged as the "hard bordered box" un-Fluent content chrome. The reference module `Hexalith.Tenants.UI` does not have this defect: every page composes `<FcPageLayout> → <FluentStack Orientation="Orientation.Vertical"> → <FcPageHeader> + Fluent content`, with no bespoke content-box wrapper. Story 13.1 landed the build-blocking layout-composition guard (`ChatBotLayoutCompositionConformanceTests`) with a shrink-only `PageContentBoxAllowlist` seeded to today's 6 offenders. **This story (13.3) burns that list down to empty:** it replaces each `chatbot-page` content-box wrapper with the Tenants-pattern Fluent stack, establishing Fluent whitespace/hierarchy and removing the boxed content chrome. [Source: `_bmad-output/planning-artifacts/epics.md#Story 13.3`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md#Section 4.A`; `_bmad-output/implementation-artifacts/13-1-frontcomposer-layout-composition-guard.md`; `_bmad-output/implementation-artifacts/13-2-adopt-fcpagelayout-fcpageheader-across-pages.md`]

**13.3 is content-wrapper re-composition only.** It does **not** regroup the inner `.chatbot-section` titled sections into `FluentAccordion` (Story 13.7 — already in progress), does **not** build `/operational-dashboards` data-viz / KPI tiles (Story 13.5), does **not** migrate `<dl class="chatbot-definition-list">` data dumps (Story 13.4, done), does **not** delete any `.chatbot-*` CSS (Story 13.8), does **not** rewrite the static E2E fixtures (Story 13.9 real-render), and makes **no** backend / CommandGateway / CLI / MCP / SignalR / Dapr / EventStore / sibling-submodule change. Governed semantics, accessibility labels/landmarks (NFR6), non-color status cues (UX-DR4), EN+FR localization, focus management (UX-DR34), and the "no fake/freeform textbox" safety model are preserved **exactly**.

> **Ahead-of-story repo (read this first).** The working tree is several Epic-13 stories ahead of this story's baseline: Stories 13.2 (FcPageHeader/FcPageLayout), 13.4 (definition-lists), 13.6 (audit form) are committed and 13.7 (accordion) is partially applied — so each of the 6 files **already** carries `<FcPageLayout>`, `<FcPageHeader>`, and (in some files, e.g. `GovernedOperations.razor`) `<FluentAccordion>`. **Story 13.3 is still genuinely un-done:** the outer `<section class="chatbot-page …">` wrapper survives in all 6 files (the guard's `PageContentBoxAllowlist` still has 6 live entries; the stale-entry ratchet proves the token is still present). Your job is **only** to unwrap that outer `chatbot-page` box — leave the already-migrated FcPageHeader/accordion/data work alone. Verify against source, not against the planning prose.

## Acceptance Criteria

1. **All 6 `chatbot-page` content-box wrappers are replaced by a Fluent layout container; `PageContentBoxAllowlist` is empty.** Given the 6 files that carry the `chatbot-page` whole-class token — `Components/Pages/AssociationReview.razor`, `Components/Pages/ComplianceAuditInvestigation.razor`, `Components/Pages/GovernedOperations.razor`, `Components/Pages/OperationalDashboards.razor`, `Components/Pages/ProjectWorkspace.razor`, and `Components/Governed/ChatBotProjectConversationWorkspace.razor` — when re-composed, then each outer `<section class="chatbot-page …" aria-labelledby="…-title" data-chatbot-responsive-fixture="…">` that currently wraps `<FcPageHeader>` + the page content **inside** `<FcPageLayout>` is replaced with a Fluent layout container `<FluentStack Orientation="Orientation.Vertical" …>` (mirroring `Hexalith.Tenants.UI` `TenantAuditPage`/`MyTenantsPage`), and its closing `</section>` becomes `</FluentStack>`. Every `chatbot-page` whole-class token is removed from `src/Hexalith.ChatBot.UI/**/*.razor`. After this story `PageContentBoxAllowlist` is `[]` and `Pages_do_not_hand_roll_content_box_wrapper_except_shrinking_allowlist` passes. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (lines 40-42, 75-83, 115-122, 207-208); `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (lines 12-17); `MyTenantsPage.razor` (lines 6-7)]

2. **Heading association, responsive-fixture hooks, and secondary layout classes are preserved; no new localization keys.** Given the wrapper swap, then on each new `<FluentStack>`: the `aria-labelledby="…-title"` is preserved so it still references the `FcPageHeader` route `h1` (the `HeadingId` values are unchanged — `governed-operations-title`, `operational-dashboards-title`, `association-review-title`, `compliance-audit-title`, `project-workspace-title`, and `@HeadingId` for the shared workspace); the `data-chatbot-responsive-fixture="…"` attribute is preserved verbatim (it is the responsive E2E hook); and any **non-`chatbot-page`** secondary class on the original wrapper moves to the FluentStack `Class=` unchanged — `chatbot-association-review` (AssociationReview), `compliance-audit-investigation` (ComplianceAuditInvestigation), `chatbot-project-workspace` (ProjectWorkspace), `chatbot-project-conversation` (ChatBotProjectConversationWorkspace); the two files with no secondary class (`GovernedOperations`, `OperationalDashboards`) get a bare `<FluentStack Orientation="Orientation.Vertical" aria-labelledby="…" data-chatbot-responsive-fixture="…">`. No localization key is added, removed, or renamed (this is a pure structural wrapper swap). [Source: source scan of the 6 `chatbot-page` wrappers on 2026-06-22 at HEAD `bfed90a`; `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (FluentStack carries `Class`, `data-*`, and `aria-labelledby`)]

3. **The boxed content chrome is gone; the page composes as a clean full-width Fluent surface.** Given the running app (`aspire run`, ChatBot UI on `http://localhost:5000`), when each of the 6 routes loads, then page content is no longer wrapped in the hand-rolled `chatbot-page` box (which provided `display:grid; gap:0.75rem; max-width:min(100%,70rem)` via `chatbot.tokens.css` lines 83-132); content now flows through `FcPageLayout` (`FullWidth`) + the Fluent vertical stack, following the Fluent → FrontComposer whitespace/hierarchy chain (no bespoke content box). Verify visually against the real render; the **binding automated gate for this story is the guard + Release build** — the full real-render screenshot gate is Story 13.9. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md#Section 1` (item 2: "Hard 1px black bordered box wrapping the main content"); `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` (lines 83-132); memory `chatbot-ui-fluent-component-divergence` — RUN the app, do not trust greps/fixtures]

4. **Scope boundary preserved — sibling sections, dashboards, `<dl>` dumps, CSS, and fixtures untouched.** Given Stories 13.4/13.5/13.7/13.8/13.9 own the adjacent work, when 13.3 is implemented, then: the inner `.chatbot-section` titled sections are **not** regrouped into `FluentAccordion` (Story 13.7, in progress — where its accordion already exists, e.g. `GovernedOperations.razor`, it is left exactly as-is; where it does not yet exist, the `.chatbot-section` sections remain for 13.7); `/operational-dashboards` data is **not** converted to `FluentDataGrid`/KPI tiles (Story 13.5) and `DefinitionListAllowlist` stays exactly `["Components/Pages/OperationalDashboards.razor"]`; `<dl class="chatbot-definition-list">` is **not** migrated (Story 13.4, done); **no `.chatbot-*` CSS is deleted** — including `.chatbot-page` and `.chatbot-section` in `chatbot.tokens.css` (Story 13.8); `PageHeaderChromeAllowlist`, `CommandBarAllowlist`, and `NotYetComposedPageBacklog` stay `[]`; the hand-authored static E2E fixtures in `tests/Hexalith.ChatBot.UI.E2E.Tests` are **not** rewritten (Story 13.9); and there is **no** backend / CommandGateway / CLI / MCP / SignalR / Dapr / EventStore change and **no edit inside any sibling submodule** (`Hexalith.FrontComposer`, `Hexalith.Tenants`, `Hexalith.EventStore`, …). [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13` (binding sequencing); `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (lines 73-103); `Hexalith.AI.Tools/hexalith-llm-instructions.md`]

5. **The guard is green and the build is clean.** Given the migration, when the Governance lane runs, then `ChatBotLayoutCompositionConformanceTests` passes with `PageContentBoxAllowlist = []` (and `PageHeaderChromeAllowlist`/`CommandBarAllowlist`/`NotYetComposedPageBacklog` `[]` and `DefinitionListAllowlist` unchanged at its 1 entry); the existing Story 12.1 `ChatBotFluentConformanceTests` still passes (no raw `<button>/<input>/<select>/<textarea>` reintroduced, no legacy v4/FAST tokens); `dotnet build Hexalith.ChatBot.slnx` is 0 warnings / 0 errors (`TreatWarningsAsErrors`); `git diff --check` is clean; and accessibility (landmarks, focusable heading targets, non-color status cues) and EN+FR localization are preserved. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `Directory.Packages.props`]

## Tasks / Subtasks

- [x] Re-scan and confirm the exact 6-file `chatbot-page` offender set before editing (AC: 1)
  - [x] Run the whole-token scan over `src/Hexalith.ChatBot.UI/**/*.razor` (e.g. `grep -rlP "(?<=[\"'\s])chatbot-page(?=[\"'\s])" --include=*.razor`) and confirm it returns exactly the 6 files in `PageContentBoxAllowlist` (no more, no fewer). The boundary regex must NOT match `chatbot-page-header`/`chatbot-page-title`. — confirmed exactly 6 files at baseline; no prefixed tokens matched.

- [x] Unwrap the `chatbot-page` content box in the 2 standalone-class pages (AC: 1, 2, 3)
  - [x] `Components/Pages/GovernedOperations.razor` (line ~22): replace `<section class="chatbot-page" aria-labelledby="governed-operations-title" data-chatbot-responsive-fixture="governed-operations">` with `<FluentStack Orientation="Orientation.Vertical" aria-labelledby="governed-operations-title" data-chatbot-responsive-fixture="governed-operations">`; change its closing `</section>` to `</FluentStack>`. Keep the `<FcPageLayout>` wrapper, the `<FcPageHeader>`, the status banners, and the already-present `<FluentAccordion>` (13.7) all unchanged inside it.
  - [x] `Components/Pages/OperationalDashboards.razor` (line ~25): same swap with `aria-labelledby="operational-dashboards-title" data-chatbot-responsive-fixture="operational-dashboards"`. Do **not** touch the dashboard data content (13.5) or its `<dl class="chatbot-definition-list">` (its `DefinitionListAllowlist` entry stays).

- [x] Unwrap the `chatbot-page` content box in the 4 multi-class pages, carrying the secondary class to `Class=` (AC: 1, 2, 3)
  - [x] `Components/Pages/AssociationReview.razor` (line ~23): `<section class="chatbot-page chatbot-association-review" aria-labelledby="association-review-title" data-chatbot-responsive-fixture="association-review">` → `<FluentStack Orientation="Orientation.Vertical" Class="chatbot-association-review" aria-labelledby="association-review-title" data-chatbot-responsive-fixture="association-review">`; close `</FluentStack>`.
  - [x] `Components/Pages/ComplianceAuditInvestigation.razor` (line ~27): `class="chatbot-page compliance-audit-investigation"` → `Class="compliance-audit-investigation"` on `<FluentStack Orientation="Orientation.Vertical">`, preserving `aria-labelledby="compliance-audit-title" data-chatbot-responsive-fixture="audit-investigation-s9"`. Do **not** touch the Story 13.6 `FluentGrid` filter layout inside.
  - [x] `Components/Pages/ProjectWorkspace.razor` (line ~31, the no-project/`else` branch wrapper): `class="chatbot-page chatbot-project-workspace"` → `Class="chatbot-project-workspace"`, preserving `aria-labelledby="project-workspace-title" data-chatbot-responsive-fixture="project-workspace"`.
  - [x] `Components/Governed/ChatBotProjectConversationWorkspace.razor` (line ~24, the shared workspace used by `/` selected branch + `/projects/{id}/conversation`): `class="chatbot-page chatbot-project-conversation"` → `Class="chatbot-project-conversation"`, preserving `aria-labelledby="@HeadingId" data-chatbot-responsive-fixture="@ResponsiveFixture"`.
  - [x] In every file: confirm the `<FluentStack>` open/close balances correctly (the original `<section>` may close near the end of the `<MainContent>` block, after inner sections) and the `<FcPageLayout>` still encloses the whole thing. — verified: each file has `<FcPageLayout>` 1 open / 1 close and the build (Razor compiler) confirms balanced tags.

- [x] Empty the Story 13.1 guard list this story owns (AC: 1, 4, 5)
  - [x] In `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` set `PageContentBoxAllowlist = []` (lines 75-83).
  - [x] **Leave the other lists unchanged:** `PageHeaderChromeAllowlist`/`CommandBarAllowlist`/`NotYetComposedPageBacklog` already `[]`; `DefinitionListAllowlist` stays `["Components/Pages/OperationalDashboards.razor"]` (13.5 owns the last entry). Do not weaken the regexes, ratchets, or detector fixtures. — confirmed unchanged.

- [x] Verify and document (AC: all)
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` (restore first if needed) → 0 warnings, 0 errors. (Also ran `-c Release`: 0/0.)
  - [x] Run the Governance lane: `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false` → confirm `ChatBotLayoutCompositionConformanceTests` (incl. `Pages_do_not_hand_roll_content_box_wrapper_except_shrinking_allowlist`) + `ChatBotFluentConformanceTests` all green. — **48 total, 0 failed, 0 skipped**.
  - [x] Token sweep: `chatbot-page` whole-token count across `src/Hexalith.ChatBot.UI/**/*.razor` is **0**.
  - [x] (Best-effort) Run the app (`aspire run`) and visually confirm the boxed content chrome is gone on the 6 routes (AC3). If the live Aspire/DAPR sandbox is flaky, record the structural verification and defer the screenshot pass to Story 13.9 (per AC3), as Story 13.2 did. — live Aspire/DAPR run deferred to Story 13.9 (documented sandbox flakiness); verified structurally (token sweep 0, FluentStack inside FcPageLayout FullWidth, Tenants parity).
  - [x] `git diff --check`. — clean.
  - [x] Add `_bmad-output/implementation-artifacts/tests/test-summary-story-13.3.md` (exact commands + pass/fail counts + the token-sweep + the content-box verification note), mirroring the Story 13.1/13.2 evidence convention.
  - [x] Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: `13-3-replace-content-boxes-with-fluent-composition` `→ review`, update `last_updated`.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files (`SKILL.md`, `discover-inputs.md`, `template.md`, `checklist.md`), config (`_bmad/bmm/config.yaml`: user `Jerome`, English), and the sibling `**/project-context.md` persistent facts (.NET 10, C# 14, warnings-as-errors, central package versions, `.slnx`, xUnit v3 + Shouldly, root-level submodules only, never edit generated output).
- Loaded `sprint-status.yaml`: `13-3-…` is `backlog`; `epic-13` is `in-progress`; 13.1/13.2 `done`, 13.4/13.6 `done`, 13.7 `in-progress`, 13.5/13.8/13.9 `backlog`.
- Loaded `epics.md` Epic 13 + Story 13.3 + amended UX-DR1/DR2; `architecture.md` "ChatBot UI FrontComposer layout composition" subsection; `sprint-change-proposal-2026-06-22.md` (root-cause + reference pattern + the 13.3 row).
- Loaded the predecessor stories 13.1 (the guard) and 13.2 (FcPageHeader/FcPageLayout adoption + the delegation knot), plus the guard `ChatBotLayoutCompositionConformanceTests.cs` (the list this story burns down) and the 13.1/13.2 test summaries.
- Loaded the Tenants reference `TenantAuditPage.razor` + `MyTenantsPage.razor` (the canonical `FcPageLayout → FluentStack(Vertical) → FcPageHeader + content` composition), the FrontComposer `FcPageLayout.razor`/`FcPageHeader.razor`, the 6 ChatBot offender files, and `chatbot.tokens.css`.

### Authoritative offender inventory (source scan, 2026-06-22, HEAD `bfed90a`)

| Guard list (in `ChatBotLayoutCompositionConformanceTests`) | Seed | After 13.3 | Files 13.3 touches |
|---|---|---|---|
| `PageContentBoxAllowlist` (`chatbot-page` whole token) | 6 | **0** | the 6 files below |
| `PageHeaderChromeAllowlist` (`chatbot-page-header`) | 0 | 0 (unchanged) | — (13.2) |
| `CommandBarAllowlist` (`chatbot-command-bar`) | 0 | 0 (unchanged) | — (13.2) |
| `DefinitionListAllowlist` (`chatbot-definition-list`) | 1 | **1 (unchanged)** | — owned by Story 13.5 |
| `NotYetComposedPageBacklog` (`@page` require-compose) | 0 | 0 (unchanged) | — (13.2) |

The 6 `chatbot-page` offenders and their current wrapper shape (all sit at `<FcPageLayout> → <section class="chatbot-page …"> → <FcPageHeader> + content`):

| File | route | wrapper attributes (verbatim) | secondary class → `Class=` |
|---|---|---|---|
| `Components/Pages/GovernedOperations.razor` (~L22) | `/governed-operations` | `aria-labelledby="governed-operations-title" data-chatbot-responsive-fixture="governed-operations"` | (none) |
| `Components/Pages/OperationalDashboards.razor` (~L25) | `/operational-dashboards` | `aria-labelledby="operational-dashboards-title" data-chatbot-responsive-fixture="operational-dashboards"` | (none) |
| `Components/Pages/AssociationReview.razor` (~L23) | `/association-review/{AssociationId}` | `aria-labelledby="association-review-title" data-chatbot-responsive-fixture="association-review"` | `chatbot-association-review` |
| `Components/Pages/ComplianceAuditInvestigation.razor` (~L27) | `/compliance-audit-investigation` | `aria-labelledby="compliance-audit-title" data-chatbot-responsive-fixture="audit-investigation-s9"` | `compliance-audit-investigation` |
| `Components/Pages/ProjectWorkspace.razor` (~L31, no-project branch) | `/` | `aria-labelledby="project-workspace-title" data-chatbot-responsive-fixture="project-workspace"` | `chatbot-project-workspace` |
| `Components/Governed/ChatBotProjectConversationWorkspace.razor` (~L24, shared) | `/` (selected) + `/projects/{id}/conversation` | `aria-labelledby="@HeadingId" data-chatbot-responsive-fixture="@ResponsiveFixture"` | `chatbot-project-conversation` |

### The canonical transform (mechanical, per-file)

**Before** (every file):
```razor
<FcPageLayout Mode="FcPageLayoutMode.FullWidth">
<section class="chatbot-page [SECONDARY]" aria-labelledby="[ID]" data-chatbot-responsive-fixture="[FIX]">
    <FcPageHeader … />
    … banners / accordion / sections …
</section>
</FcPageLayout>
```
**After** (Tenants `TenantAuditPage` shape):
```razor
<FcPageLayout Mode="FcPageLayoutMode.FullWidth">
<FluentStack Orientation="Orientation.Vertical" Class="[SECONDARY]" aria-labelledby="[ID]" data-chatbot-responsive-fixture="[FIX]">
    <FcPageHeader … />
    … banners / accordion / sections … (unchanged)
</FluentStack>
</FcPageLayout>
```
- Omit `Class=` entirely for the 2 files with no `[SECONDARY]` class.
- `FluentStack` captures unmatched attributes, so `aria-labelledby` and `data-chatbot-responsive-fixture` splat through (the Tenants `TenantAuditPage` outer FluentStack does exactly this — `Class` + `data-testid` + `aria-labelledby`).
- Keep `<FcPageLayout>`/`</FcPageLayout>` exactly as 13.2 left them. The only edit is the opening `<section …>` → `<FluentStack …>` and its matching `</section>` → `</FluentStack>`.

### Why `FluentStack` (not `FluentCard`) for the outer container

The AC text reads "FluentStack/FluentCard spacing and grouping," but the **outer page-content container** is a vertical **`FluentStack`** in the Tenants reference (`MyTenantsPage`/`TenantAuditPage` both use `<FluentStack Orientation="Orientation.Vertical">` directly inside `<FcPageLayout>`), not a `FluentCard`. A `FluentCard` adds elevation/surface chrome — exactly the boxed look we are removing; reserve `FluentCard` for **grouped sub-surfaces** (e.g. the KPI/data tiles that **Story 13.5** introduces on the dashboard), not the page shell. Using `FluentStack Vertical` gives clean Fluent whitespace with no border and matches the reference one-to-one. Do not add an explicit `VerticalGap`/`Class` beyond the carried secondary class unless a file visibly needs it — the FluentStack default gap is fine; keep the diff minimal.

### Accessibility / landmark note (so review does not churn)

- The original `<section class="chatbot-page …" aria-labelledby="…-title">` is a named **`region`** landmark (a `<section>` gains the `region` role once it has an accessible name). Replacing it with `FluentStack` (which renders a `<div>`) drops that implicit `region` role. **This is intentional and matches Tenants** — the single content landmark is the FrontComposer shell's `<main>` (`#fc-main-content`), inside which `FcPageHeader` renders the route `h1`; the page does not need a second nested `region` landmark. Keeping `aria-labelledby` on the FluentStack preserves the programmatic name association and is exactly what `TenantAuditPage` does. **Do not** flag the lost `region` as an a11y regression. (If a reviewer insists on preserving an explicit `region`, an acceptable alternative is to keep a bare `<section aria-labelledby="…-title">` wrapper with the `chatbot-page` **class token dropped** — that also passes the guard, since only the class token is banned — but the `FluentStack` form is preferred for Tenants parity and is the AC's intent.)
- `FcPageHeader` continues to own the focusable route `h1` via `HeadingId`; no heading `id` changes, so any kept `aria-labelledby` still resolves. Non-color status cues, EN+FR localization, and the "no fake/freeform textbox" model are untouched (no text or control changes).

### Scope boundary — what 13.3 must NOT do (the review will check these)

- **Do NOT touch `.chatbot-section` titled sections / `FluentAccordion`.** The inner `<section class="chatbot-section">` blocks are **Story 13.7's** concern (group sibling titled sections in one `FluentAccordion`). Several files already have 13.7's accordion applied (e.g. `GovernedOperations.razor` lines ~66-71); leave it exactly as-is. Where 13.7 has not yet run, leave the `.chatbot-section` sections in place — 13.3 only unwraps the **outer `chatbot-page` box**. `.chatbot-section` is **not guarded** and **not** in 13.8's CSS-deletion list (it is pure `display:grid; gap` layout); do not migrate or delete it here.
- **Do NOT migrate dashboard data or `<dl>` dumps.** `/operational-dashboards` data-viz (`FluentDataGrid` + KPI `FluentCard` tiles) is **Story 13.5**; its `<dl class="chatbot-definition-list">` keeps the single live `DefinitionListAllowlist` entry. `<dl>`→Fluent migration elsewhere was **Story 13.4** (done).
- **Do NOT delete any CSS.** `.chatbot-page` (and `.chatbot-section`) CSS stays in `chatbot.tokens.css` until **Story 13.8**. Removing the *class usage* from the `.razor` is this story; removing the *CSS rule* is 13.8.
- **Do NOT rewrite the static E2E fixtures.** `tests/Hexalith.ChatBot.UI.E2E.Tests/*` are hand-authored HTML string fixtures (ChatBot UI has zero `RenderComponent<`); editing `.razor` does not break them, and rewriting them to the new markup is **Story 13.9's** real-render reverification (the deliberate Story 12.9 gap). [memory `chatbot-ui-no-bunit-test-strategy`]
- **Do NOT edit sibling submodules.** `FcPageLayout`/`FcPageHeader`/`FluentStack` are consumed read-only from `Hexalith.FrontComposer` / Fluent UI. No `MainLayout.razor` or CSS-in-submodule change.
- **Do NOT let any allowlist grow or weaken the guard.** Only `PageContentBoxAllowlist` shrinks (to `[]`); every other list and every ratchet/detector-fixture stays intact.

### Architecture & boundary guardrails

- UI may reference ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts only — never Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, or projection internals. This story touches only `Hexalith.ChatBot.UI` `.razor` + the one UI test guard. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13`; `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs`]
- Rendering-layer correction only: preserve governed semantics, NFR6 a11y labels/landmarks, UX-DR4 non-color status cues, EN+FR localization, UX-DR34 focus management, and the "no fake/freeform textbox" safety model. UX-DR1/DR2 require page-composition-level conformance (compose via `FcPageLayout`/`FcPageHeader` + Fluent layout primitives, no hand-rolled content boxes). [Source: `epics.md#UX-DR1`/`#UX-DR2`]

### File structure

- Edit (UI source, 6 files): `Components/Pages/{GovernedOperations,OperationalDashboards,AssociationReview,ComplianceAuditInvestigation,ProjectWorkspace}.razor`; `Components/Governed/ChatBotProjectConversationWorkspace.razor`.
- Edit (guard, 1 file): `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (set `PageContentBoxAllowlist = []`).
- New (evidence): `_bmad-output/implementation-artifacts/tests/test-summary-story-13.3.md`.
- Do **not** edit generated files under `obj/**/generated/`, the static E2E fixtures, `chatbot.tokens.css`, or any sibling submodule.

### Testing standards

- xUnit v3 + Shouldly; `[Trait("Category", "Governance")]` already carried by the guard — no new test class is required (this story only empties an allowlist; the existing detector/ratchet asserts the migration). No package-version edits (central versions already provide xUnit v3 / Shouldly / Fluent UI `5.0.0-rc.3-26138.1` — all pinned; Epic 13 states "Fluent UI v5 and FrontComposer stay pinned"). Run the UI Governance filter, then the full slnx build, then the token sweep + `git diff --check`. Failure messages stay metadata-only (relative paths + class names). [Source: `Directory.Packages.props`; `tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj`]

### Latest technical information

- `Components/_Imports.razor` already imports `@using Hexalith.FrontComposer.Shell.Components.Layout` and `@using Hexalith.FrontComposer.Contracts.Rendering` (added by 13.2), and Fluent UI's `_Imports` provides `<FluentStack>`/`Orientation` — so no new `@using` is needed. Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3, Shouldly, FrontComposer — all pinned; no upgrades. `FluentStack` captures unmatched attributes (so `aria-labelledby`/`data-*` splat), confirmed by the Tenants `TenantAuditPage` outer FluentStack. [Source: `src/Hexalith.ChatBot.UI/Components/_Imports.razor`; `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]

### Git intelligence

Recent commits (`bfed90a` feat(story-13.2), `d344a98` feat(story-13.1), `61c74e9` 13.1 test-summary + 13.7 accordion tests, `8a7964d` feat(story-13.6), `b310462` feat(story-13.4)) show Epic 13 is being built ahead of story order — 13.2/13.4/13.6 are committed and 13.7 is in flight, but **13.3's `chatbot-page` unwrap is not among them** (the guard's `PageContentBoxAllowlist` still has 6 live entries, proven by the green stale-entry ratchet). The carried lesson (memory `chatbot-ui-fluent-component-divergence`): leaf-control conformance and static-fixture verification both passed while live page composition stayed boxed — so 13.3's truth signal is the build-enforced guard (`PageContentBoxAllowlist` → empty) **plus** a real-app check that the content box is gone, not fixtures. Because the repo is ahead, the dev-story session may find the work already partly staged in the tree; verify each of the 6 files against source and prove the guard green rather than trusting the planning prose. [Source: `git log`; `_bmad-output/implementation-artifacts/13-1-…md`, `13-2-…md`; memory `chatbot-epic13-guard-seed-count-variance`]

### Project structure notes

- Aligns with the established ChatBot UI test layout: the layout-composition guard lives in `tests/Hexalith.ChatBot.UI.Tests` with `[Trait("Category", "Governance")]`, source-scan based (no bUnit). This story makes the guard's `PageContentBoxAllowlist` shrink to empty. The real-render screenshot gate remains Story 13.9. No conflicts with the unified project structure; no new projects or packages.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, `checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`; `Hexalith.AI.Tools/CLAUDE.md` (UI/UX rules, submodule policy)]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 13: ChatBot UI FrontComposer Layout Composition Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 13.3: Replace .chatbot-page/.chatbot-section content boxes with Fluent composition`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`; `#UX-DR2`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture` (ChatBot UI FrontComposer layout composition)]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-22.md#Section 4.A` (13.3 row); `#Section 1` (item 2, bordered content box)]
- [Source: `_bmad-output/implementation-artifacts/13-1-frontcomposer-layout-composition-guard.md`; `13-2-adopt-fcpagelayout-fcpageheader-across-pages.md`; `tests/test-summary-story-13.1.md`, `test-summary-story-13.2.md`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (lines 40-42, 75-83, 115-122, 207-208)]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` (lines 12-17); `MyTenantsPage.razor` (lines 6-7)]
- [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor`; `FcPageHeader.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Pages/{GovernedOperations,OperationalDashboards,AssociationReview,ComplianceAuditInvestigation,ProjectWorkspace}.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor`]
- [Source: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` (lines 83-132); `src/Hexalith.ChatBot.UI/Components/_Imports.razor`]
- [Source: `Directory.Packages.props`]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8[1m])

### Debug Log References

- Whole-token scan (`grep -rlP "(?<=[\"'\s])chatbot-page(?=[\"'\s])" --include=*.razor src/Hexalith.ChatBot.UI/`): 6 files at baseline → 0 after migration.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`: Build succeeded, 0 warnings, 0 errors.
- `dotnet build Hexalith.ChatBot.slnx -c Release --no-restore -m:1 -nodeReuse:false`: Build succeeded, 0 warnings, 0 errors.
- `DiffEngine_Disabled=true dotnet test … --filter "Category=Governance" …`: Passed — 48 total, 0 failed, 0 skipped.
- `git diff --check`: clean.

### Completion Notes List

- Replaced the hand-rolled `<section class="chatbot-page …">` content-box wrapper in all 6 offender files with the `Hexalith.Tenants.UI` reference pattern: `<FluentStack Orientation="Orientation.Vertical">` directly inside `FcPageLayout`, closing `</FluentStack>`. The 2 standalone-class files (`GovernedOperations`, `OperationalDashboards`) use a bare `FluentStack`; the 4 multi-class files carry their secondary class to `Class=` (`chatbot-association-review`, `compliance-audit-investigation`, `chatbot-project-workspace`, `chatbot-project-conversation`).
- `aria-labelledby` and `data-chatbot-responsive-fixture` are preserved verbatim on every new `FluentStack` (splatted through `FluentStack`'s captured unmatched attributes); no heading `id`, localization key, or inner content (`FcPageHeader`, banners, 13.7 accordions, 13.6 `FluentGrid`, 13.4/13.5 data) was touched.
- Emptied the Story 13.1 guard list this story owns: `PageContentBoxAllowlist = []`. All other allowlists/ratchets/detector-fixtures left intact (`DefinitionListAllowlist` still has its 1 Story-13.5 entry; the other three lists already `[]`).
- Binding gate green: layout-composition guard (incl. `Pages_do_not_hand_roll_content_box_wrapper_except_shrinking_allowlist`) + `ChatBotFluentConformanceTests` pass; Debug + Release builds 0W/0E; `chatbot-page` whole-token count = 0.
- Live `aspire run` screenshot pass deferred to Story 13.9 per AC3 (documented sandbox flakiness), as Story 13.2 did; AC3 verified structurally. Accessibility: the implicit `region` landmark intentionally drops to match Tenants (shell `<main>` + `FcPageHeader` h1 remain; `aria-labelledby` preserved) — not a regression.

### File List

- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor` (modified)
- `src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor` (modified)
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor` (modified)
- `src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor` (modified)
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor` (modified)
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor` (modified)
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs` (modified — `PageContentBoxAllowlist` emptied)
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.3.md` (new)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — `13-3-…` → review; `last_updated`)
- `_bmad-output/implementation-artifacts/13-3-replace-content-boxes-with-fluent-composition.md` (modified — tasks, Dev Agent Record, Status)

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-22 | Senior Developer Review (AI) — adversarial review passed with 0 CRITICAL / 0 HIGH / 0 MEDIUM verified findings. Independently reproduced the binding gate (Governance lane 48/0/0; full slnx build 0W/0E; `chatbot-page` whole-token sweep = 0; `git diff --check` clean) and verified all 5 ACs against source + the `Hexalith.Tenants.UI` reference. Status → done; sprint-status synced. |
| 2026-06-22 | Implemented Story 13.3 (dev-story): swapped the 6 hand-rolled `<section class="chatbot-page …">` content-box wrappers for `<FluentStack Orientation="Orientation.Vertical">` inside `FcPageLayout` (preserving `aria-labelledby`, `data-chatbot-responsive-fixture`, and the secondary layout classes), and emptied the Story 13.1 guard's `PageContentBoxAllowlist`. Governance lane (48 tests) green; Debug + Release builds 0W/0E; `chatbot-page` whole-token count = 0; `git diff --check` clean. Added test-summary-story-13.3.md. Status → review. |
| 2026-06-22 | Created Story 13.3 (ready-for-dev): replace the 6 hand-rolled `<section class="chatbot-page …">` content-box wrappers with the Tenants-pattern `<FluentStack Orientation="Orientation.Vertical">` inside `FcPageLayout` (preserving `aria-labelledby`, `data-chatbot-responsive-fixture`, and any secondary layout class), and empty the Story 13.1 guard's `PageContentBoxAllowlist`. Sibling-section accordion grouping (13.7), dashboard data-viz (13.5), `<dl>` migration (13.4), and CSS deletion (13.8) are explicitly out of scope. |

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-06-22 · **Outcome:** ✅ Approve · **Status:** review → done

### Scope reviewed

The 6 `.razor` wrapper swaps + the owned guard-list edit, cross-referenced against git reality, the
5 ACs, and the `Hexalith.Tenants.UI` reference pattern. `_bmad-output/` artifacts and sibling submodules
excluded from code review per workflow.

### Verification (all gates reproduced independently)

| Check | Result |
| --- | --- |
| `git diff` of all 6 files | Each outer `<section class="chatbot-page …">` → `<FluentStack Orientation="Orientation.Vertical">`; matching `</FluentStack>`; `FcPageLayout`/`FcPageHeader`/inner content untouched. |
| `aria-labelledby` + `data-chatbot-responsive-fixture` | Preserved verbatim on every FluentStack (splat through `FluentComponentBase` `CaptureUnmatchedValues`; confirmed against Tenants `TenantAuditPage` which carries `Class`+`data-testid`+`aria-labelledby` identically). |
| Secondary classes → `Class=` | `chatbot-association-review`, `compliance-audit-investigation`, `chatbot-project-workspace`, `chatbot-project-conversation` carried; `GovernedOperations`/`OperationalDashboards` bare. |
| `chatbot-page` whole-token sweep (`src/**/*.razor`) | **0** (prefixed `-header`/`-title` correctly excluded). |
| `PageContentBoxAllowlist` | `[]`; other allowlists unchanged (`DefinitionListAllowlist` still 1 entry; others `[]`). |
| Governance lane (`Category=Governance`) | **48 passed, 0 failed, 0 skipped** — incl. `Pages_do_not_hand_roll_content_box_wrapper_except_shrinking_allowlist` + detector-fixture pins + `ChatBotFluentConformanceTests`. |
| `dotnet build Hexalith.ChatBot.slnx` | **0 Warnings, 0 Errors** (`TreatWarningsAsErrors`). |
| `git diff --check` | Clean. |
| File List vs git | Matches (6 `.razor` + 1 test modified; test-summary + story new; sprint-status modified). Only extra git-modified file is the story-automator orchestration log — out of review scope. |

### Findings

**0 CRITICAL / 0 HIGH / 0 MEDIUM verified.** The change is a mechanically exact, fully-scoped wrapper
swap; every `[x]` task reproduces against source.

**Observation (non-blocking, correctly out of scope):** the carried secondary classes still resolve to
live rules in `chatbot.tokens.css` (`.chatbot-association-review`/`.chatbot-project-conversation` →
`display:grid; gap:.75rem`; `.chatbot-project-workspace` → `overflow-wrap`), so they now apply to the
FluentStack-rendered `<div>` alongside its own flex layout until the rules are removed. This is the
intended transitional state: AC2 **mandates** carrying the class verbatim and AC4 **forbids** deleting
the CSS — removal is owned by **Story 13.8**, and the real-render confirmation is **Story 13.9**'s gate.
Not a 13.3 defect. (`compliance-audit-investigation` has no CSS rule — a no-op class carried verbatim
per AC2, which is correct.)

**Note:** the live `aspire run` visual pass was deferred to Story 13.9 (per AC3 the binding gate is the
guard + Release build, both green; documented sandbox flakiness; 13.2 precedent). Boxed-chrome removal
verified structurally.

### Decision

0 CRITICAL issues → **done**. No code fixes required (implementation was already correct). Sprint status
synced: `13-3-replace-content-boxes-with-fluent-composition` → `done`.
