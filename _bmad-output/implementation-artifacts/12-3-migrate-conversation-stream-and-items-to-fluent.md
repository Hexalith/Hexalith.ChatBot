---
baseline_commit: 6266d0c
---

# Story 12.3: Migrate conversation stream and item components to Fluent v5

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-21. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As an authorized ChatBot user,
I want the conversation stream and its item surfaces rendered with Fluent v5 components,
so that read-projection conversation context inherits the FrontComposer visual system without losing governed status, evidence, redaction, or accessibility semantics.

## Acceptance Criteria

1. **Conversation shell and stream use Fluent layout/surface/text primitives.** Given `ChatBotConversationShell` and `ChatBotConversationStream`, when migrated, then layout-only wrapper `<div>` / custom section styling is replaced where appropriate with `FluentStack`, stream/item containing surfaces use `FluentCard`, titles and visible labels use `FluentText`, and existing landmarks remain intact: shell `aria-label`, main `role="region"`, complementary `role="complementary"`, stream `aria-labelledby`, ordered list semantics, and `data-chatbot-conversation-stream="metadata-only"`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.3`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`]

2. **All conversation item read-projection surfaces are Fluent-composed without changing data contracts.** Given `ChatBotEmailConversationItem`, `ChatBotParticipantConversationItem`, `ChatBotAttachmentConversationItem`, `ChatBotDecisionConversationItem`, `ChatBotFailureStateConversationItem`, `ChatBotAiOutcomeConversationItem`, `ChatBotApprovalConversationItem`, `ChatBotConversationItemStatusSummary`, `ChatBotConversationItemClassificationBadge`, and `ChatBotConversationItemReviewHistory`, when migrated, then their primary surfaces render through `FluentCard`/`FluentStack`/`FluentText` and existing `FluentBadge`/chip affordances, while preserving item kind/id data attributes, `tabindex="0"` focusability, accessible names, actor attribution, timestamps, classification metadata, status summary facets, review-history ordering, redaction labels, retention/schema/source version fields, correlation ids, and safe-next-action values. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.3`; `_bmad-output/planning-artifacts/epics.md#Epic 3`; `src/Hexalith.ChatBot.UI/Components/Governed/*ConversationItem.razor`]

3. **The stream remains a governed read projection, not a chat transcript.** Given S1 Conversation Detail, when the migrated stream renders email, participant, attachment, decision/correction, approval, failure/retry, and AI outcome items, then system decisions remain labelled as system/background decisions, AI-generated summaries remain distinct from source evidence, failures use catalogued messages rather than raw errors, unauthorized/restricted attachment and participant details stay redacted, and command/projection-pending states continue to show partial-success identity instead of false completion. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR6`; `_bmad-output/planning-artifacts/epics.md#Story 3.1-3.11`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

4. **Conversation support primitives used by the stream are migrated enough to shrink the guard backlog.** Given `ChatBotActorBadge` and `ChatBotEvidenceChip` are used throughout the Story 12.3 item surfaces, when this story is complete, then their raw lowercase `<button>` affordances are replaced by Fluent v5 components, their accessible labels / `aria-disabled` / `aria-describedby` / unresolved-action semantics remain, and `Components/Governed/ChatBotActorBadge.razor` plus `Components/Governed/ChatBotEvidenceChip.razor` are removed from `RawControlMigrationBacklog`. `ChatBotApprovalConversationItem` remains in the raw-control backlog if its decision buttons are still present, because approval/governed-action controls are Story 12.5 scope. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `_bmad-output/planning-artifacts/epics.md#Story 12.1`; `_bmad-output/planning-artifacts/epics.md#Story 12.5`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor`]

5. **Accessibility, localization, and non-color status cues are preserved.** Given the migrated stream in light, dark, forced-colors, English, and French contexts, when items render, then WCAG 2.2 AA expectations remain: labels announce actor type and status, status meaning is not conveyed by color alone, repeated landmarks keep unique labels, projection-pending live-region announcements remain deduplicated, unavailable reasons are keyboard reachable, locale-aware dates/numbers/confidence labels remain, and machine codes / ids stay untranslated. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR4`; `_bmad-output/planning-artifacts/epics.md#UX-DR35-UX-DR45`; `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation and audit semantics`]

