---
baseline_commit: f1262bd1f1263467e5a4ed0ff8aeb1bc70119bf9
---

# Story 3.3: Participant rendering in the conversation stream

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want internal, external, and unresolved participants represented in the project conversation,
so that I can understand who contributed without exposing unauthorized identity detail.

## Acceptance Criteria

1. Given a participant conversation item, when it renders on S1, then the item exposes actor attribution, an actor-type label before content in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, reduced-motion behavior, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.3; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics]
2. Resolved internal participants, resolved external participants, unresolved participants, and restricted participants render with distinct localized labels and safe status metadata; unresolved or restricted details use safe identity evidence and redaction where required. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.3; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2]
3. Participant items remain metadata-only: they may expose stable `PartyId`, `SourceParticipantId`, resolution status, blocked reason, allowed review actions, evidence reference/fingerprint, source mailbox id, correlation id, redaction state, retention class, schema version, and safe display label, but must not expose raw email address evidence, provider display names, unauthorized party names, restricted party details, raw exception text, or hidden diagnostic data. [Source: src/Hexalith.ChatBot.Contracts/Commands/ResolveMailboxMessageParticipants.cs; src/Hexalith.ChatBot.Contracts/Commands/MailboxParticipantSourceReference.cs; src/Hexalith.ChatBot.Contracts/Commands/UnresolvedMailboxParticipantEvidence.cs]
4. Participant conversation items attach only to authorized project conversations that already have an associated S1 item for the same tenant and intake. A participant resolution event without a project association remains pending or non-rendered until the intake is associated; it must never be rendered from a cross-tenant or cross-project scan followed by filtering. [Source: src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionView.cs; src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs; _bmad-output/planning-artifacts/architecture.md#Data-boundaries]
5. S1 preserves Story 3.1 and Story 3.2 behavior: tenant/project partitioning, cursor pagination, safe empty/blocked/degraded states, metadata-only associated-email rendering, source-version replay safety, stale/correction safe-next-action behavior, EN/FR localization, and UI state clearing on route load/failure. [Source: _bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md#Current-State-To-Preserve]
6. Contract, server projection, conformance, UI service/state/component, localization, static CSS, and UI E2E fixture coverage prove participant rendering is safe, localized, accessible, metadata-only, replay-safe, and cross-tenant isolated. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR65-NFR70]

## Tasks / Subtasks

- [x] Extend the S1 contract spine for participant conversation items (AC: 1, 2, 3, 6)
  - [x] Add participant-safe fields to `ProjectConversationItem` and OpenAPI `ProjectConversationItem`: `participantResolutionId`, `sourceParticipantId`, `partyId`, `participantStatus`, `participantBlockedReason`, `participantDisplayKind`, `participantEvidenceReference`, `participantEvidenceFingerprint`, `participantAllowedReviewActions`, and `participantRedactionState` or equivalent additive names.
  - [x] Add stable enum wire tokens for participant item/actor/display classification. Suggested tokens: item kind `participant`, actor kinds/display classes for `internal-participant`, `external-participant`, `unresolved-participant`, and `restricted-participant`. Keep existing `email-derived` and `system-decision` tokens unchanged.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated client output.
  - [x] Add contract/OpenAPI tests proving participant fields are additive, enum wire values are stable, and raw address/body/provider payload fields are absent.
- [x] Materialize participant resolution state into the project conversation projection (AC: 2, 3, 4, 5)
  - [x] Reuse `ParticipantResolutionProjectionHandler`, `ParticipantResolutionView`, and `PublishedParticipantResolutionEvent`; do not add a second participant-resolution read model for S1.
  - [x] Inject or otherwise compose `IProjectConversationProjectionStore` with the participant projection path so participant items are created from resolved/unresolved participant events after tenant/intake association to a project is known.
  - [x] Because participant resolution events do not carry `ProjectId`, use tenant plus `IntakeId` to join to existing project conversation association items. If participant events arrive before association, store pending participant state by tenant/intake and materialize when the association item is later projected. If association arrives first, materialize when participant state arrives.
  - [x] Preserve project correction behavior: during `Correcting` or `CorrectionDelayed`, participant items must follow the same stale/blocking safe-next-action semantics as the parent intake conversation item and must not present corrected context as current.
  - [x] Keep participant source-version replacement independent from association/source-email replacement. A stale participant replay must not overwrite a newer participant item, and a newer participant event must not raise or lower the parent association item source version.
- [x] Add safe participant display classification without leaking identity (AC: 2, 3)
  - [x] Classify resolved internal/external display only from trusted Party directory metadata or explicit resolution metadata, never from email domain, provider display name, or raw address evidence. If a safe display adapter does not exist yet, add the minimal server-side adapter contract and test fake needed for this story.
  - [x] Use stable `PartyId` as the durable resolved identity reference. Safe display labels may be used only when the adapter says they are authorized and non-restricted; otherwise render a localized generic label such as "Restricted participant" or "Resolved participant".
  - [x] For unresolved/restricted states, render `ParticipantResolutionBlockedReason`, allowed review actions, evidence reference/fingerprint, and safe next action without exposing address evidence or party details.
  - [x] Do not call Parties, DAPR, EventStore, or server projection internals from the UI. Any safe display hydration belongs in server-side projection/read mapping behind an adapter.
- [x] Update UI mapping and participant rendering components (AC: 1, 2, 3, 5)
  - [x] Extend `ProjectConversationItemModel` and `ProjectConversationService.MapItem` to carry participant metadata through `IChatBotClient` only.
  - [x] Add `ChatBotParticipantConversationItem.razor` or split the existing item component cleanly so participant items do not overload `ChatBotEmailConversationItem` semantics.
  - [x] Reuse `ChatBotActorBadge`, `ChatBotEvidenceChip`, stable `<time>` elements, definition-list metadata, and existing governed layout classes. Actor labels must lead the accessible name.
  - [x] For unresolved participant actions, use the existing `ChatBotActorBadge` unresolved action affordance or an adjacent focusable "Why unavailable?" pattern. Disabled or unavailable actions must expose a reachable reason, not tooltip-only text.
  - [x] Add EN/FR resource keys for participant labels, reasons, allowed review actions, accessible names, status copy, and metadata labels. Do not hard-code user-facing strings except stable machine tokens displayed as metadata.
- [x] Maintain S1 responsive, visual, and accessibility behavior (AC: 1, 5, 6)
  - [x] Extend `chatbot.tokens.css` with existing Fluent/FrontComposer tokens only; do not introduce raw `#`, `rgb(`, or `hsl(` color literals.
  - [x] Ensure phone/tablet layouts wrap participant metadata without overlap, preserve actor/status/evidence/timestamp order, and keep actions at reachable touch and keyboard targets.
  - [x] Ensure forced-colors and reduced-motion rules cover participant items, actor badges, evidence chips, focus outlines, and unavailable-action explanations.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for DTO serialization, OpenAPI schema, generated client availability, participant enum wire tokens, and absence of raw identity/body/provider fields.
  - [x] Server projection tests for participant-before-association, association-before-participant, resolved internal, resolved external, unresolved, restricted/degraded, duplicate/stale replay, correction stale state, and tenant/project partitioning.
  - [x] Conformance/read-surface tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant contexts collapse to safe denial with metadata-only bodies and no participant evidence leakage.
  - [x] UI service/state/component tests for mapped participant metadata, actor-label accessible names, evidence/status/timestamp order, localization keys, no stale prior project content, and no raw colors.
  - [x] Update existing Playwright/UI.E2E fixture coverage for populated S1 stream to include participant items in internal, external, unresolved, and restricted states with forced-colors and reduced-motion assertions where the harness supports them.

## Dev Notes

### Scope Boundaries

- This story is participant rendering on the existing S1 project conversation stream. It must not implement the unresolved-participant queue or management workflow beyond rendering safe review affordance metadata; queue management belongs to later operational/admin stories.
- Do not change participant resolution command semantics unless an additive event/contract field is strictly required for rendering. The durable source of resolution remains `ResolveMailboxMessageParticipants`, `MailboxParticipantResolved`, and `MailboxParticipantUnresolved`.
- Do not implement attachment rendering/status (Stories 3.4, 3.12, 3.13), association/correction decision rendering (Story 3.5), approval events (Story 3.6), failures/retries (Story 3.7), AI outcomes (Story 3.8), the "why this project" panel (Story 3.9), classification/review history (Story 3.11), or scoped AI context packaging (Story 3.14).
- Do not add a chat composer or message-send workflow. Architecture defines S1 as a read projection; future writes go through the CommandGateway, not a separate chat subsystem.

### Existing Code To Reuse

- S1 contract and read surface: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `ProjectConversationResponse.cs`, `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, and generated client output under `src/Hexalith.ChatBot.Client/Generated/`.
- S1 projection store and item model: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `IProjectConversationProjectionStore.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs`, and `ProjectConversationPage.cs`.
- Participant projection path: `src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionProjectionHandler.cs`, `ParticipantResolutionProjectionTranslator.cs`, `ParticipantResolutionView.cs`, `PublishedParticipantResolutionEvent.cs`, and `InMemoryParticipantResolutionProjectionStore.cs`.
- Participant command/event contracts: `ResolveMailboxMessageParticipants`, `MailboxParticipantSourceReference`, `ResolvedMailboxParticipantReference`, `UnresolvedMailboxParticipantEvidence`, `MailboxParticipantResolved`, `MailboxParticipantUnresolved`, `MailboxParticipantRejected`, and `MailboxParticipantQuarantined`.
- UI route/state/service: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- Governed UI primitives: `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, localization resources, and `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Existing tests to extend: `ProjectConversationContractTests`, `ParticipantResolutionContractTests`, `ProjectConversationProjectionTests`, `ParticipantResolutionProjectionTests`, `ProjectConversationServiceTests`, `AssociationReviewComponentContractTests`, `CrossTenantReadSurfaceIsolationTests`, and `ProjectConversationE2ETests`.

### Current State To Preserve

- Story 3.1 added `GET /api/v1/projects/{projectId}/conversation`, tenant/project keyed S1 projection storage, cursor pagination, authorized empty reads, safe denial for unauthorized reads, governed UI route/state, and reducers that clear previous project state on load/failure.
- Story 3.2 enriched the S1 item with source-email metadata and fixed replay/version safety. Do not regress: source-email enrichment has its own source version and must not overwrite newer association/correction state.
- `ProjectConversationItemView.ShouldReplace` currently guards association item replacement by source version; both in-memory and DAPR stores depend on this rule.
- `ParticipantResolutionProjectionHandler` currently ignores duplicate/stale notifications by source version and writes metadata-only `ParticipantResolutionView` records with tenant, intake, source participant id, stable `PartyId`, reason, evidence reference/fingerprint, redaction, retention, schema version, correlation id, and timestamps.
- `ProjectConversationItemView` currently has only `EmailDerived` and `SystemDecision` kinds and only `Mailbox` and `SystemDecision` actor kinds. Additive enum changes must preserve old wire tokens and generated-client compatibility.
- Existing worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. UI reads through `IChatBotClient` only; server projection internals, DAPR, EventStore, and Parties adapters stay server-side. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- Store stable IDs such as `ProjectId`, `PartyId`, `ConversationId`, and source participant ids. Never store or render upstream personal data as durable conversation content. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns; Hexalith.Conversations/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Tenant authority comes from authenticated claims/context and projection gates, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe tenant context must collapse to safe denial without confirming restricted resource existence. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR1-NFR12]
- DAPR pub/sub is at-least-once and unordered. Projection handlers must tolerate duplicate and out-of-order participant, intake, and association events. SignalR nudges, if used, trigger re-query and are never trusted as data. [Source: _bmad-output/planning-artifacts/architecture.md#Communication-Patterns; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Cursor tokens stay opaque and tenant/project scoped. Do not embed tenant, project, mailbox, source participant, Party, evidence, address, or provider payload text in cursor values.
- Use `System.Text.Json` shared options and camelCase wire names. Do not add inline `JsonSerializerOptions` or new serialization libraries.

### UX And Accessibility Guardrails

- The UX package is binding despite having no mockups. The conversation stream orders human, external party, mailbox, AI, CLI/MCP, background, trigger, and system events with actor attribution; actor-type label precedes content in accessible names. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Components]
- Actor badges distinguish category by accessible label and icon affordance, not color alone. Unresolved actors show unresolved state and safe actions. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Component-Patterns]
- Evidence, risk, status, actor, and timestamp must appear in a consistent order. Plain-language summaries precede raw IDs; IDs remain available as metadata.
- Conversation stream focus remains stable: Tab reaches message/event groups and actions, and reduced motion suppresses non-essential item movement.
- EN/FR localization is required. Stable machine codes, IDs, status codes, reason codes, and correlation ids remain untranslated; labels and explanations are translated. Avoid concatenated strings for accessible names.

### Project Structure Notes

- Contract DTO and enum changes belong under `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Enums/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server projection changes belong under `src/Hexalith.ChatBot.Server/Projections/`; participant resolution command/event behavior remains under the existing gateway/association participant areas.
- If a safe participant display adapter is needed, put it under `src/Hexalith.ChatBot.Server/Adapters/Parties/` and keep it server-side.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.2 review fixed four regressions that matter here: participant enrichment must not let stale source records update existing items, must not conflate enrichment source version with association source version, must not omit required threshold/status metadata, and must not echo unknown provenance or raw provider context.
- Story 3.1 review fixed cross-project stale UI state and authorized empty reads. Participant rendering must preserve state clearing and must not convert an unknown or foreign project into an empty authorized participant view.
- Epic 2 retrospective and architecture both warn not to invent a separate conversation model. Build on the existing contract spine and S1 projection; do not introduce a transcript table or participant-specific UI data plane.
- Prior validation used compiled xUnit v3 executables because VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - targeted contract/client/server/UI/conformance tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Add regression tests around event ordering: participant before association, association before participant, source-email before participant, participant before source-email, stale participant replay after current participant, and project correction while participant items exist.
- Include negative content assertions for raw email address evidence, provider display name evidence, source context, raw exception text, unauthorized party names, and restricted party details in API, UI, fixture, logs/test output where applicable.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, DAPR 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Decisions-Provided-by-the-Platform-Starter]
- NuGet currently lists `Microsoft.FluentUI.AspNetCore.Components` 5.0.0-rc.3 as a newer prerelease than the architecture pin, but this story must keep the repo-pinned Fluent/FrontComposer stack unless a separate dependency-upgrade story is created. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]
- Microsoft Learn tracks .NET 10 breaking changes separately; do not mix framework migration work into participant rendering. [Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.3 requirements.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR13, FR14, FR22, FR68, FR76, FR77, NFR1-NFR12, NFR60-NFR70.
- `_bmad-output/planning-artifacts/architecture.md` - projection boundaries, DAPR/event ordering, format patterns, file organization, and testing standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - component patterns, state patterns, conversation semantics, accessibility floor, localization, responsive behavior.
- `Hexalith.Conversations/_bmad-output/project-context.md` - conversation/participant read-time hydration, stable ID, no transcript table, and fail-closed rules.
- `Hexalith.Parties/_bmad-output/project-context.md` - Parties privacy and stable contract boundaries.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` - S1 shell/read surface implementation context and review fixes.
- `_bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md` - associated-email enrichment context and review fixes.
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-01.md` - Epic 3 preview and source-evidence reuse warning.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01T02:59:45+02:00 - Moved story and sprint status to in-progress; preserved existing `baseline_commit`.
- 2026-06-01T03:14:28+02:00 - `dotnet test` was blocked by VSTest socket creation (`SocketException (13): Permission denied`); used compiled xUnit v3 runners per story guidance.
- 2026-06-01T03:14:28+02:00 - Validation passed: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
- 2026-06-01T03:14:28+02:00 - Targeted compiled xUnit runners passed for contracts, client generation, server projections, UI, conformance, and ProjectConversation E2E fixture coverage.
- 2026-06-01T03:14:28+02:00 - Full compiled test executable sweep passed with exit code 0.
- 2026-06-01T03:33:37+02:00 - Senior developer review found and auto-fixed participant localization/label fallback issues; validation passed with build, targeted runners, conformance, ProjectConversation E2E, and full compiled executable sweep.

### Completion Notes List

- Extended S1 contract/OpenAPI/generated client with additive participant item metadata and stable `participant`, `internal-participant`, `external-participant`, `unresolved-participant`, and `restricted-participant` wire tokens.
- Composed participant resolution projection into the existing project conversation store so participant events remain pending until tenant/intake association is known, materialize without cross-project scans, and keep participant source-version replacement independent from association/source-email replacement.
- Added server-side safe participant display adapter boundary with restricted fallback; UI still reads only through `IChatBotClient`.
- Added dedicated participant conversation rendering with localized EN/FR labels, accessible names, evidence/status/actor/timestamp ordering, reachable unavailable reasons, forced-colors and reduced-motion coverage, and metadata-only display.
- Expanded contract/client/server/UI/conformance/E2E coverage for participant fields, materialization order, stale replay, correction safe-next-action behavior, localization, raw payload absence, and populated internal/external/unresolved/restricted fixture states.

### Senior Developer Review (AI)

**Reviewer:** Codex GPT-5
**Date:** 2026-06-01
**Outcome:** Approved after auto-fixes; no critical issues remain.

#### Findings Fixed

- **HIGH:** Participant UI rendered status, blocked reason, and allowed review actions from raw enum names instead of localized EN/FR participant copy, so AC2 and AC6 localization/status requirements were only partially met. Fixed `ChatBotParticipantConversationItem.razor`, `ChatBotUiTextLocalizer.cs`, `ChatBotUiTextKey.cs`, EN/FR resources, and E2E fixture expectations.
- **HIGH:** `ChatBotActorBadge` replaced unresolved/restricted participant display labels with generic `Unresolved actor`, so the visible actor badge did not preserve distinct unresolved vs restricted participant labels. Fixed badge label fallback to use provided safe labels while retaining unresolved action behavior.
- **MEDIUM:** Server-side participant display projection baked English fallback labels into resolved participant records when the safe display directory returned no authorized label. Fixed `ParticipantResolutionProjectionHandler` to emit no fallback label and let the localized UI choose generic participant copy.

#### Validation

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests` - passed 3/3.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests` - passed 14/14 targeted before full sweep; full sweep passed 15/15.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests -class Hexalith.ChatBot.Server.Tests.Projections.ParticipantResolutionProjectionTests` - passed 20/20.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests -class Hexalith.ChatBot.UI.Tests.AssociationReviewComponentContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests` - passed 16/16.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none` - passed 56/56.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 6/6.
- Full compiled test executable sweep under `tests/*/bin/Debug/net10.0/` - passed with exit code 0.

#### Re-Review — 2026-06-10 (Story Automator adversarial pass)

**Reviewer:** Claude (Opus 4.8) on 2026-06-10
**Outcome:** Approved — no critical/high/medium issues. Status remains `done`.

Validated every Acceptance Criterion and every `[x]` task against the actual story-3.3 commit (`0bbf839`, parent `f1262bd` = `baseline_commit`), not the current HEAD (HEAD is 197 commits past 3.3; later stories 3.11/6.5 expanded several of these files, so review was scoped to the 3.3 diff to avoid conflating out-of-scope work).

- **AC1** ✓ Accessible name leads with the actor-type label (`{DisplayKind}: {DisplayLabel}, {LifecycleState}`); header order evidence → status → actor → timestamp; status is localized text + non-color evidence chips; reduced-motion/forced-colors handled in `chatbot.tokens.css`.
- **AC2** ✓ Four distinct localized display kinds (internal/external/unresolved/restricted) with safe status metadata and redaction state.
- **AC3** ✓ Metadata-only additive DTO fields (all `= null` defaults); `ParticipantResolutionView`/handler persist only stable IDs (`PartyId`, `SourceParticipantId`), safe evidence reference/fingerprint, and an authorized safe label — no raw address/provider/body/exception fields. Restricted items hide `PartyId` (verified in component gate and E2E).
- **AC4** ✓ Participant↔association join is keyed by `tenant:project-conversation:{intake}` in both `InMemory` and `Dapr` stores — no cross-tenant/cross-project scan-then-filter; pending-until-association works in both arrival orders; `ProjectId` is inherited from the association.
- **AC5** ✓ Participants skip source-email enrichment (`Kind != Participant` guards in both stores and `WithSourceEmail`); participant materialization creates a separate item id and never raises/lowers the parent association `SourceVersion`; stale replay guarded by `SourceVersion >= existing` + value-equality re-materialization check.
- **AC6** ✓ Coverage green across all layers.

Handler is correctly fail-closed: non-`Resolved` or blank `PartyId` → `UnresolvedParticipant`; any directory result other than internal/external collapses to `RestrictedParticipant` with no label (prior review's no-fallback-label fix confirmed intact).

**Validation (this pass, compiled xUnit v3 runners — `dotnet test` blocked by sandbox socket perms):**

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — passed, exit 0.
- Contracts `ProjectConversationContractTests` — 6/6; Client `ClientGenerationTests` — 19/19.
- Server `ProjectConversationProjectionTests` + `ParticipantResolutionProjectionTests` — 63/63.
- UI `ProjectConversationServiceTests` + `ChatBotLocalizationContractTests` (+`AssociationReviewComponentContractTests`) — 20/20.
- Conformance (full, incl. `CrossTenantReadSurfaceIsolationTests`) — 87/87.
- E2E `ProjectConversationE2ETests` — 22/22 (includes the working-tree gap-coverage additions below).

**Auto-fix applied this cycle:** The in-flight working-tree gap-coverage in `ProjectConversationE2ETests.cs` — full ordered-metadata assertions for **external** and **restricted** participant items (previously only internal/unresolved had them) — was validated and retained; it closes an AC2/AC6 coverage gap and is green. No source defects required fixing.

**Non-blocking observations (LOW, not fixed — out-of-scope on current HEAD / pre-existing pattern):**

- The durable intake-index key was renamed `…:project-conversation-source-email:{intake}:items` → `…:project-conversation:{intake}:items` in both stores. Self-consistent and the new name is more accurate, but on a live upgrade it orphans pre-3.3 persisted intake indexes (projection rebuild mitigates in this greenfield).
- The UI keys participant-kind logic on `enum.ToString()` Pascal names string-matched against hard-coded literals in the component — rename-fragile (no compile-time safety), but a pre-existing 3.1/3.2 pattern covered by E2E.
- AC1's "evidence/risk/status/actor/timestamp" order omits *risk* for participants (no threshold/risk concept applies) — defensible.

### File List

- `_bmad-output/implementation-artifacts/3-3-participant-rendering-in-the-conversation-stream.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationActorKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationParticipantDisplayKind.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Adapters/Parties/IParticipantDisplayDirectory.cs`
- `src/Hexalith.ChatBot.Server/Adapters/Parties/UnavailableParticipantDisplayDirectory.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionNotification.cs`
- `src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/ParticipantResolutionView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedParticipantResolutionEvent.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ParticipantResolutionProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Change Log

- 2026-06-01 - Implemented Story 3.3 participant rendering in S1, including additive contract fields, generated client refresh, server projection materialization, safe display adapter boundary, UI rendering/localization/CSS, and validation coverage.
- 2026-06-01 - Senior developer review auto-fixed participant localized status/reason/action rendering, unresolved/restricted badge labels, and server safe-display fallback behavior; marked story done after validation.
- 2026-06-10 - Story Automator adversarial re-review against commit `0bbf839`: all 6 ACs and all tasks verified, 217 targeted tests green (build + contracts/client/server/UI/conformance/E2E); validated and retained working-tree E2E gap coverage for external/restricted participant metadata; no critical/high/medium issues; status remains done.
