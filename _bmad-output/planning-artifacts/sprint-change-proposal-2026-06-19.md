# Sprint Change Proposal — ChatBot UI Fluent v5 Component Conformance

- **Date:** 2026-06-19
- **Author:** Jerome (via Correct Course workflow)
- **Trigger:** UI does not show an enterprise-quality chatbot design; suspected non-use of Fluent UI Blazor v5 / Hexalith.FrontComposer.
- **Mode:** Batch
- **Chosen approach:** Full Fluent v5 migration of all raw-HTML surfaces + add a Fluent-only governance guard.
- **Scope classification:** **Moderate→Major** (reworks the surface of a `done` epic + backlog reorganization + planning-artifact AC hardening + one architecture amendment). Product scope, safety model, command spine, and backend are **unchanged**.
- **Evidence basis:** Static code analysis of `src/Hexalith.ChatBot.UI` + planning artifacts (`epics.md`, `architecture.md`) + `sprint-status.yaml`. No running-app screenshots were captured; the "raw HTML, not Fluent components" finding is provable from source and does not require them.

---

## Section 1 — Issue Summary

The ChatBot UI renders as lightly-CSS'd plain HTML inside a Fluent shell, which reads as non-enterprise. The user's diagnosis is correct and is corroborated by the project's own specifications.

**What is correct (shell adoption succeeded):**

| Layer | State | Verdict |
|---|---|---|
| Package refs (`Microsoft.FluentUI.AspNetCore.Components`, `Hexalith.FrontComposer.Shell`) | Present | ✅ |
| Startup wiring (`AddFluentUIComponents` → `AddHexalithFrontComposerQuickstart` → `AddHexalithDomain<TMarker>` → `AddHexalithEventStore`) | Correct, in prescribed order (`Program.cs`) | ✅ |
| `MainLayout.razor` = `<FrontComposerShell AppTitle="Hexalith ChatBot">@Body</FrontComposerShell>` | Yes | ✅ |

**What is wrong (the interior never migrated to Fluent v5 components):**

- **31 of 39** real components/pages use **zero** Fluent components.
- Across the entire UI there are only **9 Fluent component usages total** (6 `FluentBadge`, 3 `FluentButton`).
- Raw interactive controls remain in **12 files**: `<button>`×8 files, `<input>`×5 files, `<select>`×2 files, `<textarea>`×2 files (plus raw `<label>`×7 files).
- A **1,323-line** `wwwroot/css/chatbot.tokens.css` hand-rolls a parallel design system (`.chatbot-button`, type ramp `--chatbot-type-page-title-size: 28px`, weights, line-heights, radii, spacing). This is the **temporary Story 1.14 token-alias bridge that Story 10.1 was supposed to retire** — it was instead "reconciled" (aliased to Fluent colors) and grew into the forbidden custom design system.

**Root cause:** In Fluent UI Blazor v5 a raw `<button>/<input>/<select>/<textarea>` is **never upgraded** — it falls back to unstyled browser rendering and drops the accessibility affordances Fluent components provide. The ChatBot interior is built almost entirely from such raw controls, so it cannot look like Microsoft Fluent V2 no matter what the shell does.

**Why it shipped undetected (the real defect is a specification + verification gap):**

- **Epic 10 is `done`** — Stories 10.1, 10.2, 10.3, 10.4, 10.5, and **10.7 "Cross-surface a11y / visual / parity re-verification"** are all `done`.
- None of those stories' acceptance criteria operationalized **UX-DR1** ("do not invent a custom chatbot design system") or **UX-DR2** (Fluent UI v5 tokens) into a *testable* "use Fluent v5 **components**, no raw HTML controls" criterion. The ACs talk about the shell, semantic tokens, and accessibility labels — all satisfiable with raw HTML + a custom CSS token layer.
- `ChatBotSemanticTokenContractTests` **validates the custom CSS** (e.g. `StylesheetShouldMapSemanticColorsOnlyToFluentOrFrontComposerVariables`) — it effectively blesses the divergence instead of forbidding it.
- There is **no Fluent-only governance guard** in the ChatBot test suite, unlike `Hexalith.FrontComposer` (`FluentConformanceTests`) and `Hexalith.Tenants.UI` (`DomainUiFluentConformanceTests`).