6. **Tests and snapshots are updated intentionally without weakening Epic 12 governance.** Given the focused UI, source-contract, governance, and affected E2E lanes, when updated, then tests assert the required Fluent tags, verify no new raw lowercase controls outside the remaining temporary backlog, keep raw-tag-aware checks case-sensitive so PascalCase Fluent tags do not false-fail, update any Verify/bUnit baselines intentionally, and record exact validation commands/results in `_bmad-output/implementation-artifacts/tests/test-summary-story-12.3.md`. [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`; `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`; `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs`]

7. **Scope remains a rendering-layer correction only.** Given Epic 12 constraints, when this story is complete, then there are no package upgrades, no Fluent version churn, no backend / CommandGateway / CLI / MCP / SignalR behavior changes, no edits to sibling submodules, no generated `obj/**/generated/HexalithFrontComposer/**` edits, no wholesale `chatbot.tokens.css` retirement beyond removing newly obsolete conversation-item primitive usage, and no migration of association review, approval action buttons, policy editors, operational dashboards, audit page filters, or final cross-surface verification owned by Stories 12.4-12.9. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `Hexalith.AI.Tools/hexalith-llm-instructions.md`]

## Tasks / Subtasks

- [x] Migrate conversation shell and stream containers (AC: 1, 3, 5)
  - [x] Replace shell layout-only wrapper `<div>` elements with `FluentStack` where the wrapper exists only for spacing/alignment; keep `section`, `aside`, and landmark roles where they carry semantics.
  - [x] Convert `ChatBotConversationStream` title rendering to `FluentText` and use `FluentStack`/`FluentCard` for stream grouping without breaking the ordered list/entry structure.
  - [x] Preserve empty-state behavior through `ChatBotBlockedState`; do not replace it with an unlabelled visual placeholder.
  - [x] Keep `data-chatbot-conversation-stream="metadata-only"` and the read-projection posture visible in source/tests.

- [x] Migrate conversation item surface components to Fluent primitives (AC: 2, 3, 5)
  - [x] Migrate `ChatBotEmailConversationItem` to `FluentCard`/`FluentStack`/`FluentText`, preserving source mailbox/provider ids, source timestamps, association/project ids, lifecycle state, confidence, threshold band, safe-next-action, correlation id, evidence chips, why-this-project callback, and review history.
  - [x] Migrate `ChatBotParticipantConversationItem`, preserving restricted/unresolved participant redaction, unavailable reasons, party/source participant ids, evidence fingerprint, allowed review actions, mailbox/lifecycle/safe-next-action/correlation metadata, and participant actor category labels.
  - [x] Migrate `ChatBotAttachmentConversationItem`, preserving redacted display-name behavior, scan/storage/capture/duplicate/retry/AI-eligibility states, authorized file/folder references, unavailable reason, allowed-action text, and redaction-safe metadata.
  - [x] Migrate `ChatBotDecisionConversationItem`, preserving association/correction decision fields, evidence summary, why-this-project callback, supersedes/superseded-by chain, propagation progress/store keys, corrected-context stale marker, redaction/retention/schema/source version fields, and unavailable-rationale explanation.
  - [x] Migrate `ChatBotFailureStateConversationItem`, preserving versioned message-catalog headline/reason, failure status/category/scope/reason code, retry/duplicate/degraded/escalation/audit metadata, terminal rule explanation, and no raw exception text.
  - [x] Migrate `ChatBotAiOutcomeConversationItem`, preserving risk action mapping, proposal/request/context/command/approval/audit/failure metadata, source-evidence section, generated-summary distinction, provenance text, and metadata-only reason.
  - [x] Migrate `ChatBotApprovalConversationItem` surface/read-projection structure only; keep approval decision button conversion aligned with Story 12.5 unless completing it here is explicitly accepted as part of that later story's scope.
  - [x] Migrate `ChatBotConversationItemStatusSummary`, preserving facet ordering, unknown fallback facet, projection-pending live-region role/mode, once-per-stable-operation announcement deduplication, safe metadata ids, and code-valued state display.
  - [x] Migrate `ChatBotConversationItemClassificationBadge`, preserving actionable/informational icon text, kernel/version/confidence/message-code/source-evidence/redaction fields, detected intent fields, and unavailable explanation.
  - [x] Migrate `ChatBotConversationItemReviewHistory`, preserving chronological ordering by `ReviewedAtUtc`, resource/action/decision/actor/surface/correlation/operation/redaction/reason metadata, and UTC `time` elements.

- [x] Migrate stream support primitives that block Fluent governance (AC: 4)
  - [x] Replace `ChatBotActorBadge` unresolved-action raw `<button>` with `FluentButton`, preserving `aria-label`, `OnUnresolvedAction`, actor category/display label text, and non-color category distinction.
  - [x] Replace `ChatBotEvidenceChip` clickable raw `<button>` with `FluentButton` or a locally proven Fluent chip/button pattern, preserving `aria-label`, `aria-disabled`, `aria-describedby`, reason id, off-surface kind attribute, `CanOpenEvidence`, and fail-closed `ActivateAsync` behavior.
  - [x] Remove only `Components/Governed/ChatBotActorBadge.razor` and `Components/Governed/ChatBotEvidenceChip.razor` from `RawControlMigrationBacklog` after their raw controls are gone.
  - [x] Leave `Components/Governed/ChatBotApprovalConversationItem.razor` in the backlog if Story 12.5-owned raw approval buttons remain.

- [x] Keep CSS changes narrow and layout-only (AC: 1, 2, 7)
  - [x] Remove obsolete conversation-item class usage only where Fluent components now provide the primitive surface/text styling.
  - [x] Do not add new `--chatbot-type-*`, `--chatbot-radius-*`, `--chatbot-font-*`, heading type-ramp declarations, foreground `color:` roles, native control selectors, `.chatbot-button`, or legacy v4/FAST tokens.
  - [x] Keep `chatbot.tokens.css` retirement out of scope except for updating the governance backlog counts if this story legitimately shrinks existing primitive debt.

- [x] Update focused contracts, snapshots, and governance tests (AC: 4, 6)
  - [x] Update `ChatBotAccessibilityFocusContractTests` to require `FluentCard`/`FluentStack`/`FluentText` in S1 stream/item components while preserving existing accessibility contract markers.
  - [x] Update or add source-contract tests for the conversation item set so every Story 12.3 component is covered by a Fluent component presence check and a preservation check for critical `aria-*`, `data-chatbot-*`, `time`, and redaction/status markers.
  - [x] Update `ProjectWorkspaceRouteContractTests`, `ProjectWorkspaceE2ETests`, and `ProjectConversationE2ETests` only where their source/fixture expectations are affected; keep browser fallbacks for unavailable Playwright.
  - [x] Update Verify/bUnit snapshots intentionally if generated component markup changes; do not mass-accept unrelated snapshots.
  - [x] Update `ChatBotFluentConformanceTests` raw-control backlog and primitive CSS backlog counts only for debt this story actually removes.

- [x] Verify and record results (AC: all)
  - [x] Run `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false`, restoring first only if needed.
  - [x] Run `DiffEngine_Disabled=true dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --filter "Category=Governance" --no-restore -m:1 -nodeReuse:false`.
  - [x] Run the focused UI test project, or if VSTest socket creation is denied, run the compiled xUnit v3 executable from `bin/<Configuration>/net10.0`.
  - [x] Run affected E2E/source-contract tests for Project Workspace / Project Conversation, using fixture fallback if no browser binary is available.
  - [x] Run `git diff --check`.
  - [x] Add `_bmad-output/implementation-artifacts/tests/test-summary-story-12.3.md` with exact commands, pass/fail status, and any environmental limitations.

## Dev Notes

### Discovery Results

- Loaded BMAD workflow files: `.agents/skills/bmad-create-story/SKILL.md`, `discover-inputs.md`, `template.md`, and `checklist.md`.
- Loaded BMAD config from `_bmad/bmm/config.yaml`: user `Jerome`, project `chatbot`, planning artifacts under `_bmad-output/planning-artifacts`, implementation artifacts under `_bmad-output/implementation-artifacts`, document language English.
- Loaded sprint status from `_bmad-output/implementation-artifacts/sprint-status.yaml`. Story key `12-3-migrate-conversation-stream-and-items-to-fluent` was `backlog`; `epic-12` was already `in-progress`; Stories 12.1 and 12.2 were `done`.
- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`, including Epic 12, Stories 12.1-12.9, UX-DR1/UX-DR2, UX-DR4, UX-DR6, UX-DR35-UX-DR45, and Epic 3 S1 conversation semantics.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`, especially `Frontend Architecture`, `Project Structure & Boundaries`, and implementation handoff rules.
- Loaded UX source material from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md` where relevant to conversation stream semantics, accessibility, Fluent inheritance, status cues, and responsive behavior.
- Loaded PRD source material from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` selectively through source hints in epics/architecture.
- Loaded persistent project-context facts from eight sibling `**/project-context.md` files. Relevant facts: .NET 10, `.slnx`, central package management, warnings-as-errors, xUnit v3 + Shouldly, FrontComposer/Fluent v5 conformance, no generated-output edits, no casual package upgrades, and root-level submodule-only policy.
- Inspected current target implementation files under `src/Hexalith.ChatBot.UI/Components/Governed` and focused tests under `tests/Hexalith.ChatBot.UI.Tests` / `tests/Hexalith.ChatBot.UI.E2E.Tests`.

### Epic 12 Context

Epic 12 closes the component-level Fluent v5 gap left after Epic 10 adopted the FrontComposer Shell but kept interior ChatBot surfaces as raw/custom HTML over `chatbot.tokens.css`. The binding rule is that every `Hexalith.ChatBot.UI` `.razor` page/component uses FrontComposer or Fluent UI v5 components, with no raw lowercase `<button>`, `<input>`, `<select>`, or `<textarea>` outside temporary migration backlog entries. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]

Story 12.3 owns the S1 conversation stream and item rendering migration. It is not a backend story and does not change the typed project-conversation read model, CommandGateway, SignalR nudge model, CLI, or MCP behavior. Story 12.4 owns association review controls, Story 12.5 owns approval and governed-action controls, Story 12.6 owns policy/config editors, Story 12.7 owns operational/audit pages, Story 12.8 retires `chatbot.tokens.css`, and Story 12.9 re-runs cross-surface a11y/visual verification. [Source: `_bmad-output/planning-artifacts/epics.md#Stories 12.3-12.9`]

### Current Implementation State

`ChatBotConversationShell` currently renders semantic shell landmarks with raw layout containers: outer `section`, optional context `<div>`, body `<div>`, main `section role="region"`, and complementary `aside role="complementary"`. Preserve the landmark roles and unique label resolution, especially the fallback that changes the complementary label if it would equal the main label. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`]

`ChatBotConversationStream` currently renders an outer `section`, `h2`, empty `ChatBotBlockedState`, and an ordered list of item components. Preserve the ordered stream and routing by item type: participant, attachment, system decision, approval event, failure state, AI outcome, else email. Do not collapse the stream into decorative chat bubbles or anonymous messages. [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`]

