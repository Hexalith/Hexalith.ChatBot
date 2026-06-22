# Sprint Change Proposal — ChatBot UI FrontComposer Layout Composition (Page-Level)

- **Date:** 2026-06-22
- **Author:** Jerome (via Correct Course workflow)
- **Trigger:** "The ChatBot UI has messy design, the UX is ugly. It should be a clean business interface (Blazor Fluent UI V5 look)" + "the chatbot should use FrontComposer" + "see how Tenant UI uses FrontComposer."
- **Mode:** Batch
- **Chosen approach:** Adopt FrontComposer **layout-composition** primitives (`FcPageLayout` / `FcPageHeader`) and Fluent **layout/data** components at the page level across all surfaces, mirroring the `Hexalith.Tenants.UI` reference pattern; extend the governance guard to enforce it; retire the remaining hand-rolled `.chatbot-*` page-chrome CSS.
- **Scope classification:** **Moderate→Major** (new remediation epic reworking the page-composition layer of two `done` epics + guard hardening + architecture/UX-DR amendments + a real-render re-verification gate). Product scope, governed semantics, command spine, and backend are **unchanged**.
- **Evidence basis:** **Live running app** — full `aspire run` topology (ChatBot AppHost + DAPR + EventStore + Tenants + ChatBot.Server) serving the real UI on `http://localhost:5000`. All 6 surfaces captured as **real rendered screenshots** (the missing evidence from the prior proposal) + source root-cause across `Hexalith.ChatBot.UI`, `Hexalith.FrontComposer`, and `Hexalith.Tenants.UI`.

---

## Section 1 — Issue Summary

The ChatBot UI still renders as a messy, non-enterprise surface — **even though the prior remediation (Epic 12, all 9 stories `done` 2026-06-21) genuinely succeeded at its stated scope.** This is not a regression and not a fake "done"; it is a **scope gap**: Epic 12 migrated *leaf controls* to Fluent but never touched *page-level composition*, and its "visual" verification never rendered the real app.

**What Epic 12 actually delivered (verified — real and complete for its scope):**

| Check | Before (2026-06-19) | Now |
|---|---|---|
| Raw `<button>/<input>/<select>/<textarea>` in `.razor` | 12 files | **0** |
| Fluent component usages | 9 total | **~160** (FluentButton/TextInput/Select/Card/Stack/Text…) |
| `chatbot.tokens.css` | 1,323 lines (custom token system) | **660 lines** (token system retired) |

**What is still wrong (the page-composition layer was never migrated):**

The live app shows the same defects on **every one of the 6 surfaces** (all shared-layout-level, not per-page):

1. **Header/command-bar overlap (a real layout bug, not just ugliness).** The page-title band ("Espace de projet", "Opérations gouvernées"…) renders **on top of** the FrontComposer shell's top bar — the page title overlaps the account icon, and the theme toggle/search/settings sit cramped above it. Reproduced on all 6 routes.
2. **Hard 1px black bordered box** wrapping the main content (un-Fluent; should be Fluent card elevation/no border).
3. **Raw monospace data dumps** — primary content is `<dl>` rows with values like `project-alpha`, `0.62`, `age>0 risk:any confidence:any…`, `m0-governed-command`, raw ISO timestamps. Reads like debug output, not a designed business surface.
4. **"Dashboard" with no data viz** — `/operational-dashboards` is a vertical stack of label rows with **no charts, no data grid, no KPI tiles**.
5. **Jumbled form** — `/compliance-audit-investigation` filter fields inline-wrap with no grid alignment (a label with no input beside it).
6. **Orphaned floating right column** and large dead space — no balanced page layout.

**Root cause (source-confirmed): the ChatBot hand-rolls its page chrome with custom `.chatbot-*` CSS that fights the FrontComposer shell, instead of composing pages from FrontComposer/Fluent layout primitives.**

In `Hexalith.ChatBot.UI` (42 `.razor` files):

| FrontComposer/Fluent layout primitive | ChatBot usages | Hand-rolled equivalent in ChatBot | Count |
|---|---|---|---|
| `FcPageLayout` | **0** | `<section class="chatbot-page">` (the black box) | 2 |
| `FcPageHeader` | **0** | `<header class="chatbot-page-header">` (the overlap) | 6 |
| Fluent toolbar (`FluentStack` actions) | — | `class="chatbot-command-bar"` | 3 |
| `FluentDataGrid` | **0** | `<dl class="chatbot-definition-list">` (monospace rows) | 25 |
| `FluentAccordion` (UX "Page sections" rule) | **0** | raw `<section>` siblings | 23 |

