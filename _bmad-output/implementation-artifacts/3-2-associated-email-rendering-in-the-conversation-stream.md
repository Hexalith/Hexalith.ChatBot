---
baseline_commit: 8bbfa98997703b3f18c4f1f2668c60ccf9cc6483
---

# Story 3.2: Associated-email rendering in the conversation stream

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want associated email represented in the project conversation,
so that the original project-relevant message is visible without leaving the workspace.

## Acceptance Criteria

1. Given an associated email conversation item, when it renders on S1, then the item exposes actor attribution, an actor-type label before content in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, reduced-motion behavior, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.2; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics]
2. Source email identity and message context are distinguishable from AI interpretation and system decisions: mailbox/source message identity, internet message id when present, conversation/thread id, received/sent/created timestamps, safe source-provenance token, association id, lifecycle state, confidence/threshold band, status, and correlation id render as metadata-only source evidence, not as AI text or anonymous chat. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.2; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR2]
3. The email item remains metadata-only: it must not add or render raw provider payloads, email body HTML/text, unauthorized project names, tenant names, sender/recipient PII beyond already authorized/source-safe labels, attachment content, raw exception text, or hidden diagnostic data. [Source: src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs; src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs]
4. The S1 conversation stream preserves Story 3.1 tenant/project partitioning, server-side UTC ordering, cursor pagination, safe empty/blocked/degraded states, and stale/correction safe-next-action behavior while adding the richer associated-email rendering. [Source: _bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md#Current-State-To-Preserve]
5. Associated-email rendering is localized in English and French; all labels, accessible names, status copy, unavailable reasons, and metadata labels come from `ChatBotUiTextKey` resources or existing governed primitives. [Source: _bmad-output/planning-artifacts/epics.md#Cross-cutting-acceptance--planning-guidance; src/Hexalith.ChatBot.UI/Localization/SharedResource.resx]
6. Forced-colors, reduced-motion, responsive phone/tablet layout, keyboard navigation, and screen-reader output are covered by component/static tests and E2E fixture coverage; visual state must not rely on color alone and must not introduce raw color literals. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#S1; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-Floor]

## Tasks / Subtasks

- [x] Extend the S1 conversation item contract with source-email identity fields needed for metadata rendering (AC: 2, 3)
  - [x] Add optional/additive fields to `ProjectConversationItem` and OpenAPI `ProjectConversationItem` for provider message id, internet message id, received/sent/created UTC timestamps, source timezone, and safe source-provenance display token where required by AC2; do not expose raw opaque `SourceContext` provider state.
  - [x] Keep enum wire tokens stable (`email-derived`, `system-decision`, `mailbox`, `system-decision`) and regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated client output.
  - [x] Add contract serialization/OpenAPI tests proving new fields are metadata-only and raw body/provider payload terms are absent.
- [x] Enrich the conversation projection from existing intake and association events (AC: 2, 3, 4)
  - [x] Reuse `MailboxMessageIntakeCaptured` as the source for mailbox/provider identity and timestamp metadata; do not introduce raw email body capture or a separate mailbox transcript store.
  - [x] Add a projection path that merges intake source identity with the association-derived `ProjectConversationItemView` by tenant + intake/association identity while preserving idempotent, order-tolerant source-version replacement.
  - [x] Preserve `ProjectConversationItemView.KeyFor` tenant/project partitioning and cursor behavior; no cross-tenant or cross-project scan followed by filtering.
  - [x] Keep correction/stale behavior from Story 3.1: `Correcting` and `CorrectionDelayed` render safe next action and must not present corrected context as current.
- [x] Update the UI service and Fluxor model mapping (AC: 1, 2, 4, 5)
  - [x] Extend `ProjectConversationItemModel` and `ProjectConversationService.MapItem` to carry the new metadata fields without referencing server projection types, DAPR, or EventStore internals.
  - [x] Preserve load/failure reducers that clear prior project state so a route change cannot display stale content under another project.
- [x] Refine `ChatBotEmailConversationItem` as the associated-email card (AC: 1, 2, 3, 5, 6)
  - [x] Render a compact source-email summary using governed primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, definition-list metadata, stable `<time>` elements, and non-color status text.
  - [x] Accessible name must start with the actor type/label before the email metadata; system decisions must remain labelled separately and must not reuse the email summary as anonymous chat.
  - [x] Show plain-language labels before stable IDs; keep IDs in metadata/code styling and include `data-chatbot-conversation-item-id` for fixture/E2E selectors.
  - [x] Add EN/FR resource keys for any new labels; no hard-coded user-facing strings except safe fallback tokens already established by the UI layer.
- [x] Maintain S1 visual, responsive, and accessibility behavior (AC: 1, 5, 6)
  - [x] Extend `chatbot.tokens.css` with existing Fluent/FrontComposer tokens only; no raw `#`, `rgb(`, or `hsl(` colors.
  - [x] Ensure phone/tablet layout wraps metadata without overlap, preserves reading order, and keeps touch/keyboard targets reachable.
  - [x] Reduced-motion must suppress any item transition/shimmer/streaming movement; forced-colors must preserve borders, labels, and status affordances.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for DTO serialization, OpenAPI schema, generated client availability, stable enum wire tokens, and absence of raw body/provider payload fields.
  - [x] Server projection tests for intake+association merge, duplicate/older replay behavior, out-of-order delivery, tenant/project partitioning, cursor safety, and correction stale state.
  - [x] UI service/state/component tests for mapped source identity, actor-label accessible names, evidence/status/timestamp ordering, localization keys, no stale prior project content, and no raw colors.
  - [x] Conformance/read-surface tests proving foreign/unknown/malformed/missing/ambiguous/stale/unsafe tenant contexts still collapse to safe denial with metadata-only bodies.
  - [x] Update the existing Playwright fixture coverage for populated S1 stream to assert the associated-email source metadata renders in order, remains metadata-only, and passes forced-colors/reduced-motion checks where the harness supports them.

## Dev Notes

### Scope Boundaries

- This story is the associated-email rendering increment for S1. It must not implement participant rendering (Story 3.3), attachment rendering/status (Stories 3.4, 3.12, 3.13), association evidence drawer/why panel (Story 3.9), decision history rendering (Story 3.5), approvals (Story 3.6), failures/retries (Story 3.7), AI outcomes/summaries (Story 3.8), classification/review history (Story 3.11), or scoped AI context packaging (Story 3.14).
- Do not add a chat composer or message-send workflow. The architecture defines S1 as a read projection; future writes go through the CommandGateway, not a separate chat subsystem. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend-Architecture]
- Do not invent raw email body storage. Current command/event contracts intentionally preserve source identity, participants, timestamps, and attachment references only; body content is out of scope. [Source: src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs; src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs]

### Existing Code To Reuse

- S1 route/shell/state: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- S1 governed stream/item primitives: `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor` and `ChatBotEmailConversationItem.razor`.
- Governed visual/accessibility primitives: `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotActorBadge`, `ChatBotStatusBanner`, `ChatBotBlockedState`, `ChatBotEvidenceChip`.
- Current read contract/projection: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `ProjectConversationResponse.cs`, OpenAPI `ProjectConversationItem`, `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs`.
- Source identity contracts: `src/Hexalith.ChatBot.Contracts/Commands/MailboxMessageSourceIdentity.cs`, `MailboxParticipantIdentity.cs`, `MailboxRecipientIdentity.cs`, and `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs`.
- Existing tests to extend: `ProjectConversationContractTests`, `ProjectConversationProjectionTests`, `ProjectConversationServiceTests`, `AssociationReviewComponentContractTests`, `CrossTenantReadSurfaceIsolationTests`, and `ProjectConversationE2ETests`.

### Current State To Preserve

- Story 3.1 added `GET /api/v1/projects/{projectId}/conversation`, generated client support, metadata-only S1 rendering, cursor pagination, tenant/project keyed projection storage, safe empty state only for authorized projects, and UI reducers that clear stale project state on load/failure.
- `ProjectConversationItemView.ShouldReplace` rejects older source-version replays; both DAPR and in-memory stores rely on this rule. Any enrichment from intake events must preserve monotonic replacement and not overwrite newer association/correction state with older source identity.
- `AssociationProjectionHandler` already projects association events into conversation items via `ProjectConversationItemView.FromAssociation`. Extend or complement this path; do not replace it with direct EventStore reads from the UI.
- Existing worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. UI reads through `IChatBotClient` only; server projection internals stay internal. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- Tenant authority comes from authenticated claims/context, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe tenant context must collapse to safe denial without confirming restricted resource existence.
- Store keys, indexes, cursors, telemetry, logs, error bodies, and test fixture output must remain tenant/project scoped and metadata-only.
- DAPR pub/sub is at-least-once and unordered. Projection handlers must tolerate duplicate and out-of-order intake/association events and must be safe when source identity arrives before or after association.
- Cursor tokens stay opaque and tenant/project scoped; do not embed tenant, project, mailbox, source message, sender, or provider payload text.
- Use ULIDs for any new identifiers; do not introduce GUID parsing.

### UX And Accessibility Guardrails

- The UX package is binding despite having no mockups. Conversation stream rows expose actor type, permitted identity label, timestamp, source surface, and state label; actor-type label precedes content in accessible names. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics]
- Evidence, risk, status, actor, and timestamp must appear in consistent order across conversation items; use text labels, not color alone.
- Conversation stream focus remains stable; Tab reaches message/event groups and actions, and reduced motion suppresses non-essential item movement.
- Redacted/unauthorized states must remain understandable to screen-reader users without leaking hidden content.