The item components are metadata-heavy read-projection views. They render critical contract fields as visible text/code/time:

- `ChatBotEmailConversationItem`: actor, system-decision label, source provenance/mailbox/provider ids, association/project/conversation ids, lifecycle state, confidence, threshold band, safe-next-action, source timestamps/timezone, correlation id, evidence chips, why-this-project callback, review history.
- `ChatBotParticipantConversationItem`: internal/external/unresolved/restricted participant state, unavailable reason, participant resolution/source ids, party id when authorized, blocked reason, evidence reference/fingerprint, allowed review actions, mailbox/lifecycle/safe-next-action/correlation metadata.
- `ChatBotAttachmentConversationItem`: redacted/unavailable filename behavior, content type/size, capture/storage/scan/duplicate/retry/AI-context eligibility, file/folder ids only when authorized, redaction state, action/unavailable reason, and metadata chips.
- `ChatBotDecisionConversationItem`: association/correction decision details, actors/timestamps, confidence/threshold, evidence references, policy/surface origin, supersession chain, downstream propagation/store progress, corrected-context stale marker, owner role, redaction/retention/schema/source/correlation metadata.
- `ChatBotFailureStateConversationItem`: failure catalog headline/reason, status/category/scope/reason code, operation/task/workflow ids, retry/duplicate/dependency/escalation/reprocess/audit metadata, terminal explanation, safe next action, and client action.
- `ChatBotAiOutcomeConversationItem`: AI outcome kind/status/actor, proposal/request/context/command/approval/audit/failure metadata, risk action mapping, authorized/excluded context references, generated-summary distinction, provenance, and metadata-only explanation.
- `ChatBotApprovalConversationItem`: approval read-projection metadata plus pending decision controls. Story 12.3 may migrate the surface/read-model presentation, but Story 12.5 owns converting the pending approval buttons to FluentButton and preserving approval disabled/blocked semantics.
- `ChatBotConversationItemStatusSummary`: visible facets ordered association -> attachment -> task -> approval -> command -> failure -> retry -> next-action, unknown fallback facet, projection-pending live-region deduplication through `ChatBotAnnouncementDeduplicationState`.
- `ChatBotConversationItemClassificationBadge`: classification and detected-intent metadata, including source evidence ids, redaction state, message code, confidence, and unavailable explanation.
- `ChatBotConversationItemReviewHistory`: chronological review history ordered by `ReviewedAtUtc`, with resource/action/decision/actor/surface/correlation/operation/redaction/reason metadata.