**Governing requirements being violated:**

- **UX-DR1** (`epics.md:358`): *"Inherit the Fluent UI v5 → FrontComposer → DESIGN.md visual chain; do not invent a custom chatbot design system."*
- **UX-DR2** (`epics.md:359`): semantic color system via Fluent UI v5 tokens (through FrontComposer CSS custom properties).
- **Story 10.1 AC** (`epics.md:2919`): *"the Story 1.14 token-alias layer is retired or reconciled … no duplicate/raw-hex mappings."*
- **FrontComposer project rule** (consumed read-only): *"every .razor page/component — Shell, samples, and domain consumers — uses FrontComposer or Fluent v5 components, never raw `<button>/<input>/<select>/<textarea>`."* `Hexalith.ChatBot.UI` is a domain consumer of FrontComposer and is bound by this rule.

---

## Section 2 — Impact Analysis

### Epic Impact

- **Epic 10 (Interactive Chat Surface & FrontComposer Shell Adoption) — `done`, but its visual goal was missed.** The shell-adoption mechanics (refs, wiring, `<FrontComposerShell>`) are genuinely complete; the *component-level* visual inheritance (UX-DR1/DR2) was never enforced. `epic-10-retrospective` is still `optional` (not run) — the lesson should be captured there.
- **No other epic's behavior is affected.** Command spine, governance/approval semantics, backend, CLI, and MCP are untouched — this is a UI rendering-layer correction.

### Story Impact

| Story | Status | Impact |
|---|---|---|
| 1.14 (design system / tokens) | done | Origin of the temporary token-alias bridge; its "retire later" intent was never completed. |
| 10.1 (Shell integration) | done | AC "token-alias layer retired or reconciled" was satisfied as "reconciled"; the retire path is now owed. |
| 10.2 (Migrate S1/S2/S3 governed surfaces) | done | Surfaces moved into the shell but remained raw HTML. |
| 10.3 (Migrate operational surfaces) | done | Same — operational pages remained raw HTML. |
| 10.4 (Project Workspace landing) | done | Layout route fine; interior components raw HTML. |
| 10.5 (Governed chat composer) | done | `ChatBotGovernedComposer` is raw `<button>/<textarea>/<label>`. |
| 10.7 (a11y/visual/parity re-verification) | done | Verified the custom design system, not Fluent-component conformance. |

### Artifact Conflicts (documents needing updates)

- **`epics.md`** — UX-DR1/UX-DR2 need an explicit, testable Fluent-component sub-criterion; Epic 10 stories need a retroactive note pointing at the remediation epic; a new remediation epic must be added.
- **`architecture.md`** — needs a "ChatBot UI Fluent-only conformance" section mirroring FrontComposer architecture.md §4.1 (guard scope + documented carve-outs).
- **`sprint-status.yaml`** — add the new epic + stories; set `epic-10-retrospective` to capture the lesson.
- **Tests** — `ChatBotSemanticTokenContractTests` must be reframed to validate Fluent-token mapping only (stop blessing custom primitives); a new Fluent-only governance guard must be added.

### Technical Impact (code)

