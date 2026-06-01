---
baseline_commit: 0bbf8398536b4503663a61c2350a099db175bff9
---

# Story 3.4: Attachment rendering in the conversation stream

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want attachments represented in the project conversation,
so that file context is visible with governed state and authorization.

## Acceptance Criteria

1. Given an attachment conversation item, when it renders on S1, then the item exposes actor attribution, an actor-type label before content in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, reduced-motion behavior, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.4; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics]
2. Attachment items render metadata only: provider attachment id, safe display name, content type classification, size, source mailbox id, source conversation/thread id, association id, lifecycle state, capture/storage/scan/status tokens, duplicate/retry state, AI-context eligibility, file/folder references when available, redaction state, retention class, schema version, source version, and correlation id. No attachment content, raw provider payload, raw source context, malware scan detail, unauthorized project/file/folder names, raw exception text, or hidden diagnostic data may be exposed. [Source: src/Hexalith.ChatBot.Contracts/Commands/MailboxAttachmentReference.cs; src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR32]
3. Unauthorized, restricted, unsafe, or unavailable attachment metadata/content renders as redacted/unavailable without confirming restricted resource existence. Redacted values must be visibly distinct from missing/unknown values and must remain understandable to screen-reader users. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.4; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2; Hexalith.Folders/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
4. Attachment conversation items materialize only for authorized project conversations that already have an associated S1 item for the same tenant and intake. Attachment references that arrive before association remain pending by tenant/intake and materialize after association; association before attachment references also materializes correctly. No implementation may scan all tenants/projects and filter afterward. [Source: src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs; _bmad-output/planning-artifacts/architecture.md#Data-boundaries]
5. S1 preserves Story 3.1, 3.2, and 3.3 behavior: tenant/project partitioning, cursor pagination, source-version replay safety, participant materialization, metadata-only associated-email rendering, stale/correction safe-next-action behavior, EN/FR localization, responsive layout, forced-colors, reduced-motion, and UI state clearing on route load/failure. [Source: _bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md#Current-State-To-Preserve; _bmad-output/implementation-artifacts/3-3-participant-rendering-in-the-conversation-stream.md#Current-State-To-Preserve]
6. Contract, generated client, server projection, conformance, UI service/state/component, localization, static CSS, and UI E2E fixture coverage prove attachment rendering is safe, localized, accessible, metadata-only, replay-safe, and cross-tenant isolated. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR65-NFR70]

## Tasks / Subtasks

- [x] Extend the S1 contract spine for attachment conversation items (AC: 1, 2, 3, 6)
  - [x] Add additive fields to `ProjectConversationItem` and OpenAPI `ProjectConversationItem` for attachment metadata. Suggested names: `sourceProviderAttachmentId`, `attachmentDisplayName`, `attachmentContentType`, `attachmentSizeInBytes`, `attachmentCaptureStatus`, `attachmentStorageStatus`, `attachmentScanStatus`, `attachmentFolderId`, `attachmentFileId`, `attachmentDuplicateState`, `attachmentRetryState`, `attachmentAiContextEligibility`, `attachmentAllowedActions`, and `attachmentRedactionState`.
  - [x] Add stable enum wire tokens for attachment item and actor classification. Suggested tokens: item kind `attachment`; actor kind `mailbox-attachment` if a distinct actor kind is useful, otherwise reuse `mailbox` while keeping the accessible label "Mailbox attachment". Preserve existing `email-derived`, `system-decision`, `participant`, mailbox, and participant wire tokens.
  - [x] Add explicit attachment status enum tokens aligned to the FR34 vocabulary: `captured`, `pending`, `unavailable`, `rejected`, `unsafe`, `failed`, and `retryable`. Initial Story 3.4 projection may emit only pending/unavailable/redacted-safe states; Stories 3.12 and 3.13 will enrich capture/storage/scanner truth.
  - [x] Regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`; never hand-edit generated client output.
  - [x] Add contract/OpenAPI/generated-client tests proving attachment fields are additive, enum wire values are stable, and raw payload/body/content/source-context fields are absent.
- [x] Materialize attachment references into the existing project conversation projection (AC: 2, 3, 4, 5)
  - [x] Reuse existing intake artifacts: `PublishedMailboxIntakeEvent.AttachmentReferences`, `MailboxIntakeProjectionTranslator`, `MailboxMessageIntakeCaptured.AttachmentReferences`, and `MailboxAttachmentReference`. Do not add a second mailbox intake read model.
  - [x] Extend `ProjectConversationSourceEmailView` or add a projection-side attachment reference view keyed by tenant plus intake so attachment references can arrive before or after association.
  - [x] Materialize attachment conversation items only after tenant plus intake has an associated project conversation item. If attachment references arrive first, store pending attachment reference state by tenant/intake and materialize when association is known. If association arrives first, materialize when attachment reference state is projected.
  - [x] Use a deterministic item id per attachment such as `attachment:{associationId}:{providerAttachmentId}` or an equivalent redaction-safe stable id. Cursor tokens must remain opaque and tenant/project scoped.
  - [x] Preserve source-version independence: attachment reference source-version replacement must not raise or lower association source version, source-email enrichment source version, or participant source version.
  - [x] During `Correcting` or `CorrectionDelayed`, attachment items must follow the parent intake safe-next-action semantics and must not present corrected folder/file context as current.
- [x] Keep attachment rendering metadata-only and authorization-safe (AC: 2, 3, 5)
  - [x] Render provider-supplied filename, content type, and size only when the server marks the metadata authorized and non-restricted. Otherwise render localized redacted/unavailable labels without confirming hidden file existence.
  - [x] Show `FileId` and `FolderId` only as stable references when already available and authorized. Story 3.4 must not implement actual Hexalith.Folders storage, move/relink, malware scanning, or AI-context packaging.
  - [x] Do not call Folders, DAPR, EventStore, scanner services, server projection internals, or mailbox provider APIs from the UI. UI reads only through `IChatBotClient`.
  - [x] Keep raw attachment content, source provider payloads, source context, scanner internals, unauthorized folder/file names, and audit-restricted details out of contracts, UI, logs, test fixture output, and diagnostics.
- [x] Update UI mapping and attachment rendering components (AC: 1, 2, 3, 5, 6)
  - [x] Extend `ProjectConversationItemModel` and `ProjectConversationService.MapItem` to carry attachment metadata through `IChatBotClient` only.
  - [x] Update `ChatBotConversationStream.razor` to route attachment items to a dedicated `ChatBotAttachmentConversationItem.razor` instead of overloading `ChatBotEmailConversationItem` or `ChatBotParticipantConversationItem`.
  - [x] Implement `ChatBotAttachmentConversationItem.razor` with existing governed primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, stable `<time>` elements, definition-list metadata, localized labels, and governed layout classes.
  - [x] Actor labels must lead the accessible name. Recommended accessible-name shape: "Mailbox attachment, <safe display label or redacted attachment>, <status>, <lifecycle state>".
  - [x] Disabled or unavailable attachment actions must expose a reachable reason via inline text or a focusable "Why unavailable?" affordance. Tooltip-only explanations are not acceptable.
  - [x] Add EN/FR resource keys for attachment labels, safe display names, redaction states, status copy, allowed actions, accessible names, unavailable reasons, and metadata labels. Do not hard-code user-facing strings except stable machine tokens displayed as metadata.
- [x] Maintain S1 responsive, visual, and accessibility behavior (AC: 1, 3, 5, 6)
  - [x] Extend `chatbot.tokens.css` using existing Fluent/FrontComposer tokens only; do not introduce raw `#`, `rgb(`, or `hsl(` color literals.
  - [x] Ensure phone/tablet layouts wrap attachment metadata without overlap, preserve actor/status/evidence/timestamp order, and keep attachment actions at reachable touch and keyboard targets.
  - [x] Ensure forced-colors and reduced-motion rules cover attachment rows, actor badges, evidence chips, status labels, focus outlines, and unavailable-action explanations.
  - [x] Ensure attachment status is never color-only. Include text labels and icon/shape/border affordances where needed.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for DTO serialization, OpenAPI schema, generated client availability, attachment enum wire tokens, and absence of raw attachment content/body/source-provider payload fields.
  - [x] Server projection tests for attachment-before-association, association-before-attachment, multiple attachments, duplicate provider attachment ids, stale replay, newer replay, correction stale state, and tenant/project partitioning.
  - [x] Conformance/read-surface tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant contexts collapse to safe denial with metadata-only bodies and no attachment metadata leakage.
  - [x] UI service/state/component tests for mapped attachment metadata, actor-label accessible names, evidence/status/timestamp order, localization keys, no stale prior project content, no raw colors, and no raw attachment content.
  - [x] Update existing Playwright/UI.E2E fixture coverage for populated S1 stream to include authorized, pending-scan, unavailable, redacted, duplicate/retry, and unsafe attachment states with forced-colors and reduced-motion assertions where the harness supports them.

## Dev Notes

### Scope Boundaries

- This story is attachment rendering on the existing S1 project conversation stream. It is not attachment capture/storage, scanner integration, folder creation, file download, file preview, AI-context packaging, operational queue management, or correction-driven attachment relinking.
- Story 3.4 may establish contract fields and render pending/unavailable/redacted attachment state so later stories can enrich them. Actual capture into Hexalith.Folders is Story 3.12; detailed attachment states, unsafe handling, and authorization enforcement are Story 3.13; scoped AI-context packaging is Story 3.14.
- Do not change mailbox intake command semantics unless an additive projection/contract field is strictly required for rendering. The current durable input is `CaptureMailboxMessageIntake.Attachments` as provider-owned references only.
- Do not implement association/correction decision rendering (Story 3.5), approval events (Story 3.6), failures/retries (Story 3.7), AI outcomes (Story 3.8), the "why this project" panel (Story 3.9), conversation item next-action consolidation (Story 3.10), or classification/review history (Story 3.11).
- Do not add a chat composer, file upload UI, attachment download UI, file browsing UI, scanner UI, or direct folder mutation workflow.

### Existing Code To Reuse

- S1 contract and read surface: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `ProjectConversationResponse.cs`, `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`, `ProjectConversationActorKind.cs`, OpenAPI `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, and generated client output under `src/Hexalith.ChatBot.Client/Generated/`.
- Intake attachment source: `src/Hexalith.ChatBot.Contracts/Commands/CaptureMailboxMessageIntake.cs`, `MailboxAttachmentReference.cs`, `src/Hexalith.ChatBot.Server/Association/Intake/MailboxMessageIntakeCaptured.cs`, `src/Hexalith.ChatBot.Server/Projections/PublishedMailboxIntakeEvent.cs`, and `MailboxIntakeProjectionTranslator.cs`.
- S1 projection store and item model: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `ProjectConversationSourceEmailView.cs`, `IProjectConversationProjectionStore.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs`, and `ProjectConversationPage.cs`.
- Existing materialization pattern: Story 3.3 participant handling in `InMemoryProjectConversationProjectionStore.UpsertParticipantResolutionAsync` and `ProjectConversationItemView.FromParticipant`. Attachment materialization should follow the same tenant/intake pending-then-materialize pattern, without sharing participant dictionaries.
- UI route/state/service: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- Governed UI primitives: `ChatBotConversationShell`, `ChatBotProjectContextHeader`, `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, localization resources, and `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Existing tests to extend: `ProjectConversationContractTests`, `ClientGenerationTests`, `ProjectConversationProjectionTests`, `CrossTenantReadSurfaceIsolationTests`, `ProjectConversationServiceTests`, `AssociationReviewComponentContractTests`, `ChatBotLocalizationContractTests`, and `ProjectConversationE2ETests`.

### Current State To Preserve

- Story 3.1 added `GET /api/v1/projects/{projectId}/conversation`, tenant/project keyed S1 projection storage, cursor pagination, authorized empty reads, safe denial for unauthorized reads, governed UI route/state, and reducers that clear previous project state on load/failure.
- Story 3.2 enriched S1 items with source-email metadata from mailbox intake while keeping raw provider `SourceContext` and email bodies out of the read contract. Source-email enrichment has its own source version and must not overwrite newer association/correction state.
- Story 3.3 added participant item rendering and pending materialization by tenant/intake. Participant source-version replacement is independent from association and source-email replacement.
- `ProjectConversationItemView.ShouldReplace` currently guards item replacement by source version. If attachment items use independent source versions, tests must prove stale attachment replay cannot overwrite newer attachment state and cannot mutate parent association state.
- `ProjectConversationSourceEmailView` currently does not retain attachment references. This story will likely need to extend it or add a separate attachment reference view. Keep the shape metadata-only and authorization-safe.
- Existing worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert it.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. UI reads through `IChatBotClient` only; server projection internals, DAPR, EventStore, scanner services, Folders adapters, and mailbox provider APIs stay server-side. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- ChatBot orchestrates attachment state; Hexalith.Folders owns governed project folders, file access control, file metadata, and eventual file storage. Store stable `FileId` and `FolderId` references only when authorized and available; do not copy Folders authority into ChatBot. [Source: _bmad-output/planning-artifacts/architecture.md#Service-boundaries; Hexalith.Folders/_bmad-output/project-context.md#Critical-Dont-Miss-Rules]
- Tenant authority comes from authenticated claims/context and projection gates, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe tenant context must collapse to safe denial without confirming restricted resource existence. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR1-NFR12]
- DAPR pub/sub is at-least-once and unordered. Projection handlers must tolerate duplicate and out-of-order intake, association, participant, and attachment-reference events. SignalR nudges, if used, trigger re-query and are never trusted as data. [Source: _bmad-output/planning-artifacts/architecture.md#Integration-Points; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Cursor tokens stay opaque and tenant/project scoped. Do not embed tenant, project, mailbox, provider attachment id, file id, folder id, file name, evidence, or provider payload text in cursor values.
- Use `System.Text.Json` shared options and camelCase wire names. Do not add inline `JsonSerializerOptions`, Newtonsoft.Json, or new serialization libraries.

### UX And Accessibility Guardrails

- The UX package is binding despite having no mockups. Attachment rows in conversation must expose filename display when authorized, storage/scan/context state, and allowed actions; restricted metadata is redacted consistently. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics]
- Attachment rows use the semantic attachment-row component treatment from the design package and existing Fluent/FrontComposer tokens. Do not create a separate visual language or raw color palette. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
- Evidence, risk/status, actor, and timestamp must appear in consistent order. Plain-language labels precede raw IDs; IDs remain available as metadata.
- Conversation stream focus remains stable: Tab reaches message/event groups and actions, and reduced motion suppresses non-essential item movement.
- EN/FR localization is required. Stable machine codes, IDs, status codes, reason codes, and correlation ids remain untranslated; labels and explanations are translated. Avoid concatenated strings for accessible names.
- Touch targets for attachment actions must be at least 44 by 44 CSS pixels where layout allows; compact dense rows need at least 24 by 24 CSS pixels or equivalent spacing. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Responsive-Model]

### Project Structure Notes

- Contract DTO and enum changes belong under `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Enums/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server projection changes belong under `src/Hexalith.ChatBot.Server/Projections/`. If a minimal server-side attachment display authorization adapter is required, put it under `src/Hexalith.ChatBot.Server/Adapters/Folders/` and keep it server-side.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.3 established the additive item pattern for participant rendering. Reuse the pattern: additive enum values, additive DTO fields, generated client regeneration, independent source-version handling, pending materialization by tenant/intake, dedicated UI component, localized EN/FR strings, and E2E fixture expansion.
- Story 3.3 review fixed localized participant status/reason/action rendering and actor badge fallback. Attachment rendering must not repeat raw enum display for user-facing labels; machine tokens may appear only as metadata where intended.
- Story 3.2 review fixed stale source-email replay, association/source-email source-version conflation, and missing threshold-band metadata. Attachment enrichment must keep replay/version ownership separate and must show status metadata explicitly.
- Story 3.1 review fixed DAPR stale replay handling, cross-project stale UI state, authorized empty conversation reads, and stable rendered item ids. Attachment rendering must preserve those regression targets.
- Epic 2 retrospective and architecture both warn not to invent a separate conversation model. Build on the existing contract spine and S1 projection; do not introduce a transcript table, attachment-specific UI data plane, or direct Folders data plane in the browser.
- Prior validation used compiled xUnit v3 executables because VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - targeted contract/client/server/UI/conformance tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Add regression tests around event ordering: attachment reference before association, association before attachment reference, source-email before attachment reference, attachment reference before source-email, stale attachment replay after current attachment, duplicate provider attachment id, and project correction while attachment items exist.
- Include negative content assertions for attachment content, provider source context, raw provider payload, raw scanner detail, unauthorized file/folder names, unauthorized project names, raw exception text, and hidden diagnostic data in API, UI, fixture, logs/test output where applicable.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, DAPR 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints--Dependencies]
- NuGet lists `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` as a newer prerelease than the repo-pinned `5.0.0-rc.2-26098.1`, but this story must keep the repo-pinned Fluent/FrontComposer stack unless a separate dependency-upgrade story is created. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]
- Dapr pub/sub documentation confirms at-least-once delivery semantics. Attachment projection code must therefore be idempotent and out-of-order tolerant. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Microsoft Learn tracks .NET 10 breaking changes separately; do not mix framework migration work into attachment rendering. [Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.4 requirements.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR22, FR29-FR34, FR77, NFR1-NFR14, NFR21, NFR32-NFR34, NFR52-NFR55, NFR60-NFR70.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - tenant policy knob `attachments.unsafe-handling`.
- `_bmad-output/planning-artifacts/architecture.md` - projection boundaries, DAPR/event ordering, Folders ownership, format patterns, file organization, and testing standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - attachment rows, conversation semantics, focus model, reduced-motion, accessibility floor, responsive behavior, and safe redaction.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - attachment-row component semantics and Fluent/FrontComposer token rules.
- `Hexalith.Folders/_bmad-output/project-context.md` - Folders ownership, metadata-only diagnostics, safe denial, and redacted values.
- `Hexalith.Conversations/_bmad-output/project-context.md` - conversation read-time hydration, stable ID rules, no transcript table, and fail-closed rules.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` - S1 shell/read surface implementation context and review fixes.
- `_bmad-output/implementation-artifacts/3-2-associated-email-rendering-in-the-conversation-stream.md` - associated-email enrichment context and review fixes.
- `_bmad-output/implementation-artifacts/3-3-participant-rendering-in-the-conversation-stream.md` - participant item materialization and UI pattern to reuse.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet build src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj --no-restore -m:1 /nr:false` - regenerated client and completed successfully.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - completed successfully with 0 warnings and 0 errors.
- `dotnet test Hexalith.ChatBot.slnx --no-build -m:1 /nr:false` - blocked by sandbox VSTest socket permission (`SocketException (13): Permission denied`).
- Compiled xUnit v3 runners completed successfully: 629 discovered tests, 627 passed, 2 existing Tier-3 Aspire/DAPR integration tests skipped, 0 failed.
- `dotnet test tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --no-restore -m:1 /nr:false --filter FullyQualifiedName~ProjectConversationProjectionTests` - blocked by sandbox VSTest socket permission (`SocketException (13): Permission denied`).
- `dotnet test tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --no-restore -m:1 /nr:false --filter "FullyQualifiedName~AssociationReviewComponentContractTests|FullyQualifiedName~ChatBotLocalizationContractTests|FullyQualifiedName~ProjectConversationServiceTests"` - blocked by sandbox VSTest socket permission (`SocketException (13): Permission denied`).
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - review build completed successfully with 0 warnings and 0 errors.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests` - completed successfully: 21 passed, 0 failed.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.AssociationReviewComponentContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests` - completed successfully: 16 passed, 0 failed.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests` - completed successfully: 3 passed, 0 failed.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` - completed successfully: 7 passed, 0 failed.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none` - completed successfully: 56 passed, 0 failed.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -parallel none` - completed successfully: 35 passed, 0 failed.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none` - completed successfully: 15 passed, 0 failed.

### Completion Notes List

- Added the additive S1 attachment contract spine, OpenAPI schema fields, wire-token enums, generated client output, and generated-client hash fixture.
- Materialized attachment references through the existing project conversation projection with tenant/intake pending state, deterministic item IDs, stale replay protection, duplicate-provider-id handling, and metadata-only item output.
- Added a dedicated governed attachment conversation component, UI mapping/state fields, EN/FR localization, responsive/forced-colors/reduced-motion CSS coverage, and no direct UI calls to Folders, scanner, DAPR, EventStore, or mailbox providers.
- Extended contract, architecture, server projection, UI service/component/localization, static CSS, and UI E2E coverage for attachment rendering, replay ordering, metadata-only safety, accessibility, and localization.
- Review fixes tightened redacted attachment handling so restricted metadata does not leak size/content type, redacted metadata remains visibly distinct from unavailable metadata, and provider display names are stripped down to safe filename segments.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- HIGH: Redacted attachment references could still carry `AttachmentSizeInBytes`, leaking restricted metadata even when display name/content type were suppressed. Fixed in `ProjectConversationAttachmentSetView.FromReference` by gating size and derived attachment metadata on metadata visibility.
- HIGH: Redacted attachment UI values fell back to the same unavailable label used for missing/unknown values, violating the requirement that redacted and unavailable remain visibly distinct. Fixed in `ChatBotAttachmentConversationItem.razor` with an explicit redacted metadata value path.
- MEDIUM: Provider-supplied attachment display names only removed control characters and could preserve path-like source context. Fixed by stripping path segments before materializing the safe display name.

Checklist validation:

- Story status was reviewable, story 3.4 and File List were loaded, and git changes were cross-checked against the story File List.
- Acceptance criteria, completed tasks, changed source files, tests, security/redaction behavior, localization, accessibility, and source-version replay behavior were reviewed.
- MCP documentation search was not available in this sandbox; the review used the local story references, architecture notes, and existing project documentation.
- No critical issues remain after fixes; sprint status was synced to done.

### File List

- `_bmad-output/implementation-artifacts/3-4-attachment-rendering-in-the-conversation-stream.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationActorKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationAttachmentStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/AssociationProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationAttachmentSetView.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

## Change Log

- 2026-06-01: Implemented Story 3.4 attachment conversation rendering and moved story to review.
- 2026-06-01: Senior developer review fixed redacted attachment metadata leakage/distinction gaps and moved story to done.