### Fluent v5 Component Notes

The local package pin is binding: `Directory.Packages.props` pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`, and `_Imports.razor` already imports `Microsoft.FluentUI.AspNetCore.Components`. Do not add package references or change versions. [Source: `Directory.Packages.props`; `src/Hexalith.ChatBot.UI/Components/_Imports.razor`; `src/Hexalith.ChatBot.UI/Hexalith.ChatBot.UI.csproj`]

Local net10.0 XML docs for the pinned package confirm:

- `FluentCard` exposes `ChildContent`, `Appearance`, `Shadow`, `Width`, `Height`, `OnClick`, and `Role`; use it for item/card surfaces, not as a replacement for semantic lists where `ol/li/dl/dt/dd` matter. [Source: local NuGet XML `microsoft.fluentui.aspnetcore.components/5.0.0-rc.3-26138.1/lib/net10.0/Microsoft.FluentUI.AspNetCore.Components.xml`]
- `FluentStack` exposes orientation, alignment, width/height, wrap, horizontal/vertical gaps, and child content; FrontComposer project context notes prefer `FluentStack` over raw `<div>` when the wrapper exists only for layout. [Source: same local NuGet XML; `Hexalith.FrontComposer/_bmad-output/project-context.md#Blazor Shell & Fluxor Rules`]
- `FluentText` exposes `As`, `Size`, `Weight`, `Align`, `Font`, `Nowrap`, `Truncate`, `Block`, and color-related parameters; use it for titles/labels/status text rather than hand-authored type-ramp classes. Keep `<code>` for machine codes and `<time>` for timestamps where semantics matter. [Source: same local NuGet XML]

