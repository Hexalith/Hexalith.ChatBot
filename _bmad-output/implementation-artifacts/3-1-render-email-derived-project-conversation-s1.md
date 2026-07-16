---
baseline_commit: cebdda21c836676ad95a8eebf1cfed196c6b1a22
---

# Story 3.1: Render email-derived project conversation (S1)

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want associated email rendered as a project conversation kept separate per tenant and project,
so that I work from project context without opening my mailbox.

## Acceptance Criteria

1. Given an associated message, when I open the Project Workspace / Conversation Detail (S1), then email-derived messages render as ordered project conversation context within a `ChatBotConversationShell` that keeps project context and workflow state visible. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.1]
2. Given multiple tenants and projects, when conversations render, then conversation context is strictly separated by tenant and project; no cross-tenant content appears in item content, pagination cursors, route parameters, logs, telemetry, or error bodies. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.1; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR11]
3. System decisions render as labelled system decisions in the stream, not as anonymous chat messages. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.1; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Components]
4. The S1 page shows authorized project identity, tenant context when relevant, current conversation state, and a safe status in the persistent project context header. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Components]
5. Email-derived items are ordered by server-side UTC source/projection time and expose stable metadata only: project id/display name when authorized, association id, source mailbox id, source conversation/thread id, lifecycle state, threshold band, confidence score, correlation id, source provenance, schema version, and safe next action when present. Do not invent raw email body storage or render provider payload content in this story. [Source: src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs; src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs]
6. Empty, loading, stale, degraded, unauthorized, and blocked states render with existing governed primitives, non-color status text, keyboard-reachable explanations, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility]
7. Conversation reads meet the default user-facing p95 target of 2 seconds under the MVP operating baseline and use cursor pagination, not offset pagination. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR24; _bmad-output/planning-artifacts/architecture.md#Format Patterns]

## Tasks / Subtasks

- [x] Define the S1 read contract and OpenAPI surface (AC: 1, 2, 5, 7)
  - [x] Add contract DTOs under `src/Hexalith.ChatBot.Contracts/Queries/` for project conversation response, item, actor/system-decision marker, status, and cursor page.
  - [x] Add a metadata-only query endpoint to `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, then regenerate the typed client; do not hand-edit generated client files.
  - [x] Use `System.Text.Json` shared enum/string behavior and stable wire tokens; add direct and nested serialization assertions for any new enum.
- [x] Build the tenant-partitioned conversation projection/read store (AC: 1, 2, 3, 5, 7)
  - [x] Project from existing association/intake/decision/correction events already exposed through `PublishedAssociationEvent` and `AssociationCandidateView`; add only the missing read-model shape needed for S1.
  - [x] Store/read keys must include tenant and project before conversation item identity, for example `{tenant}:project-conversation:{projectId}:{itemId}`; never query a cross-tenant bucket and filter later.
  - [x] Projection handlers must be idempotent and order-tolerant by source version; duplicate or older events must not duplicate or reorder stream items.
  - [x] During `Correcting` or `Correction-delayed`, show stale/blocking state and safe next action instead of presenting corrected context as current.
- [x] Add the S1 UI service, Fluxor state, and route (AC: 1, 2, 4, 6, 7)
  - [x] Add a UI-owned service that reads through `IChatBotClient` only; the UI must not reference server projections, DAPR, stores, or EventStore internals.
  - [x] Add route `/projects/{ProjectId}/conversation` or the nearest existing routing convention, with guarded empty/loading/error states.
  - [x] Reuse `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotStatusBanner`, `ChatBotBlockedState`, `ChatBotActorBadge`, and `ChatBotEvidenceChip` where applicable.
- [x] Add conversation stream primitives and styling (AC: 1, 3, 4, 6)
  - [x] Add scoped components under `src/Hexalith.ChatBot.UI/Components/Governed/` for a conversation stream and a metadata-only email-derived item.
  - [x] Ensure system decisions have explicit labels and actor/category affordances; do not show system/worker events as human chat bubbles.
  - [x] Extend `chatbot.tokens.css` using existing Fluent/FrontComposer tokens only; include responsive, forced-colors, reduced-motion, and no raw color literals.
- [x] Localize user-facing text (AC: 3, 4, 6)
  - [x] Add EN and FR resources in `SharedResource.resx` and `SharedResource.fr.resx`.
  - [x] Keep status and denial copy user-safe; no raw exception text, provider payloads, unauthorized project/file/party names, or source email content.
- [x] Add focused tests and validation evidence (AC: all)
  - [x] Contract tests for new DTOs, OpenAPI operation, enum wire tokens, and generated client availability.
  - [x] Server projection/store tests for ordering, duplicate delivery, older source-version ignore, correction stale/blocking state, and tenant/project key partitioning.
  - [x] Conformance/isolation tests extending `CrossTenantReadSurfaceIsolationTests` for foreign, unknown, malformed, missing-tenant, ambiguous-tenant, stale-tenant, and unsafe-tenant conversation reads.
  - [x] UI tests mirroring existing component-contract style: page uses governed primitives, accessible labels include actor/system decision type, responsive/forced-colors/reduced-motion CSS exists, and no raw colors are introduced.
  - [x] If a browser endpoint is available, add or update Playwright coverage for S1 loading, populated stream, empty state, and unauthorized/redacted state.

## Dev Notes

### Scope Boundaries

- This story creates the S1 project conversation read surface and metadata-only email-derived item rendering. It must not implement detailed participant rendering, attachment rows, association evidence drawer, approval events, failure/retry item taxonomy, AI outcomes, or scoped AI context packaging; those are Stories 3.2 through 3.14.
- Do not add a fake chat/composer workflow. Architecture says the M0 conversation view is a read projection a future chat surface can write into via the same CommandGateway, not a separate chat subsystem. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- Do not invent raw email content storage. Current intake contracts explicitly capture metadata/source identity and attachment references only; body content is out of scope in existing `CaptureMailboxMessageIntake`. If an implementation needs source body content, stop and split a prerequisite contract/story rather than leaking provider payloads. [Source: src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs]

### Existing Code To Reuse

- UI shell and governed primitives: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor`, `ChatBotProjectContextHeader.razor`, `ChatBotActorBadge.razor`, `ChatBotStatusBanner.razor`, `ChatBotBlockedState.razor`, `ChatBotEvidenceChip.razor`.
- Existing S2 page/state patterns: `src/Hexalith.ChatBot.UI/Components/Pages/AssociationReview.razor`, `src/Hexalith.ChatBot.UI/State/AssociationReview/*`, and `src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs`.
- Existing projection patterns: `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionEndpoints.cs`, `AssociationProjectionTranslator.cs`, `AssociationProjectionHandler.cs`, `AssociationCandidateView.cs`, `DaprAssociationProjectionStore.cs`, and `InMemoryAssociationProjectionStore.cs`.
- Existing isolation harness: `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs` and leakage scanner harness files.