- Hand-rolled header markup: `<header class="chatbot-page-header"><h1 class="chatbot-page-title">…</h1></header>` rendered **inside** the shell `@Body`, so it collides with `FrontComposerShell`'s own header region → the overlap. (`Components/Pages/ProjectWorkspace.razor:34`, `Components/Governed/ChatBotProjectConversationWorkspace.razor:26`.)
- `MainLayout.razor:4` wraps `@Body` directly in `<FrontComposerShell>` with **no** `FcPageLayout`/`FcPageHeader` composition — every page reinvents the chrome.
- The black box comes from a uniform `border: 1px solid …` applied to `.chatbot-page`/content wrappers in `chatbot.tokens.css`.

**Why it shipped `done` undetected — a verification gap, again:**

- **Story 12.9 "cross-surface a11y/visual reverification" verified hand-authored static HTML fixtures, not the real rendered components** (no bUnit; fixtures are built in the E2E test C# and asserted with Playwright selectors — they never screenshot the live app). So the broken overlap/layout was **never visually observed** by the gate.
- **`ChatBotFluentConformanceTests` (the Epic 12.1 guard) only bans raw form controls + legacy v4 tokens.** It does **not** check that pages use `FcPageHeader`/`FcPageLayout`, does not ban hand-rolled `.chatbot-page-header`/`.chatbot-page`/`.chatbot-command-bar`, and does not forbid `<dl>` data dumps. The hand-rolled page chrome passed every gate.

**Governing requirements being violated:**

- **Hexalith UX — Component sources & reuse:** *"Module UI must always use the FrontComposer technical module and Blazor Fluent UI V5 components"* and *"avoid raw CSS/HTML when an equivalent component already exists"* (`hexalith-ux-instructions.md`). `FcPageHeader`/`FcPageLayout`/`FluentDataGrid` exist and are unused.
- **Hexalith UX — Page sections:** *"page-like surfaces with two or more sibling titled content sections must group those sections in a single `FluentAccordion`."* ChatBot uses **0** accordions across 23 raw `<section>`s.
- **UX-DR1 / UX-DR2** (`epics.md`): the Fluent → FrontComposer visual chain; "do not invent a custom chatbot design system." The custom system survives at the **layout** layer.

---

## Section 2 — Impact Analysis

### Epic Impact

- **Epic 12 (ChatBot UI Fluent v5 Component Conformance) — `done`, genuinely complete for *leaf-control* scope, but page-level composition was out of its scope and remains un-migrated.** No rollback: its component migration is correct and is the foundation this epic builds on.
- **Epic 10 (FrontComposer Shell Adoption) — `done`; the shell was adopted but pages never composed *through* it** (`FcPageLayout`/`FcPageHeader` never used) — the overlap is the direct symptom.
- **No other epic's behavior is affected.** Command spine, governance/approval semantics, backend, CLI, MCP untouched — pure UI rendering-layer correction.

### Story Impact

| Story | Status | Impact |
|---|---|---|
| 10.1 (Shell integration) | done | Shell wired, but pages render their own header inside `@Body` → overlap. |
| 12.1 (Fluent-only guard) | done | Guard is leaf-control-only; does not enforce FrontComposer layout composition. |
| 12.8 (retire tokens CSS) | done | Retired the *token* system; kept the *layout* system (`.chatbot-page-header`/`.chatbot-page`/`.chatbot-command-bar`) that causes the defects. |
| 12.9 (a11y/visual reverification) | done | Verified hand-authored fixtures, **not** the real render → never saw the broken layout. |

### Artifact Conflicts (documents needing updates)

- **`epics.md`** — UX-DR1/DR2 need a **page-composition** sub-criterion (compose via `FcPageLayout`/`FcPageHeader`, no hand-rolled page chrome); add the new remediation epic; retroactive notes on 10.1/12.1/12.8/12.9.
- **`architecture.md`** — extend the "ChatBot UI Fluent-only conformance" section to **"FrontComposer layout composition"** (require `FcPageLayout`/`FcPageHeader`; name the extended guard; carve-outs target none).
- **`sprint-status.yaml`** — add the new epic + stories.
- **Tests** — extend `ChatBotFluentConformanceTests` (or add a sibling guard) mirroring `Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests`; the re-verification story must screenshot the **real** app.

### Technical Impact (code)

- **6 pages** re-composed onto `FcPageLayout` + `FcPageHeader`; **6** hand-rolled headers, **2** page boxes, **3** command bars, **25** `<dl>` data dumps replaced with Fluent layout/data components.
- **`chatbot.tokens.css`** layout classes (`.chatbot-page*`, `.chatbot-command-bar`, `.chatbot-definition-list`, skip-link) deleted as `FcPageHeader`/shell provide them → file collapses toward genuine layout-only.
- **No dependency/deployment/infra changes.** Fluent UI v5 and FrontComposer are already referenced and pinned. Adapter boundary (UI → Client/ServiceDefaults/FrontComposer only) preserved.

---

## Section 3 — Recommended Approach

**Direct Adjustment** — add **Epic 13 — ChatBot UI FrontComposer Layout Composition Remediation** (rather than reopening `done` Epics 10/12, mirroring how Epics 10/11/12 were each added by prior correct-course proposals). Reference implementation: **`Hexalith.Tenants.UI`**, which composes every page as:

```razor
<FcPageLayout Mode="FcPageLayoutMode.FullWidth">
  <FluentStack Orientation="Orientation.Vertical">
    <FcPageHeader PageTitle="…" Heading="…" Eyebrow="…" Description="…" HeadingId="…">
      <Metadata>…context…</Metadata>
      <Actions>…toolbar buttons / back link…</Actions>
    </FcPageHeader>
    … Fluent content (FluentDataGrid / FluentAccordion / FluentCard / FluentStack) …
  </FluentStack>
</FcPageLayout>
```
(`Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`, `TenantAuditPage.razor`; guarded by `DomainUiFluentConformanceTests`.)

`FcPageHeader` renders inside the shell's single `#fc-main-content` landmark with the title typography (FluentText Size700/Semibold), an `Eyebrow`, `Description`, and `Metadata`/`Actions` slots — so adopting it **removes the overlap by construction** and replaces the custom command bar.

**Guard-first sequencing** (mirrors Epic 12): Story 13.1 lands the extended layout-composition guard with an allowlist seeded to today's offenders (allowlist may only shrink); each migration story removes its files, so progress is build-enforced, ending at an empty allowlist. The final story re-verifies against the **real rendered app**, closing the 12.9 fixture gap.

- **Effort:** ~9 fine-grained, independently-sprintable stories (house preference). Bulk is mechanical composition substitution following the Tenants template; the audit form (13.6) and the 25 `<dl>` data surfaces (13.4) carry the most surface area.
- **Risk:** Low-to-moderate. Highest risk is a11y-label/landmark churn when moving headings into `FcPageHeader` (`aria-labelledby`) and snapshot churn — contained by the guard + the real-render re-verification (13.9). Governed semantics and the "no fake/freeform textbox" safety model are untouched.
- **Timeline:** Additive M2 quality closure; does not block M0/M1; should close before MVP readiness sign-off (Epic 10/12 closure is a stated readiness gate).

**Alternatives considered & rejected:** *CSS-only patch of the overlap* — leaves the custom layout system, the `<dl>` dumps, and the unmeasured gate in place (treats the symptom). *Reopen Epic 12* — destroys its audit trail and breaks the established additive-epic pattern.

---

## Section 4 — Detailed Change Proposals

### 4.A New Epic + Stories (append to `epics.md`; register in `sprint-status.yaml`)

> **Epic 13: ChatBot UI FrontComposer Layout Composition Remediation**
> *Added by `sprint-change-proposal-2026-06-22.md`. Closes the page-level composition gap left open when Epic 10 adopted the FrontComposer shell and Epic 12 migrated leaf controls, but pages continued to hand-roll chrome with `.chatbot-*` CSS that collides with the shell. Increment: M2 release-readiness quality closure.*
>
> **Goal:** Every `Hexalith.ChatBot.UI` routable page composes through `FcPageLayout` + `FcPageHeader` and Fluent layout/data components (mirroring `Hexalith.Tenants.UI`) — no hand-rolled `.chatbot-page-header`/`.chatbot-page`/`.chatbot-command-bar`, no `<dl>` data dumps for primary content — eliminating the shell overlap and producing a clean Fluent business interface, enforced by an extended governance guard, and verified against the **real rendered app**.
>
> **Constraints:** Adapter boundary preserved. Governed semantics, accessibility labels/landmarks, non-color status cues, EN+FR localization, and the "no fake/freeform textbox" safety model preserved exactly. Fluent UI v5 and FrontComposer stay pinned.

| Story | Title | Scope |
|---|---|---|
| **13.1** | FrontComposer layout-composition governance guard (gates 13.2–13.8) | Extend `ChatBotFluentConformanceTests` (mirror `Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests`): ban hand-rolled `class="chatbot-page-header"`, page-title `<header>`, `class="chatbot-page"`, `class="chatbot-command-bar"`; require each `@page` to use `FcPageLayout` + `FcPageHeader`; ban `<dl class="chatbot-definition-list">` for primary data. Seed allowlist with today's offenders (6 headers / 2 page boxes / 3 command bars / 25 dl-lists); allowlist may only shrink (stale-entry assertion). |
| **13.2** | Adopt `FcPageLayout` + `FcPageHeader` across all 6 pages (fixes the overlap) | Replace `<header class="chatbot-page-header">` + `.chatbot-command-bar` with `<FcPageHeader Heading/Eyebrow/Description>` + `Metadata`/`Actions` slots, wrapped in `<FcPageLayout>`. Preserve `HeadingId`/`aria-labelledby` focus target. Removes the shell-overlap bug on every route. |
| **13.3** | Replace `.chatbot-page`/`.chatbot-section` content boxes with Fluent composition | Swap the hard-bordered wrappers for `FluentStack`/`FluentCard` spacing and grouping (removes the black box; establishes hierarchy and whitespace). |
| **13.4** | Migrate `<dl>` data dumps → Fluent data presentation | 25 `chatbot-definition-list` surfaces (conversation/governed/approval items, evidence/why-panels) → `FluentDataGrid` for tabular/queue data; structured `FluentStack`/`FluentText` for key-value; remove monospace for non-code values. Split across conversation + governed surfaces. |
| **13.5** | Operational dashboards → real Fluent data viz | `/operational-dashboards`: `FluentDataGrid` + status/KPI tiles (`FluentCard`) instead of stacked label rows; preserve degraded-dependency states and stable status enumeration. |
| **13.6** | Compliance audit search form → Fluent form grid | `/compliance-audit-investigation`: lay filters out in an aligned `FluentGrid`/`FluentStack` (label-above-input), not inline-wrapping; preserve all filters + opaque-id semantics. |
| **13.7** | Group sibling titled sections in `FluentAccordion` (UX "Page sections" rule) | Pages/panels with ≥2 sibling titled sections wrap them in one `FluentAccordion` (primary item expanded by default); keep single primary content (one grid/form) outside the accordion. |
| **13.8** | Retire remaining `.chatbot-*` layout CSS | Delete `.chatbot-page-header`, `.chatbot-page`, `.chatbot-command-bar`, `.chatbot-definition-list`, custom skip-link (shell/`FcPageHeader` provide these). Collapse `chatbot.tokens.css` to genuine layout-only (flex/grid/gaps the design system doesn't own). Guard allowlist must be empty. |
| **13.9** | **Real-render** cross-surface re-verification (closes the 12.9 fixture gap) | Capture **actual rendered screenshots of all 6 live surfaces** (not hand-authored fixtures); assert no shell overlap, no bordered content box, no `<dl>` dumps, empty guard allowlist; WCAG 2.2 AA in light/dark/forced-colors + EN/FR; Playwright a11y/visual gate green against the real components. |

**Guard allowlist seed (offenders):** 6 pages with `chatbot-page-header` (`ProjectWorkspace`, `GovernedOperations`, `OperationalDashboards`, `ComplianceAuditInvestigation`, `AssociationReview`, `ProjectConversation`), 2 `chatbot-page` wrappers, 3 `chatbot-command-bar`, 25 `chatbot-definition-list` components.

### 4.B UX-DR amendments (`epics.md`)

- **UX-DR1 — append:** *"Conformance is **page-composition-level**: every routable page composes through FrontComposer `FcPageLayout` + `FcPageHeader`; hand-rolled page chrome (`.chatbot-page-header`/`.chatbot-page`/`.chatbot-command-bar`) and `<dl>` data dumps for primary content are prohibited and fail the build via the ChatBot layout-composition governance guard. Carve-outs are allowlisted in `architecture.md`."*
- **UX-DR2 — append:** *"Primary data renders through Fluent data components (`FluentDataGrid`, structured `FluentStack`/`FluentText`), not monospace `<dl>` rows. Sibling titled sections group in `FluentAccordion` per the Hexalith UX Page-sections rule."*

### 4.C Epic 10/12 story notes (`epics.md`)

- Add to 10.1, 12.1, 12.8, 12.9: *"Page-level FrontComposer composition (FcPageLayout/FcPageHeader) and Fluent data presentation were out of this story's scope and are completed by Epic 13 (`sprint-change-proposal-2026-06-22.md`). Story 12.9's visual reverification used static fixtures, not the live render; Epic 13.9 re-verifies the real app."*

### 4.D Architecture amendment (`architecture.md`)

- Rename/extend the "ChatBot UI Fluent-only conformance" subsection to **"ChatBot UI FrontComposer layout composition + Fluent-only conformance"**: require `FcPageLayout`/`FcPageHeader` composition, ban hand-rolled page chrome and `<dl>` data dumps, name the extended `ChatBotFluentConformanceTests` guard, reference `Hexalith.Tenants.UI` as the canonical pattern, list carve-outs (target: none).

### 4.E Sprint status (`sprint-status.yaml`)

```yaml
  # Epic 13 added by sprint-change-proposal-2026-06-22 (ChatBot UI FrontComposer layout composition; page-level gap left open by Epics 10 & 12).
  # Story 13.1 (layout-composition guard) gates 13.2-13.8; 13.8 lands after 13.2-13.7; 13.9 re-verifies the REAL render last.
  epic-13: backlog
  13-1-frontcomposer-layout-composition-guard: backlog          # gates 13.2-13.8
  13-2-adopt-fcpagelayout-fcpageheader-across-pages: backlog
  13-3-replace-content-boxes-with-fluent-composition: backlog
  13-4-migrate-definition-lists-to-fluent-data: backlog
  13-5-operational-dashboards-fluent-data-viz: backlog
  13-6-compliance-audit-form-fluent-grid: backlog
  13-7-group-sections-in-fluent-accordion: backlog
  13-8-retire-chatbot-layout-css: backlog                       # after 13.2-13.7
  13-9-real-render-cross-surface-reverification: backlog
  epic-13-retrospective: optional
```

---

## Section 5 — Implementation Handoff

- **Scope classification:** **Moderate→Major** — backlog reorganization (new Epic 13 + 9 stories) + architecture/UX-DR amendments + guard hardening + a verification-method correction. No fundamental product replan.
- **Route to:** Product Owner / Developer (backlog reorg + dev), light Architect touch for the guard design and `architecture.md` section.
- **Deliverables:** this proposal; the Epic 13 stories + AC amendments; the extended layout-composition guard; the retired layout CSS; **real-render** screenshots + green a11y/visual gate.
- **Success criteria:**
  1. `FcPageLayout` and `FcPageHeader` used on **all 6** routable pages; **zero** hand-rolled `.chatbot-page-header`/`.chatbot-page`/`.chatbot-command-bar`.
  2. The shell-overlap bug is gone on every route (page title no longer collides with the shell top bar).
  3. Primary data renders via Fluent data components; **zero** `<dl class="chatbot-definition-list">`; no monospace for non-code values.
  4. Extended `ChatBotFluentConformanceTests` exists, is non-vacuous, passes with an **empty** allowlist.
  5. Re-verification (13.9) screenshots the **real running app**; WCAG 2.2 AA holds (light/dark/forced-colors); EN+FR intact; Playwright gate green.
  6. Release build clean (`TreatWarningsAsErrors`); adapter-boundary fitness tests still prove UI excludes Server/gateway internals; governed semantics + "no fake/freeform textbox" model demonstrably unchanged.

---

## Appendix — Evidence Index

- **Live run:** full `aspire run` topology serving the real UI at `http://localhost:5000`; 6 surfaces captured (Project Workspace, Governed Operations, Operational Dashboards, Compliance Audit, Project Conversation, Association Review). Overlap, black box, and `<dl>` dumps reproduced on every route.
- Hand-rolled header (overlap): `src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor:34`; `Components/Governed/ChatBotProjectConversationWorkspace.razor:26`.
- Shell wrap with no composition: `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor:4`.
- Offender counts (42 `.razor`): `FcPageHeader`=0, `FcPageLayout`=0, `FluentDataGrid`=0, `FluentAccordion`=0; `chatbot-page-header`×6, `chatbot-page`×2, `chatbot-command-bar`×3, `chatbot-definition-list`×25.
- FrontComposer primitives: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor`, `FcPageLayout.razor`, `FrontComposerShell.razor`.
- Reference pattern: `Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`, `TenantAuditPage.razor`; guard `Hexalith.Tenants/tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`.
- Guard gap: `ChatBotFluentConformanceTests` (Epic 12.1) bans only raw form controls + v4 tokens; no page-composition check.
- Verification gap: Story 12.9 (`_bmad-output/implementation-artifacts/12-9-cross-surface-a11y-visual-reverification.md`) asserts hand-authored fixtures, not the live render.