### Architecture and UX Guardrails

- UI may depend on ChatBot Client, ServiceDefaults/host defaults, and FrontComposer Shell/Contracts. It must not reference Server, Gateway internals, Dapr, EventStore internals, audit, idempotency, projection internals, CLI, or MCP. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- UX-DR1 requires component-level Fluent inheritance and build-enforced conformance; raw `<a>` nav links are allowed, but raw lowercase interactive controls are not. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- UX-DR2 bans recreating Fluent-provided primitives in hand CSS. Custom CSS should be layout-only unless Story 12.8 is explicitly retiring token debt. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`]
- UX-DR6 defines Conversation Detail as a multi-actor event stream with system decisions labelled as system decisions, never anonymous chat. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR6`]
- UX-DR35 requires projection-pending and validation/live-region behavior to be stable and not over-announced. Preserve `ChatBotAnnouncementDeduplicationState` behavior in status summary. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR35`; `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor`]
- UX-DR39/UX-DR41 require redaction-safe off-surface behavior and consistent evidence/risk/status/actor/time ordering. Do not move ids or redacted/unavailable details into hidden-only content that screen readers cannot reach. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR39`; `_bmad-output/planning-artifacts/epics.md#UX-DR41`]

### File Structure Requirements

Likely implementation locations:

- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemClassificationBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.3.md`

Avoid these locations:

- Do not edit `Hexalith.FrontComposer`, `Hexalith.EventStore`, `Hexalith.Tenants`, or other sibling submodules.
- Do not edit generated files under `obj/**/generated/HexalithFrontComposer/`.
- Do not modify package pins in `Directory.Packages.props` or add inline package versions.
- Do not move conversation rendering into a new subsystem or create backend endpoints.

### Previous Story Intelligence

Story 12.2 completed the first post-guard Fluent migration and established these patterns:

- The local Fluent package pin is authoritative; no version churn.
- Use PascalCase Fluent component tags so the case-sensitive governance regex does not false-match raw controls.
- Remove stale raw-control backlog entries after the source no longer contains raw controls.
- Keep source-contract tests raw-tag-aware rather than case-insensitive substring checks that reject names such as `FluentTextArea`.
- VSTest socket creation may fail in this sandbox; Story 12.1/12.2 documented compiled xUnit v3 executable fallback.
- Record exact commands and results in the per-story test summary.
- Do not claim this UI story caused or fixed unrelated backend/conformance failures. [Source: `_bmad-output/implementation-artifacts/12-2-migrate-governed-chat-composer-to-fluent.md`; `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]

