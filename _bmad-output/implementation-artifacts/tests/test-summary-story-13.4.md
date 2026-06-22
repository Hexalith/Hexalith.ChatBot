# Test Summary - Story 13.4

Date: 2026-06-22
Baseline commit: 21be905

## Scope

Story 13.4 migrates the 23 hand-rolled `<dl class="chatbot-definition-list">` monospace data
dumps it owns in `Hexalith.ChatBot.UI` to Fluent data presentation, and burns the Story 13.1
`DefinitionListAllowlist` down from **25 → 2**. Repeated/queue surfaces and fixed key-value
metadata blocks now render through structured `FluentStack` + `FluentText` rows (label
`FluentText` + value `FluentText`/`<code class="chatbot-code">`), mirroring the
`Hexalith.Tenants.UI` reference (`AuditDataGrid`/`MyTenantsPage`). Monospace (`<code>`) is kept
only for genuine opaque tokens (ids, refs, reason/catalog codes, raw enum tokens, versions,
fingerprints, opaque query expressions); localized status/kind labels, prose, display names and
timestamps render as plain `FluentText`.

**Rendering-layer only.** Governed read-projection semantics, the "no fake/freeform textbox"
safety model, every `aria-label`/`aria-labelledby`/`role`/`aria-live`/`<time datetime>` and
`data-*` marker, every `@if`/null-guard conditional row, the non-color status cues (UX-DR4), and
the EN+FR localization keys are preserved exactly. No new localization key was added (incl.
`ChatBotTaskIntentReviewPanel`'s hard-coded English labels, kept verbatim). No backend /
CommandGateway / CLI / MCP / SignalR / Dapr / EventStore change and no sibling-submodule edit.

## Scope decision (the one boundary call)

The 25-entry `DefinitionListAllowlist` includes two dedicated-page surfaces owned by later
stories: `Components/Pages/OperationalDashboards.razor` (Story 13.5) and
`Components/Pages/ComplianceAuditInvestigation.razor` (Story 13.6). Story 13.4 migrates the other
**23** files and removes their 23 entries, leaving the allowlist at exactly:

```
["Components/Pages/ComplianceAuditInvestigation.razor", "Components/Pages/OperationalDashboards.razor"]
```

Story 13.5 removes the dashboards entry, 13.6 removes the compliance-audit entry, and Story 13.8
verifies the list is empty. The 2 page-owned files were **not** edited by 13.4 — their `<dl>`
dumps remain in place (ComplianceAuditInvestigation L157; OperationalDashboards L96/L175).

## Commands

| Command | Result |
| --- | --- |
| `dotnet restore Hexalith.ChatBot.slnx` | Passed: all projects up-to-date. |
| `dotnet build src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj --no-restore -m:1 -nodeReuse:false` | Passed: 0 warnings, 0 errors (TreatWarningsAsErrors). |
| `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` | Passed: 0 warnings, 0 errors (TreatWarningsAsErrors). |
| `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore --no-build -m:1 -nodeReuse:false` | Passed: 26 total, 0 failed, 0 skipped. |
| `... (full project, regression)` | Passed: 193 total, 0 failed, 0 skipped. |
| `rg -l 'chatbot-definition-list' src/Hexalith.ChatBot.UI --glob '*.razor'` | Returns exactly the 2 page-owned files (`ComplianceAuditInvestigation.razor`, `OperationalDashboards.razor`). |
| `git diff --check` | Passed: clean. |

The Governance lane total of 26 is the existing 6 Story 12.1 `ChatBotFluentConformanceTests` plus
the 20 Story 13.1 `ChatBotLayoutCompositionConformanceTests` (test count unchanged — Story 13.4
only mutates the seeded `DefinitionListAllowlist` data, not the test methods). Both classes carry
`[Trait("Category", "Governance")]`. `ChatBotFluentConformanceTests` is green, confirming no raw
`<button>/<input>/<select>/<textarea>` and no legacy v4/FAST tokens were reintroduced;
`FluentStack`/`FluentText`/`FluentCard`/`FluentBadge` are Fluent v5 and allowed.

## Guard list deltas

| Guard list (`ChatBotLayoutCompositionConformanceTests`) | 13.1 seed | After 13.4 | Status |
| --- | --- | --- | --- |
| `DefinitionListAllowlist` | 25 | **2** | 13.4 removed 23 (✓) |
| `PageContentBoxAllowlist` | 6 | 6 | unchanged (Story 13.3) |
| `PageHeaderChromeAllowlist` | 0 | 0 | unchanged |
| `CommandBarAllowlist` | 0 | 0 | unchanged |
| `NotYetComposedPageBacklog` | 0 | 0 | unchanged |

The three ratchets (missing-path, offender-outside-allowlist, stale-entry) are all green for the
shrunk list: every owned file is gone from the scan, and the 2 remaining entries still contain the
banned token (so neither is a stale entry).

## Migration treatment per owned file

All 23 owned `.razor` files: zero `chatbot-definition-list` tokens and zero `<dl>/<dt>/<dd>`
markup remain (the only residual matches are explanatory `// Story 13.4: the former <dl> …`
comments, which use no quoted/whitespace-bounded `chatbot-definition-list` token and so do not
trip the guard). FluentDataGrid was not required: the single-column repeated lists and the
per-row queue surfaces use structured `FluentStack` rows (an alternative the story's Dev Notes
explicitly permit — "render … as columns or a structured per-entry FluentStack"), which preserves
the per-row action buttons, status banners and `data-*` markers that a tabular grid could not
carry. Spot-verified AC3/AC4 preservation on: `ChatBotApprovalQueuePriorityView` (container
`aria-label`), `ChatBotTenantPolicyEditor` (`aria-label` + `data-mailbox-status-row` /
`data-mailbox-action-row`), `GovernedOperations` (filter `aria-label`; per-row `data-chatbot-*`),
`ChatBotEmailConversationItem` (conditional `*If` rows), `ChatBotConversationItemReviewHistory` /
`ChatBotConversationItemStatusSummary` (`span`+`code` split, `<time datetime>` keeps machine value
and drops monospace).

## Visual check (AC6)

The build-enforced guards above are the authoritative truth signal for 13.4 (per the story's git
intelligence: allowlist 25→2 + Fluent conformance). The live `aspire run` visual confirmation was
**not** executed in this headless session — the Tier-3 Aspire/DAPR stack is heavy and flaky in
this sandbox (see memory `tier3-live-dapr-run`) and the **full real-render screenshot gate is
owned by Story 13.9**, which replaces the static E2E fixtures with real-render assertions. The
migrated surfaces contain no `<dl>` primary-data dumps and render Fluent data components by
construction (verified by source scan + 0/0 build + green guard).

## Changed Files (Story 13.4)

- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalQueuePriorityView.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationEvidenceComparison.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAssociationReviewActions.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemClassificationBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEscalationPolicyEditor.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotNotificationRoutingEditor.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTaskIntentReviewPanel.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotTenantPolicyEditor.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLayoutCompositionConformanceTests.cs`
- `_bmad-output/implementation-artifacts/13-4-migrate-definition-lists-to-fluent-data.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-13.4.md`

Note: `Components/Pages/OperationalDashboards.razor`, `Components/Pages/ComplianceAuditInvestigation.razor`,
`Components/Pages/ProjectConversation.razor` and `Components/_Imports.razor` also show as modified
in the working tree, but those edits are the concurrent **in-progress Story 13.2** FcPageHeader /
FcPageLayout adoption (`@using Hexalith.FrontComposer.Contracts.Rendering`, header/command-bar
chrome → `FcPageHeader`/`FluentStack`), **not** Story 13.4 — 13.4 made no edit to those files.

---

## QA Automation Addendum — `qa-generate-e2e-tests` workflow (2026-06-22)

Ran the `bmad-qa-generate-e2e-tests` workflow against the migrated feature. **Detected gap:** the
Story 13.1 guard (`ChatBotLayoutCompositionConformanceTests`) only asserts the **absence** of the
`chatbot-definition-list` class token — which the story's own "Regression traps" section flags as
gameable (delete the class, keep the `<dl>` monospace dump). No test **positively** locked in that
the migration produced Fluent data presentation, that no `<dl>/<dt>/<dd>` markup survives, that the
AC3 monospace rule holds, or that the AC4 aria/`data-*`/`<time>` machine attributes were preserved.
(The ChatBot UI has zero bUnit — see memory `chatbot-ui-no-bunit-test-strategy` — so the gap is
filled with a source-scan contract suite in the build-gated Governance lane, the same family as the
existing guards; the real-render screenshot gate remains Story 13.9.)

**Generated:** `tests/Hexalith.ChatBot.UI.Tests/Story13DefinitionListMigrationTests.cs`
(`[Trait("Category", "Governance")]`, xUnit v3 + Shouldly, source-scan, no bUnit).

| # | Test | Covers | Asserts |
| --- | --- | --- | --- |
| 1 | `Migrated_surfaces_render_data_through_fluent_components_and_drop_definition_list_markup` | AC1, anti-gaming trap #1 | each of the 23 owned files contains `FluentStack`, has **no** `chatbot-definition-list` class, and (after stripping `@*…*@`/`<!--…-->`/full-line `//` comments) has **no** `<dl>/<dt>/<dd>` element markup |
| 2 | `Definition_list_class_end_state_is_exactly_the_two_page_owned_surfaces` | AC2 | a fresh scan of every `.razor` under the UI root → exactly `ComplianceAuditInvestigation.razor` + `OperationalDashboards.razor` still carry the class (independent of the guard's internal array) |
| 3 | `Migration_preserves_accessibility_data_and_time_machine_attributes` | AC4 | `data-mailbox-status-row`/`data-mailbox-action-row` survive in `ChatBotTenantPolicyEditor`; the 4 editor/queue containers + `GovernedOperations` filter keep a localized `aria-label="@UiText[…]"`; 5 conversation surfaces keep `<time datetime="…">` |
| 4 | `Timestamps_drop_monospace_but_opaque_tokens_keep_chatbot_code` | AC3 | no owned file has a `<time … chatbot-code>` element; every owned file still uses `chatbot-code` for genuine tokens (monospace not blanket-removed) |
| 5 | `Task_intent_review_panel_keeps_its_preexisting_hard_coded_labels` | AC4 / Tasks note | `ChatBotTaskIntentReviewPanel` keeps the hard-coded `"Project"`/`"Source version"`/`"Correlation"` labels verbatim (no new localization key) |
| 6-7 | `Definition_list_element_detector_…` (9 cases), `Monospace_time_detector_…` (3 cases) | detector-fixture pins | the comment-strip + element/`<time>` regexes match real tags but not comment references to the former markup, so a future edit cannot silently re-open the bypass |

**Commands (QA addendum):**

| Command | Result |
| --- | --- |
| `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj -m:1 -nodeReuse:false` | Passed: 0 warnings, 0 errors (TreatWarningsAsErrors). |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --no-build --filter "FullyQualifiedName~Story13DefinitionListMigrationTests"` | Passed: **17** total, 0 failed, 0 skipped. |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --no-build --filter "Category=Governance"` | Passed: **43** total (was 26 + 17 new), 0 failed, 0 skipped. |
| `DiffEngine_Disabled=true dotnet test …UI.Tests… --no-build` (full project) | Passed: **210** total, 0 failed, 0 skipped. |
| `git diff --check` | Passed: clean. |

**API tests:** none generated — Story 13.4 is a pure UI rendering-layer migration with no
endpoint/service surface (AC explicitly: no backend/CommandGateway/CLI/MCP/SignalR/Dapr/EventStore
change).

**Coverage:** ACs 1–4 are now positively guarded by source-scan contract tests (previously only the
class-absence half of AC1/AC2 was). AC6's live `aspire run` visual confirmation and the full
real-render gate remain Story 13.9's scope (unchanged by this addendum).

**Added file:** `tests/Hexalith.ChatBot.UI.Tests/Story13DefinitionListMigrationTests.cs` (test-only;
no production source touched).