- **12 files** with raw interactive controls to migrate (full list in Section 4), plus ~19 further read-only/structural components to move to Fluent surface primitives (`FluentCard`/`FluentStack`/`FluentText`).
- **`wwwroot/css/chatbot.tokens.css`** (1,323 lines) collapses to genuine layout-only CSS (flex/grid/gaps the design system doesn't own); `.chatbot-button`, type-ramp, and other Fluent-provided primitives are deleted.
- **No dependency, deployment, or infrastructure changes.** Fluent UI v5 is already pinned identically to FrontComposer (`5.0.0-rc.3-26138.1`); the adapter boundary (UI → Client/ServiceDefaults/FrontComposer only) is preserved.

---

## Section 3 — Recommended Approach

**Direct Adjustment** (per user decision: *Full Fluent v5 migration*). Add a new **Epic 12 — ChatBot UI Fluent v5 Component Conformance Remediation**, rather than re-opening the `done` Epic 10, to preserve Epic 10's audit trail and mirror how Epics 10 and 11 were themselves added by prior correct-course proposals. Harden the under-specified planning artifacts (UX-DR1/DR2, architecture conformance section) so the gap cannot recur, and run the Epic 10 retrospective to capture the lesson.

**Guard-first sequencing** (mirrors the 10.6a→10.6b ADR-first pattern): Story 12.1 lands the governance guard with an allowlist seeded to today's 12 offenders; the allowlist may only shrink. Each subsequent migration story deletes its files from the allowlist, so progress is build-enforced and measurable, ending at an empty allowlist.

- **Effort estimate:** ~9 fine-grained, independently-sprintable stories (granularity per house preference). Bulk is mechanical component substitution; the editors (12.6) and the audit page (12.7) carry the most surface area.
- **Risk:** Low-to-moderate. Highest risk is Verify-snapshot churn and a11y-label regressions during substitution — contained by the guard + Story 12.9 re-verification. Fluent UI v5 remaining RC is a pre-existing, pinned constraint, not introduced here.
- **Timeline impact:** Additive M2 remediation. Does not block M0/M1; should close before MVP readiness sign-off since Epic 10 closure is a stated readiness gate.

**Alternatives considered & rejected:** *Phased (hotspots only)* — leaves a permanent mixed-paradigm UI and a non-empty guard allowlist (rejected per user's "Full" choice). *Plan-only* — defers the visible quality win (rejected per user's "Full" choice).

---

## Section 4 — Detailed Change Proposals

### 4.A New Epic + Stories (append to `epics.md`; register in `sprint-status.yaml`)

> **Epic 12: ChatBot UI Fluent v5 Component Conformance Remediation**
> *Added by `sprint-change-proposal-2026-06-19.md`. Closes the UX-DR1/UX-DR2 component-level gap left open when Epic 10 adopted the FrontComposer Shell but kept interior surfaces as raw HTML + a custom CSS design system. Increment: M2 release-readiness quality closure.*
>
> **Goal:** Every `Hexalith.ChatBot.UI` `.razor` page/component renders through FrontComposer or Fluent UI v5 components (Microsoft Fluent V2) — no raw `<button>/<input>/<select>/<textarea>` — and the custom `chatbot.tokens.css` design system is retired to layout-only CSS, satisfying UX-DR1/UX-DR2 and the FrontComposer Fluent-only rule, enforced by a governance guard.
>
> **Constraints:** Adapter boundary preserved (UI → Client/ServiceDefaults/FrontComposer only). Governed semantics, accessibility labels, non-color status cues, EN+FR localization, and "no fake/freeform textbox" safety model preserved exactly. Fluent UI v5 stays pinned at `5.0.0-rc.3-26138.1`.

| Story | Title | Scope |
|---|---|---|
| **12.1** | Fluent-only + no-theme-redefinition governance guard (gates 12.2–12.8) | Add `ChatBotFluentConformanceTests` (Governance trait) banning raw `<button>/<input>/<select>/<textarea>` in `src/Hexalith.ChatBot.UI`, mirroring `Hexalith.FrontComposer` `FluentConformanceTests` / `Hexalith.Tenants.UI` `DomainUiFluentConformanceTests`; raw `<a>` allowed. Seed allowlist with today's 12 offenders (allowlist may only shrink; stale-entry assertion enforces deletion). Add a CSS guard banning re-creation of Fluent-provided primitives (button/heading-ramp) and legacy v4/FAST tokens. |
| **12.2** | Migrate governed chat composer → Fluent v5 | `ChatBotGovernedComposer` (3×button, 1×textarea, 1×label) → `FluentButton`/`FluentTextArea`/`FluentLabel`; preserve UX-DR34 shortcut suppression, focus management, validation `role="alert"`. Remove from guard allowlist. |
| **12.3** | Migrate conversation stream + item components → Fluent v5 | `ChatBotConversationStream`, all `*ConversationItem`, `ChatBotConversationShell`, `ChatBotConversationItemReviewHistory` → `FluentCard`/`FluentStack`/`FluentText`; preserve "not a chat transcript" read-projection semantics. |
| **12.4** | Migrate association review surface → Fluent v5 | `ChatBotAssociationReviewActions` (2×textarea/2×label), `ChatBotAssociationCandidateRow` (1×button), `ChatBotAssociationEvidenceComparison`, `Pages/AssociationReview`. Remove from allowlist. |
| **12.5** | Migrate approval & governed-action surfaces → Fluent v5 | `ChatBotApprovalConversationItem` (5×button), `ChatBotWhyProjectPanel` (2×button), `ChatBotTaskIntentReviewPanel` (1×button/1×input/1×label), `ChatBotGovernedAction`, `ChatBotApprovalQueuePriorityView`. Remove from allowlist. |
| **12.6** | Migrate policy/notification/escalation editors → Fluent v5 | `ChatBotEscalationPolicyEditor` (3×input/3×select/2×label), `ChatBotNotificationRoutingEditor` (2×input/2×select/2×label), `ChatBotTenantPolicyEditor` (1×input/1×label) → `FluentTextField`/`FluentSelect`/`FluentNumberField`/`FluentLabel`. Highest form surface area. Remove from allowlist. |
| **12.7** | Migrate operational dashboards + compliance audit page → Fluent v5 | `Pages/ComplianceAuditInvestigation` (5×button/12×input/12×label — largest single offender), `Pages/OperationalDashboards`, `Pages/GovernedOperations` → `FluentDataGrid`/`FluentSearch`/`FluentSelect` filters; preserve stable filters + degraded-dependency states. Remove from allowlist. |
| **12.8** | Retire `chatbot.tokens.css` custom design system | Collapse the 1,323-line stylesheet to layout-only CSS (flex/grid/gaps the design system doesn't own); delete `.chatbot-button`, type-ramp, weights, radii Fluent now provides. Reframe `ChatBotSemanticTokenContractTests` to validate Fluent-token mapping only (stop asserting custom primitives). Lands after 12.2–12.7 so removing classes breaks nothing. Guard allowlist must be empty at completion. |
| **12.9** | Cross-surface a11y / visual re-verification (re-run 10.7 against Fluent) | WCAG 2.2 AA in light/dark/forced-colors + EN/FR; Verify snapshots refreshed intentionally; Playwright a11y/visual gate green; confirm guard allowlist empty and no legacy v4/FAST tokens. |

**Guard allowlist seed (12 files with raw interactive controls):** `ChatBotActorBadge`, `ChatBotApprovalConversationItem`, `ChatBotAssociationCandidateRow`, `ChatBotAssociationReviewActions`, `ChatBotEscalationPolicyEditor`, `ChatBotEvidenceChip`, `ChatBotGovernedComposer`, `ChatBotNotificationRoutingEditor`, `ChatBotTaskIntentReviewPanel`, `ChatBotTenantPolicyEditor`, `ChatBotWhyProjectPanel`, `ComplianceAuditInvestigation`.

### 4.B UX-DR amendments (`epics.md`)

- **UX-DR1 — append:** *"Conformance is component-level and build-enforced: every `.razor` page/component uses FrontComposer or Fluent v5 components; raw `<button>/<input>/<select>/<textarea>` are prohibited (raw `<a>` nav links allowed) and fail the build via the ChatBot Fluent-only governance guard. Documented carve-outs are allowlisted in `architecture.md`."*
- **UX-DR2 — append:** *"Hand-authored CSS must not recreate primitives a Fluent component provides (button styling, heading type-ramp via font-size/weight/line-height, foreground role via `color:`) nor use legacy v4/FAST tokens (`--type-ramp-*`, `--neutral-*`, `--accent-*`, `--palette-*`, `--design-unit`). Custom CSS is permitted only for layout the design system does not own."*

### 4.C Epic 10 story notes (`epics.md`)

- Add to Stories 10.1, 10.2, 10.3, 10.5, 10.7: *"Component-level Fluent v5 conformance (UX-DR1/DR2) was under-specified in this story's ACs and is completed by Epic 12 (`sprint-change-proposal-2026-06-19.md`). The Story 1.14 token-alias bridge is retired in Story 12.8."*

### 4.D Architecture amendment (`architecture.md`)

- Add a **"ChatBot UI Fluent-only conformance"** subsection (mirroring FrontComposer architecture.md §4.1): states the Fluent-only + no-theme-redefinition rules for `Hexalith.ChatBot.UI`, names the `ChatBotFluentConformanceTests` guard, and lists any documented carve-outs (target: none).

### 4.E Sprint status (`sprint-status.yaml`)

```yaml
  # Epic 12 added by sprint-change-proposal-2026-06-19 (ChatBot UI Fluent v5 component conformance remediation; UX-DR1/DR2 gap left open by Epic 10).
  epic-12: backlog
  12-1-fluent-only-governance-guard: backlog          # gates 12.2-12.8
  12-2-migrate-governed-chat-composer-to-fluent: backlog
  12-3-migrate-conversation-stream-and-items-to-fluent: backlog
  12-4-migrate-association-review-surface-to-fluent: backlog
  12-5-migrate-approval-and-governed-action-surfaces-to-fluent: backlog
  12-6-migrate-policy-notification-escalation-editors-to-fluent: backlog
  12-7-migrate-operational-and-audit-pages-to-fluent: backlog
  12-8-retire-chatbot-tokens-css-custom-design-system: backlog   # after 12.2-12.7
  12-9-cross-surface-a11y-visual-reverification: backlog
  epic-12-retrospective: optional
```
- Set `epic-10-retrospective: backlog` (run it to capture the under-specified-AC lesson) — or record the lesson directly in `_bmad-output/story-automator/learnings.md`.

---

## Section 5 — Implementation Handoff

- **Scope classification:** **Moderate→Major** — backlog reorganization (new Epic 12 + 9 stories) plus a small architecture amendment and planning-artifact AC hardening. No fundamental product replan.
- **Route to:** Product Owner / Developer (backlog reorg + dev) with a light Architect touch for the conformance-guard design and the `architecture.md` section.
- **Deliverables:** this proposal; the Epic 12 stories + AC amendments above; the new governance guard; the retired custom CSS; refreshed snapshots and green a11y/visual gate.
- **Success criteria:**
  1. `ChatBotFluentConformanceTests` exists, is non-vacuous, and passes with an **empty** allowlist.
  2. Zero raw `<button>/<input>/<select>/<textarea>` in `src/Hexalith.ChatBot.UI/**/*.razor`.
  3. `chatbot.tokens.css` contains no Fluent-provided primitives or legacy v4/FAST tokens.
  4. WCAG 2.2 AA holds (light/dark/forced-colors), EN+FR intact, Playwright a11y/visual gate green.
  5. Release build clean (`TreatWarningsAsErrors`), default test lane green; adapter-boundary fitness tests still prove UI excludes Server/gateway internals.
  6. Governed semantics and the "no fake/freeform textbox" safety model are demonstrably unchanged.

---

## Appendix — Evidence Index

- Composer raw controls: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:33-71`
- Stream raw structure: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor:4-55`
- Custom design system: `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` (1,323 lines)
- Shell adoption (correct): `src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor:4`; `src/Hexalith.ChatBot.UI/Program.cs:17-21`
- Specs: `epics.md:358` (UX-DR1), `epics.md:359` (UX-DR2), `epics.md:2919` (Story 10.1 AC)
- Guard template: `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`
- Statuses: `_bmad-output/implementation-artifacts/sprint-status.yaml:187-197` (Epic 10 `done`)
</content>
</invoke>