### Git Intelligence

Recent relevant commits:

- `6fb7edc feat(story-12.2): Migrate governed chat composer to Fluent`
- `0fcdf27 feat(story-12.1): Fluent-only governance guard`
- `09aa92b fix(tests): Enhance cross-tenant leakage scans in isolation tests`
- `6266d0c chore: Update subproject commits for FrontComposer and Memories`

The relevant implementation pattern is that Epic 12 conformance is now enforced by code. Migration stories must shrink guard debt only for files they actually fix and must not weaken the guard to make large markup churn pass.

### Testing Standards

- Use xUnit v3 and Shouldly; avoid raw `Assert.*`.
- Keep `DiffEngine_Disabled=true` when running Verify-backed tests.
- Build with `.slnx`; do not create or use `.sln`.
- Prefer focused test project commands over solution-level `dotnet test`.
- Keep tests non-vacuous: assert target files exist, Fluent tags are present, and critical governed markers remain.
- If browser prerequisites are unavailable, use existing fixture fallback and record the limitation honestly.

### Regression Traps to Avoid

- Do not turn the stream into a chat transcript or hide system decisions as anonymous messages.
- Do not lose `data-chatbot-conversation-item-kind`, `data-chatbot-conversation-item-id`, `tabindex="0"`, or accessible item labels.
- Do not replace semantic `ol/li`, `dl/dt/dd`, `code`, or `time` markup with generic spans where the semantics are part of the contract.
- Do not remove or over-announce projection-pending live-region behavior in `ChatBotConversationItemStatusSummary`.
- Do not leak restricted participant, attachment, policy, audit, or AI context detail while converting markup.
- Do not replace visible unavailable reasons with tooltip-only explanations.
- Do not remove why-this-project callbacks from email/decision items or evidence chips.
- Do not migrate approval decision buttons in a way that partially breaks Story 12.5; either leave them scoped for 12.5 or complete the full approval-control migration with tests.
- Do not add new primitive CSS debt while trying to remove old classes.
- Do not run recursive submodule initialization.

### References