### Current State To Preserve

- Association projection is already DAPR-backed through `chatbot-statestore`, tenant-keyed via `AssociationCandidateView.KeyFor`, idempotent by source version, and merge-aware for correction propagation progress. Preserve those properties when adding conversation projection state.
- S2 UI already relies on governed primitives, localized text, focus-reachable disabled reasons, reduced-motion and forced-colors CSS, and metadata-only service mapping. S1 should follow that shape rather than creating a separate UI language.
- The worktree currently has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert it.

### Architecture Guardrails

- Read APIs must enforce tenant from authenticated claims, not route/body values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe tenant context must collapse to indistinguishable safe denial with metadata-only body.
- Query results must use derived-record fields: `tenantId`, `sourceProvenance`, `derivationKernelVersion`, `redactionState`, `retentionClass`, `schemaVersion`, `sourceVersion`, and `correlationId`.
- Use ULIDs for new item/cursor identifiers where new identifiers are required; do not use GUIDs.
- All projection handlers must tolerate at-least-once, unordered DAPR pub/sub delivery and must re-query on SignalR nudges if a nudge is added; do not trust nudge payloads as data.
- Keep dependency direction: `Contracts <- Client <- UI/Server`. UI reads through `IChatBotClient`; server internals stay internal.
- Cursor tokens must be tenant/project scoped, tamper-resistant or opaque, and redaction safe. They must not embed unauthorized content or raw source payload.

### Project Structure Notes