### Project Structure Notes

- Contract DTO and enum changes belong in `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Enums/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server projection changes belong under `src/Hexalith.ChatBot.Server/Projections/`; source-intake event contracts remain in the existing Association/Intake area.
- UI changes belong in `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.1 review fixed DAPR stale replay handling, cross-project stale UI state, authorized empty conversation reads, and stable rendered item ids. Treat those as regression targets in this story.
- Story 3.1 validation used compiled xUnit v3 executables because VSTest socket creation was blocked in the sandbox. Prefer the same fallback if `dotnet test` fails with local socket permission errors.
- Epic 2 retrospective warns Epic 3 not to invent a separate conversation model; project from metadata/source-evidence records already behind the contract spine and carry corrected-context readiness forward.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1`
  - targeted contract/client/server/UI/conformance tests touched by this story
  - Playwright/UI.E2E fixture tests if the existing harness can run locally
- Use xUnit v3, Shouldly, NSubstitute, bUnit, and existing Playwright harnesses only; do not add assertion, mocking, UI, or E2E libraries.
- If VSTest cannot open sockets in this sandbox, run compiled xUnit v3 test executables and record that limitation in the Dev Agent Record.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, DAPR 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints--Dependencies]
- Fluent UI v5 remains RC in the architecture; keep customization minimal and use existing FrontComposer/governed primitives instead of adding a custom visual system.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.2 requirements.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR2, FR21-FR28, S1 surface inventory, NFR60 accessibility scope.
- `_bmad-output/planning-artifacts/architecture.md` - frontend architecture, projection boundaries, DAPR/event ordering, testing standards, file organization.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - conversation semantics, focus model, reduced-motion, accessibility floor, component rules.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - Fluent/FrontComposer component and token constraints.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` - previous S1 implementation context and review fixes.
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-01.md` - Epic 3 preview and source-evidence reuse warning.

## Dev Agent Record

### Senior Developer Review (AI)

Reviewer: Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- HIGH: Source-email enrichment raised `ProjectConversationItemView.SourceVersion` to the intake event version, which could let stale association item writes pass conversation-store replay checks. Fixed by keeping association source version separate from source-email enrichment and adding regression coverage.
- HIGH: In-memory source-email replay handling rejected stale source records in `_sourceEmails` but still applied the stale incoming metadata to existing conversation items. Fixed by enriching items only when the incoming source record is the effective stored source.
- HIGH: Associated-email UI rendered confidence but omitted the threshold band required by AC2. Fixed by adding localized confidence and threshold-band metadata rows and E2E/static coverage.
- MEDIUM: Mailbox intake projection translation accepted a default `ReceivedAtUtc` and the source provenance display fallback could echo unknown provenance text. Fixed by rejecting missing received timestamps and using a safe fallback token for unknown provenance.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed, 0 warnings, 0 errors.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests` - passed 12/12.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests` - passed 3/3.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests` - passed 14/14.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests` - passed 1/1.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.AssociationReviewComponentContractTests` - passed 4/4.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - passed 5/5.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none -class Hexalith.ChatBot.Conformance.Tests.CrossTenantReadSurfaceIsolationTests` - passed 8/8.
- `git diff --check -- <review-touched files>` - passed.

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01T02:16:58+02:00 - Marked story and sprint status in-progress; preserved existing `baseline_commit`.
- 2026-06-01T02:18:00+02:00 - Confirmed `dotnet test` VSTest socket path is blocked by sandbox permission; used compiled xUnit v3 executables for validation.
- 2026-06-01T02:29:14+02:00 - Completed validation with clean solution build and compiled xUnit regression pass; Tier-3 Aspire E2E remained skipped by existing Docker/DAPR opt-in guard.

### Completion Notes List

- Added metadata-only source-email fields to the contract spine and DTO: provider message id, internet message id, source timestamps, source timezone, and safe source provenance display token. Raw provider `SourceContext`, body content, and provider payload fields are not exposed.
- Added mailbox-intake projection handling and source-email view storage so `MailboxMessageIntakeCaptured` can enrich association-derived S1 conversation items whether intake or association events arrive first.
- Updated S1 UI mapping and associated-email rendering to show localized metadata labels, stable ID/code styling, stable `<time>` elements, governed actor/evidence primitives, focusable conversation items, and forced-colors/reduced-motion-safe styling.
- Expanded contract, client-generation, server projection, conformance, UI service/static component, and UI E2E fixture coverage for associated-email metadata and metadata-only safety.
- Validation passed through compiled xUnit v3 executables because VSTest socket creation is blocked in this sandbox.

### File List

- `_bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/MailboxIntakeProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/MailboxIntakeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationSourceEmailView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationHttpHost.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Change Log

- 2026-06-01 - Implemented associated-email metadata rendering for S1, including contract/client regeneration, intake+association projection enrichment, localized UI metadata rendering, and expanded validation coverage.
- 2026-06-01 - Senior review auto-fixed projection replay/version safety, stale source replay handling, missing threshold-band UI metadata, and intake/source-provenance validation; story approved and marked done.