- [Source: `.agents/skills/bmad-create-story/SKILL.md`]
- [Source: `.agents/skills/bmad-create-story/discover-inputs.md`]
- [Source: `.agents/skills/bmad-create-story/template.md`]
- [Source: `.agents/skills/bmad-create-story/checklist.md`]
- [Source: `Hexalith.AI.Tools/hexalith-llm-instructions.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: ChatBot UI Fluent v5 Component Conformance Remediation`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.3: Migrate conversation stream + item components -> Fluent v5`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR1`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR2`]
- [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR6`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.1-3.11`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19.md`]
- [Source: `_bmad-output/implementation-artifacts/12-1-fluent-only-governance-guard.md`]
- [Source: `_bmad-output/implementation-artifacts/12-2-migrate-governed-chat-composer-to-fluent.md`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/*ConversationItem.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemClassificationBadge.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`]
- [Source: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.Tests/ProjectWorkspaceRouteContractTests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectWorkspaceE2ETests.cs`]
- [Source: `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`]
- [Source: `Directory.Packages.props`]
- [Source: `src/Hexalith.ChatBot.UI/Components/_Imports.razor`]
- [Source: local NuGet package docs for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`]
- [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]
- [Source: `Hexalith.EventStore/_bmad-output/project-context.md`]
- [Source: `Hexalith.Conversations/_bmad-output/project-context.md`]
- [Source: `Hexalith.Projects/_bmad-output/project-context.md`]
- [Source: `Hexalith.Folders/_bmad-output/project-context.md`]
- [Source: `Hexalith.Parties/_bmad-output/project-context.md`]
- [Source: `Hexalith.Tenants/_bmad-output/project-context.md`]
- [Source: `Hexalith.Memories/_bmad-output/project-context.md`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red phase: source-contract/governance tests initially failed for missing Fluent primitives and raw controls in `ChatBotActorBadge` / `ChatBotEvidenceChip`.
- VSTest limitation: `dotnet test` governance command aborts in this sandbox with `System.Net.Sockets.SocketException (13): Permission denied`; compiled xUnit v3 executable fallback was used.
- Full local executable sweep found an orphaned non-solution `Hexalith.ChatBot.Aspire.Tests` binary under `bin/obj` only; solution-member test projects passed.

### Completion Notes List

- Migrated `ChatBotConversationShell` and `ChatBotConversationStream` layout wrappers to Fluent v5 primitives while preserving shell landmarks, stream `aria-labelledby`, ordered-list semantics, empty blocked-state behavior, and metadata-only stream posture.
- Migrated all Story 12.3 conversation item surfaces and support read-projection primitives to `FluentCard` / `FluentStack` / `FluentText` composition without changing read-model contracts, data attributes, focusability, metadata fields, `code`, `time`, live-region, redaction, or review-history semantics.
- Replaced raw support primitive buttons in `ChatBotActorBadge` and `ChatBotEvidenceChip` with `FluentButton`; removed those files from `RawControlMigrationBacklog`. `ChatBotApprovalConversationItem` remains in the backlog because Story 12.5 still owns its raw approval decision buttons.
- Added source-contract coverage for every Story 12.3 stream/item/support component, updated affected E2E source expectations for Fluent tags, and recorded validation in `_bmad-output/implementation-artifacts/tests/test-summary-story-12.3.md`.

### File List

- `_bmad-output/implementation-artifacts/12-3-migrate-conversation-stream-and-items-to-fluent.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary-story-12.3.md`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemClassificationBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/GovernedOperationsVisualFoundationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotAccessibilityFocusContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotFluentConformanceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotGovernedPrimitiveContractTests.cs`

### Change Log

- 2026-06-21: Migrated conversation shell, stream, item surfaces, status/classification/review subcomponents, actor badge, and evidence chip to Fluent v5 composition for Story 12.3.
- 2026-06-21: Updated Fluent governance/source-contract/E2E expectations and recorded build/test results.
- 2026-06-21: Senior Developer Review (AI) completed — Approve. 0 Critical, 0 High, 1 Medium (commit-hygiene/transparency), 3 Low (cosmetic). Status → done.

## Senior Developer Review (AI)

**Reviewer:** Jerome (adversarial automated review) on 2026-06-21
**Outcome:** ✅ Approve — Status → `done` (0 Critical issues)

### What was verified (independently reproduced, not taken from the story's own claims)

- **Build:** `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 -nodeReuse:false` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- **UI tests (compiled xUnit v3 fallback; VSTest sockets blocked in sandbox):** `Hexalith.ChatBot.UI.Tests` → **168 total, 0 failed** (includes the 6 `Category=Governance` tests and the new accessibility-contract test).
- **E2E (real browser path executed — 20s / 212s wall-clock, not a string-only fallback):** `ProjectWorkspaceE2ETests` + `ProjectConversationE2ETests` → **35/0**; `GovernedOperationsVisualFoundationE2ETests` → **38/0**.
- **`git diff --check`** → clean.
- **AC1–AC7 cross-check:** all seven acceptance criteria implemented. Read every file in the File List against requirements.
  - AC1/AC2/AC3: All 14 components migrated to `FluentCard`/`FluentStack`/`FluentText`/`FluentBadge`/`FluentButton`. Every governed marker preserved — `<article>`/`<section>`/`<aside>` landmarks, `role="region"`/`role="complementary"`, `aria-labelledby`/`aria-label`, `data-chatbot-conversation-item-kind`/`-id`, `data-chatbot-conversation-stream="metadata-only"`, `tabindex="0"`, `ol/li` and `dl/dt/dd` semantics, `<code>` machine codes, `<time>` UTC elements, review-history `ReviewedAtUtc` ordering, redaction-state labels, and the why-this-project evidence chip (`CanOpenEvidence`/`OnActivate`).
  - AC4: `ChatBotActorBadge` and `ChatBotEvidenceChip` raw `<button>`s replaced by `FluentButton` (aria-label/aria-disabled/aria-describedby/fail-closed `ActivateAsync` preserved) and removed from `RawControlMigrationBacklog`. Only the 4 approval decision `<button>`s remain raw (lines 115–132 of `ChatBotApprovalConversationItem.razor`) — correctly retained for Story 12.5, and that file correctly stays in the backlog.
  - AC5: Status meaning still carried by text, not color; `aria-live="off"` containers and projection-pending live-region dedup markers (`data-chatbot-announcement-key`, `data-chatbot-live-announced`, `role`/`aria-live` via `LiveRegionRole`/`LiveRegionMode`) preserved in `ChatBotConversationItemStatusSummary`. Confirmed `FluentText As="TextTag.H2" Id="@TitleId"` renders the `id` (FluentComponentBase `Id` parameter → HTML `id`), so the stream `aria-labelledby` landmark association survives at runtime.
  - AC6: Governance guard is genuinely enforced — `ChatBotFluentConformanceTests` fails on stale backlog entries (a backlog file with no raw controls) and on any CSS primitive-debt drift, so the backlog edits were required, not self-serving. New `ChatBotAccessibilityFocusContractTests` case is non-vacuous: per-component Fluent-tag presence **and** preservation checks for the critical `aria-*`/`data-chatbot-*`/`time`/redaction/live-region markers.
  - AC7: Rendering-layer only — no package/version churn (`Directory.Packages.props` untouched), no `chatbot.tokens.css` edits, no backend/CLI/MCP/SignalR changes, no generated-output edits.

### Findings

- 🟡 **MEDIUM (transparency / commit hygiene — NOT a code defect):** The working tree carries uncommitted changes that are **not** in the story File List and are unrelated to Story 12.3's rendering code: submodule pointer moves `Hexalith.EventStore` (`216c70f`→`514802a`) and `Hexalith.Parties` (`698deec`→`a22985d`), plus modified `_bmad-output/implementation-artifacts/test-summary.md` and `_bmad-output/story-automator/orchestration-10-20260619-173555.md`. These are pre-existing story-automator session drift. **Not auto-reverted** (they were not created by this story; reverting could discard unrelated work — AC7 forbids submodule edits, so they must simply be excluded from this story's commit). The commit-story step must stage only the File List; the submodule guard applies.
- 🟢 **LOW (cosmetic, not fixed — fixing would add large no-value diff churn):** In the 8 `<article>`-based item components, body content nested one level deeper inside the new `FluentCard`/`FluentStack` was not re-indented (Razor-insignificant whitespace).
- 🟢 **LOW (cosmetic):** Mixed `class=` (on `FluentBadge`) vs `Class=` (other Fluent components) attribute casing; both compile and render — pre-existing `FluentBadge` pattern.
- 🟢 **LOW (over-claim):** The "Remove obsolete conversation-item class usage" subtask is marked `[x]` though no CSS classes were actually removed (layout classes are conservatively retained on the Fluent components). Safe no-op; matches the "only where Fluent now provides the primitive" wording.

### Decision

No Critical or High issues. The migration is faithful and complete, governance is enforced rather than weakened, and all validation reproduces. Story status set to `done`; sprint-status synced.