- Contract DTOs belong under `src/Hexalith.ChatBot.Contracts/Queries/`; OpenAPI remains the single contract source under `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Server read models, translators, projection handlers, stores, and endpoints belong under `src/Hexalith.ChatBot.Server/Projections/`; do not add broad type-bucket folders.
- UI page, service, Fluxor state, components, localization, and CSS should mirror the existing S2 organization under `src/Hexalith.ChatBot.UI/Components/Pages/`, `Components/Governed/`, `Services/`, `State/`, `Localization/`, and `wwwroot/css/`.
- Tests should mirror source boundaries: contract tests in `tests/Hexalith.ChatBot.Contracts.Tests/`, projection/server tests in `tests/Hexalith.ChatBot.Server.Tests/Projections/`, isolation tests in `tests/Hexalith.ChatBot.Conformance.Tests/`, and UI contract tests in `tests/Hexalith.ChatBot.UI.Tests/`.
- Detected variance: architecture planning names `Adapters/Conversations/`, but current code does not yet expose a Conversations adapter for S1. Prefer a ChatBot-owned read projection from existing association events for this story; do not introduce sibling write behavior unless implementation proves a read-only Conversations adapter is already available.

### UX And Accessibility Guardrails

- Use the UX package as binding context even though it contains no mockups. The conversation shell keeps project context and workflow state visible; conversation stream orders human, external party, mailbox, AI, CLI/MCP, background, trigger, and system events with actor attribution.
- Actor badges distinguish category by accessible label and icon affordance, not color alone. For this story, mailbox/system decision categories are enough for the S1 shell; richer participant rendering belongs to Story 3.3.
- Empty project conversation should show project context and a simple start/setup/status affordance when relevant. Unauthorized and degraded states must show safe next action without confirming restricted resource existence.
- Phone/tablet layouts must preserve read-only summary, status, safe actions, and screen-reader-equivalent recovery guidance; dense editing/admin workflows can defer to larger-screen fallback patterns.

### Testing Notes

- Prefer focused compiled xUnit v3 test executables if default VSTest sockets are blocked in this sandbox; this was the reliable local validation path in prior stories.
- Use Shouldly/NSubstitute/bUnit patterns already present. Do not add new assertion or mocking libraries.
- Run at least:
  - `dotnet build Hexalith.ChatBot.slnx`
  - targeted contract, server projection, UI, and conformance tests changed by this story
  - UI static/component tests for CSS and governed primitive contracts
- Add Playwright only if the existing E2E harness can run with an available endpoint; otherwise document the limitation in the dev record.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC through FrontComposer, DAPR 1.17.x, and Aspire 13.3.x. Do not upgrade packages as part of this story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies]
- Current Microsoft Aspire docs describe AppHost as the code-first application model and confirm Aspire startup handles service discovery, dependency resolution, configuration injection, and health monitoring; keep AppHost changes in that model if topology work becomes necessary. [Source: https://learn.microsoft.com/dotnet/aspire/fundamentals/app-host-overview]
- Microsoft docs for Aspire 13.3 note manual package upgrades and configuration behavior; this story should not rely on automatic Aspire version changes. [Source: https://learn.microsoft.com/dotnet/aspire/app-host/configuration]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3, Story 3.1 and cross-cutting planning guidance.
- `_bmad-output/planning-artifacts/architecture.md` - technical constraints, frontend architecture, projection/query structure, format patterns, testing standards.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - NFR1-NFR17, NFR24, NFR32, NFR34, NFR60-NFR64.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - IA/components/states/accessibility/responsive requirements.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - component semantics and Fluent/FrontComposer token rules.
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-01.md` - Epic 3 preview and carry-forward warning not to invent a separate conversation model.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` - passed.
- `dotnet test` was attempted for targeted projects, but the VSTest host cannot open its local socket in this sandbox (`SocketException (13): Permission denied`); validation used compiled xUnit v3 executables instead.
- Direct xUnit executables passed before review fixes: Contracts 81/81, Client 15/15, Server 232/232, UI 90/90, Workers 15/15, Conformance 56/56.
- Review validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1` - passed.
- Review validation: compiled xUnit executables passed: Server 234/234, UI 91/91, Contracts 84/84, Conformance 56/56, Client 15/15, UI.E2E 34/34.

### Completion Notes List

- Added the metadata-only S1 project conversation read contract, OpenAPI operation, generated client facade, and enum serialization coverage.
- Added a tenant/project-partitioned conversation projection store with in-memory and DAPR-backed implementations, cursor pagination, idempotent source-version handling, stale/correction state mapping, and safe denial for invalid or unauthorized reads.
- Added the Blazor project conversation route, UI-owned service, Fluxor state, governed stream/item components, localized EN/FR copy, responsive/forced-colors/reduced-motion CSS, and metadata-only rendering.
- Added contract, client, server projection, UI service/component contract, worker fake, and cross-tenant conformance coverage. Playwright was not added because no browser endpoint was available in this sandbox run.
- Definition of Done validation against `.agents/skills/bmad-dev-story/checklist.md`: PASS.

### File List

- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Client/IChatBotClient.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationActorKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationReadStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationCursorPage.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationResponse.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationCursor.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationPage.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`
- `src/Hexalith.ChatBot.UI/Components/_Imports.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Program.cs`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationActions.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationFeature.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationReducers.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationState.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationHttpHost.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/M365MailboxEventActorIsolationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewEffectsTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewServiceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/GovernedOperationServiceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/GovernedOperationsEffectsTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationStateTests.cs`
- `tests/Hexalith.ChatBot.Workers.Tests/Mailbox/GraphMailboxIntakeWorkerTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Senior Developer Review (AI)

**Outcome:** Approved after automatic fixes. No critical issues remain.

**Findings fixed:**

- **High:** `DaprProjectConversationProjectionStore.UpsertAsync` could overwrite a newer projected conversation item with an older replay, violating the idempotent/source-version task. Fixed by sharing replacement logic with the in-memory store and rejecting older DAPR writes.
- **High:** `ProjectConversationReducers` preserved a previous project's conversation during a new load or failed read, allowing stale cross-project content to remain visible under a different route. Fixed load/failure reducers to clear prior conversation state.
- **High:** The S1 endpoint could not render a legitimate empty project conversation; it always collapsed an empty page to safe denial. Fixed authorized-empty reads by requiring an explicit project-owner claim for empty `200` responses while preserving safe denial for unknown/foreign empty reads.
- **Low:** `ChatBotEmailConversationItem` did not expose the stable item id in the rendered item metadata attributes used by the E2E fixture pattern. Added `data-chatbot-conversation-item-id`.

**Review checklist evidence:**

- Story status was `review`; epic/story resolved as 3.1.
- Story context was embedded in the story file; architecture, UX, PRD, and epic references were used from the story's local planning links.
- Tech stack confirmed from repo build files and story notes: .NET 10, Blazor, Fluxor, Fluent/FrontComposer, DAPR-backed stores, xUnit v3, central package management.
- MCP/web doc search was not needed for this code review; all reviewed behavior was governed by local story, architecture, and implementation artifacts.
- Acceptance criteria, completed tasks, file list, tests, code quality, security/isolation, cursor behavior, UI state handling, and empty/unauthorized behavior were cross-checked against implementation and tests.

#### Re-review 2026-06-10 (story-automator, auto-fix)

**Outcome:** Approved after one automatic fix. No critical issues remain. Status stays `done`.

**Finding fixed:**

- **Medium (AC4 / correction safety):** `BuildProjectConversationResponse` derived the header `status`, `conversationState`, and `safeNextAction` from only the requested page's items. Because items are ordered oldest-first and the S1 UI loads only the first page, a newer `Correcting` / `CorrectionDelayed` / `Failed` item beyond page 1 would be hidden and corrected context could be presented as current — violating AC4 ("current conversation state ... in the persistent project context header") and the correction-safety subtask. Fixed by computing the conversation-current item across the whole project item set (new `ProjectConversationItemView.LatestOf`, surfaced via the new optional `ProjectConversationPage.LatestItem`) in both `InMemoryProjectConversationProjectionStore` and `DaprProjectConversationProjectionStore` at zero extra I/O, and consuming it in `BuildProjectConversationResponse`. Added regression test `ReadPageShouldExposeConversationLatestItemBeyondTheRequestedPage`.

**Verified clean (no change required):**

- Tenant/project isolation is enforced from authenticated claims (not route/body); cursor is HMAC-signed, tenant/project-scoped, and opaque; both stores ignore older source versions idempotently; UI reducers clear prior-project conversation on load/failure; OpenAPI `pageSize` (min 1 / max 100 / default 25) matches the server clamp; item mapping is metadata-only; the QA-added cursor pass-through test is valid.

**Validation:** `dotnet build Hexalith.ChatBot.slnx` clean (0 warnings, warnings-as-errors). Compiled xUnit v3 executables (VSTest sockets remain sandbox-blocked): Server 1526/1526, Conformance 87/87, Contracts 480/480, UI 131/131, Client 34/34, UI.E2E 75/75, Workers 30/30.

### Change Log

- 2026-06-01 - Implemented Story 3.1 S1 project conversation read surface, metadata-only rendering, tenant/project isolation, generated client support, localized UI states, and focused validation coverage.
- 2026-06-01 - Senior developer review fixed DAPR stale replay handling, cross-project stale UI state, authorized empty conversation reads, item-id render metadata, and added focused regression coverage.
- 2026-06-10 - Story-automator re-review fixed page-scoped conversation header state so the persistent project context header reflects the conversation-current item across the whole conversation (AC4 / correction safety), shared via `ProjectConversationItemView.LatestOf` and `ProjectConversationPage.LatestItem`; added regression coverage. Full targeted suites green.
