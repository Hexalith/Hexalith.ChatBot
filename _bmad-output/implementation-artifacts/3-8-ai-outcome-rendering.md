---
baseline_commit: 66d47e3e37b5f1f4f0ef892c01e51a447b2d84f2
---

# Story 3.8: AI outcome rendering

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want AI proposals, denials, executions, and outcomes represented in the project conversation,
so that AI work is visible as governed activity rather than anonymous chat content.

## Acceptance Criteria

1. Given an AI outcome conversation item, when it renders on S1, then the item exposes actor attribution, an AI/service/system actor-type label before content in the accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, reduced-motion behavior, and WCAG 2.2 AA behavior. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.8; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Conversation-and-audit-semantics]
2. AI outcome items render as governed activity, not as anonymous chat messages, raw model output, approvals, failure rows, or system decisions. They must include proposal, denial/refusal, execution-started, execution-succeeded, execution-failed, and outcome-recorded states when supplied by projection metadata. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.8; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR44]
3. AI-generated content remains visually and programmatically distinct from source evidence. Summaries, recommendations, proposed changes, and generated text must be labelled as AI-generated; source evidence references must remain separate metadata/evidence chips and must not be presented as model-authored prose. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR27; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
4. AI outcome rendering is metadata-only by default. Raw prompt text, raw model output, provider diagnostics, tool payloads/results, hidden file/message/party/policy/audit data, and unsafe generated content must not reach API, UI, logs, snapshots, fixtures, or test output unless an explicit authorized redacted display field exists for this story. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR9; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40]
5. AI proposal/outcome rows carry enough governed metadata for trust and audit handoff: proposal id, AI actor id/type, requester id, source conversation item id, operation id, correlation id, risk class/action classes, policy snapshot visibility, context package manifest id, authorized file/context references, command name/allowlist version when present, approval id/status when present, execution status, audit status, and safe next action. Restricted identifiers must be redacted without confirming hidden resource existence. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR42; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR44]
6. AI outcome history is append-only and replay-safe. Later denials, approval outcomes, execution results, failures, reprocess events, or corrected-context invalidations may link to earlier AI items but must not mutate, hide, or replace prior history except deterministic same-item/source-version replacement for the same event version. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture; _bmad-output/implementation-artifacts/3-5-association-and-correction-decision-rendering.md#Previous-Story-Intelligence]
7. S1 preserves Stories 3.1 through 3.7 behavior: tenant/project partitioning, cursor pagination, source-email enrichment, participant/attachment materialization, association/correction decisions, approval events, failure/retry/blocked rows, source-version replay safety, EN/FR localization, responsive layout, forced-colors, reduced-motion, and route-load/failure state clearing. [Source: _bmad-output/implementation-artifacts/3-7-failure-retry-and-blocked-state-rendering.md#Current-State-To-Preserve]
8. Contract, generated client, server projection, conformance, UI service/state/component, localization, static CSS, and UI E2E coverage prove AI outcome rendering is localized, accessible, metadata-only, append-only, replay-safe, AI-vs-source-evidence distinct, raw-provider-safe, and cross-tenant isolated. [Source: _bmad-output/planning-artifacts/epics.md#FR22; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60]

## Tasks / Subtasks

- [x] Extend the S1 contract spine for AI outcome conversation items (AC: 1, 2, 3, 5, 8)
  - [x] Add a distinct additive item kind. Recommended wire token: `ai-outcome`; do not reuse `email-derived`, `system-decision`, `participant`, `attachment`, `approval-event`, or `failure-state`.
  - [x] Add an actor kind for governed AI activity. Recommended wire token: `ai-actor`; preserve all existing actor wire tokens.
  - [x] Add additive `ProjectConversationItem` fields for AI outcome metadata. Suggested names: `aiOutcomeKind`, `aiOutcomeStatus`, `aiActorId`, `aiActorType`, `aiProposalId`, `aiRequestId`, `aiRequesterId`, `aiSourceConversationItemId`, `aiSourceMessageId`, `aiOperationId`, `aiCorrelationId`, `aiRiskClass`, `aiRiskActionClasses`, `aiPolicySnapshotId`, `aiPolicySnapshotVisibility`, `aiContextPackageId`, `aiContextPackageVersion`, `aiContextRedactionState`, `aiAuthorizedContextReferences`, `aiExcludedContextReasons`, `aiGeneratedSummaryRedactionState`, `aiGeneratedContentVisibility`, `aiCommandName`, `aiCommandAllowlistVersion`, `aiApprovalId`, `aiApprovalStatus`, `aiExecutionStatus`, `aiExecutionOutcomeCode`, `aiAuditOperationId`, `aiAuditStatus`, `aiFailureCode`, `aiRetryability`, `aiSafeNextAction`, `supersedesAiOutcomeId`, and `supersededByAiOutcomeId`.
  - [x] Add stable enum wire tokens where useful: outcome kind (`proposal`, `denial`, `refusal`, `approval-linked`, `execution-started`, `execution-succeeded`, `execution-failed`, `outcome-recorded`, `corrected-context-invalidated`) and outcome status (`proposed`, `blocked`, `denied`, `pending-approval`, `approved`, `executing`, `succeeded`, `failed`, `invalidated`, `unknown`).
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and update the generated-client hash fixture. Never hand-edit generated client output.
  - [x] Add contract/OpenAPI/generated-client tests proving the fields are additive, wire values are stable, existing item tokens are unchanged, and no raw `prompt`, `completion`, `modelOutput`, `providerDiagnostic`, `toolPayload`, `toolResult`, `policyBody`, `auditEnvelope`, hidden evidence, or hidden resource-name fields exist.
- [x] Add projection source and materialize S1 AI outcome items (AC: 2, 3, 4, 5, 6, 7, 8)
  - [x] Introduce projection-facing types under `src/Hexalith.ChatBot.Server/Projections/`, following the Story 3.6/3.7 pattern: `AiOutcomeEventView`, `PublishedAiOutcomeEvent`, `AiOutcomeProjectionTranslator`, and `AiOutcomeProjectionHandler` or names that match nearby conventions.
  - [x] Keep the source metadata-only and projection-facing. Consume EventStore-stamped tenant/domain/aggregate/source-version/correlation metadata, not route/body/query tenant/project values.
  - [x] Materialize deterministic item ids such as `ai:{proposalId}:{outcomeKind}:{sourceVersion}` or `ai:{operationId}:{outcomeKind}:{sourceVersion}`. IDs must stay opaque and tenant/project scoped; do not embed prompt text, generated text, file names, participant names, policy text, provider diagnostics, or hidden evidence in ids.
  - [x] Store materialized items in the existing `IProjectConversationProjectionStore`; do not add a transcript table, browser AI data plane, direct AI-provider reader, direct EventStore reader, direct audit reader, or direct approval reader from S1.
  - [x] Preserve DAPR/CloudEvent duplicate and out-of-order tolerance: duplicate source version is idempotent, stale replay cannot overwrite newer item state, execution result before proposal still renders safe metadata-only history and can be enriched later.
  - [x] Link AI outcomes to governed source context using stable ids only: project id, source message id, source conversation item id, proposal id, request id, operation id, task id, approval id, context package id, audit operation id, policy snapshot id when authorized, command name, allowlist version, risk class/action classes, and correlation id.
- [x] Keep AI generated content distinct from source evidence (AC: 3, 4, 5, 8)
  - [x] Reuse `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotStatusBanner`, stable `<time>` elements, definition-list metadata, localized labels, and governed layout classes.
  - [x] Reuse the versioned message catalog for denial/refusal/failure language where the item needs user-safe reason and next-action text. Existing relevant codes include `RefusalBlockedAction`, `FailedCommand`, `AuditUnavailable`, `DependencyDegraded`, `AssociationAiContextBlocked`, retry/failure codes from Story 3.7, and authorization/participant suppression codes. Extend the catalog only for finite missing AI outcome codes.
  - [x] Add a dedicated AI outcome component such as `ChatBotAiOutcomeConversationItem.razor`; do not overload `ChatBotEmailConversationItem`, `ChatBotDecisionConversationItem`, `ChatBotApprovalConversationItem`, or `ChatBotFailureStateConversationItem`.
  - [x] If the projection supplies an authorized summary or generated content display field, show it only with explicit AI-generated labelling and redaction/visibility metadata. If not supplied, show metadata and safe next action only.
  - [x] Source evidence references must render as evidence metadata/chips, not as model prose. The UI must make "AI-generated" and "source evidence" distinguishable for visual and screen-reader users.
  - [x] Raw prompt/output/provider/tool payloads remain absent from contract and UI. Add negative assertions for likely leakage strings and local paths.
- [x] Update UI mapping, state, localization, and styling (AC: 1, 2, 3, 5, 7, 8)
  - [x] Extend `ProjectConversationItemModel` and `ProjectConversationService.MapItem` to carry AI outcome metadata through `IChatBotClient` only.
  - [x] Update `ChatBotConversationStream.razor` to route AI outcome items to the dedicated component.
  - [x] Actor label must lead the accessible name. Recommended shape: "AI actor, <localized outcome kind>, <localized outcome status>, <timestamp>".
  - [x] Keep evidence/risk/status/actor/timestamp ordering consistent with existing S1 rows. Plain-language labels precede IDs; IDs remain available as metadata.
  - [x] Add EN/FR resource keys for outcome kind/status, AI actor labels, proposal/execution labels, AI-generated/source-evidence distinction, policy/context package labels, risk/action-class labels, audit availability, safe next action, accessible names, redacted/unavailable reasons, and metadata labels. Avoid concatenated strings for accessible names.
  - [x] Extend `chatbot.tokens.css` using existing Fluent/FrontComposer tokens only; do not introduce raw `#`, `rgb(`, or `hsl(` color literals.
- [x] Preserve existing S1 behavior and prior story fields (AC: 6, 7, 8)
  - [x] Treat `src/Hexalith.ChatBot.Server/Program.cs` `ToContractItem` as an update chokepoint: pass through all new AI outcome fields and regression-protect existing Story 3.7 failure-state fields so adding AI does not silently drop prior metadata.
  - [x] Preserve `ProjectConversationItemView.IsSourceEmailEnrichableKind` semantics. AI outcome rows should not be source-email-enriched unless a deliberate, tested rule says they are; source email context should link by stable ids.
  - [x] Preserve authorization redaction rules from approval/failure rows: policy snapshot ids and audit operation ids render only when their visibility/status permits it.
  - [x] Preserve route-load/failure state clearing, authorized empty reads, cursor opacity, tenant/project partitioning, and same-source-version replacement semantics.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for DTO serialization, OpenAPI schema, generated client availability, AI outcome wire tokens, stable existing item tokens, and absence of raw AI/provider/tool fields.
  - [x] Server projection tests for proposal, denial, refusal, approval-linked, execution started, execution success, execution failure, outcome recorded, corrected-context invalidated, duplicate delivery, stale replay, result-before-proposal, proposal-before-approval, approval-before-outcome, failure-before-outcome, and tenant/project partitioning.
  - [x] Conformance/read-surface tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant/project contexts collapse to safe denial with metadata-only bodies and no prompt/output/provider/tool/policy/evidence leakage.
  - [x] UI service/state/component tests for mapped AI metadata, actor-label accessible names, evidence/risk/status/timestamp order, localization keys, AI-vs-source-evidence distinction, no raw colors, and no raw prompt/output/provider diagnostics.
  - [x] Update Playwright/UI.E2E fixture coverage for populated S1 stream to include AI proposal, denial/refusal, execution started, execution succeeded, execution failed, outcome recorded, and corrected-context invalidated rows with forced-colors and reduced-motion assertions where the harness supports them.

## Dev Notes

### Scope Boundaries

- This story is AI outcome rendering on the existing S1 project conversation stream.
- It may establish additive contract fields, projection event envelopes, server projection materialization, UI service/state/component mapping, localization, CSS, fixtures, and tests needed to display AI proposals, denials/refusals, executions, and outcomes already present in projection metadata.
- Do not implement task-intent detection, AI risk classification, tenant policy editing, model invocation, prompt construction, context packaging, approval-gate policy logic, allowlisted command execution, outbound communication, memory/vector indexing, or AI provider adapters in this story. Those belong to Stories 3.14 and Epic 4.
- Do not add a chat composer, direct model response stream, browser-side provider transcript, direct AI-provider reader, direct audit browser, direct EventStore reader in the UI, or direct server projection access from the UI.
- Do not make AI content look like source email, attachment evidence, completed human work, or approved command output unless the projection explicitly says the governed action reached that state.

### Existing Code To Reuse

- S1 contract and read surface: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `ProjectConversationResponse.cs`, `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`, `ProjectConversationActorKind.cs`, `RiskClass.cs`, OpenAPI `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, and generated client output under `src/Hexalith.ChatBot.Client/Generated/`.
- S1 projection store and item model: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `IProjectConversationProjectionStore.cs`, `InMemoryProjectConversationProjectionStore.cs`, `DaprProjectConversationProjectionStore.cs`, and `ProjectConversationPage.cs`.
- Latest projection patterns: `ApprovalEventView`, `PublishedApprovalEvent`, `ApprovalProjectionTranslator`, `ApprovalProjectionHandler`, `FailureStateEventView`, `PublishedFailureStateEvent`, `FailureStateProjectionTranslator`, and `FailureStateProjectionHandler`.
- API mapping chokepoint: `src/Hexalith.ChatBot.Server/Program.cs` `ToContractItem`. This currently maps many S1 fields into `ProjectConversationItem`; any new AI outcome field must be added here and tests should protect prior approval/failure metadata from being dropped.
- UI route/state/service: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotApprovalConversationItem.razor`, `ChatBotFailureStateConversationItem.razor`, `ChatBotDecisionConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- Governed UI primitives: `ChatBotActorBadge`, `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, `ChatBotStreamingStopControl`, localization resources, live-region feedback matrix, and `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Message catalog: `src/Hexalith.ChatBot.Contracts/Messages/ChatBotMessageCatalog.cs`, `ChatBotMessageCodes.cs`, `ChatBotMessageCatalogVersion.cs`, `ChatBotMessageNextActions.cs`, `ChatBotDisabledActionReasons.cs`, and `ChatBotDetailVisibility.cs`.
- Existing tests to extend: `MessageCatalogContractTests`, `ProjectConversationContractTests`, `OpenApiContractSpineTests`, `ClientGenerationTests`, `ProjectConversationProjectionTests`, `CrossTenantReadSurfaceIsolationTests`, `ProjectConversationServiceTests`, `ProjectConversationStateTests`, `ChatBotLocalizationContractTests`, `ChatBotLiveRegionReducedMotionContractTests`, `ChatBotSemanticTokenContractTests`, `ProjectConversationE2ETests`, and generated-client hash fixture tests.

### Current State To Preserve

- Story 3.1 added `GET /api/v1/projects/{projectId}/conversation`, tenant/project keyed S1 projection storage, cursor pagination, authorized empty reads, safe denial for unauthorized reads, governed UI route/state, and reducers that clear previous project state on load/failure.
- Story 3.2 enriched S1 items with source-email metadata from mailbox intake while keeping raw provider `SourceContext` and email bodies out of the read contract. AI outcome rows must not inherit raw source-email/provider bodies.
- Story 3.3 added participant item rendering and pending materialization by tenant/intake. Participant source-version replacement is independent from AI outcome replacement.
- Story 3.4 added attachment item rendering and pending materialization by tenant/intake. Attachment authorization/redaction and AI-context eligibility metadata must remain separate from AI generated summaries.
- Story 3.5 established append-only association/correction decision items with deterministic `decision:{associationId}:{sourceVersion}` ids. AI outcomes must follow append-only history and link later outcomes instead of mutating earlier rows.
- Story 3.6 added approval event rows with policy snapshot, risk, affected-resource, recipient, command, audit, and outcome metadata. AI outcome rows should link to approval ids/statuses where present but must not replace approval history.
- Story 3.7 added failure/retry/blocked rows, message-catalog-backed safe language, and a dedicated failure component. AI outcome rendering must preserve those fields and route `failure-state` rows unchanged.

### Architecture Guardrails

- Contract Spine remains the source of truth: OpenAPI 3.1 + generated client + parity/contract tests. UI/CLI/MCP bind through `Hexalith.ChatBot.Client`; S1 UI reads through `IChatBotClient` only. [Source: _bmad-output/planning-artifacts/architecture.md#Contract-Spine]
- Keep dependencies flowing `Contracts <- Client <- Server`; UI/CLI/MCP must not reference server governance internals. Governance interfaces stay internal to `.Server`. [Source: _bmad-output/planning-artifacts/architecture.md#Source-Tree]
- Store durable records as metadata-only derived projection rows with tenant id, source provenance, schema version, source version, correlation id, redaction state, and retention class. Do not add direct transcript or provider data storage for S1. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- Event-driven projection handling must be duplicate and out-of-order tolerant because DAPR pub/sub delivery is at-least-once. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Use `System.Text.Json` shared options and existing enum-member converters. Do not add Newtonsoft.Json, inline serializer options, or new serialization libraries.
- Use server-side UTC timestamps and preserve source timestamps at presentation boundaries. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR36]
- Lifecycle-state strings are exact and shared: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, `Correcting`, and `Correction-delayed`. Do not invent synonyms for existing lifecycle fields.

### UX And Accessibility Guardrails

- AI rows should read as governed AI activity, not chat bubbles. Use compact operational layout, not decorative cards or a marketing-style feed.
- Actor type must precede content in the accessible name/description. Recommended accessible label starts with `AI actor`.
- AI-generated content must be explicitly labelled before users encounter the generated text. Source evidence remains evidence metadata/chips and should be programmatically separate.
- Evidence, risk, status, actor, and timestamp must appear in consistent order. Plain-language labels precede raw IDs; IDs remain metadata.
- AI proposal ready uses one polite announcement for the current user's request. Projection pending uses one polite announcement with operation identity. Approval rejection caused by the current user's submitted action is assertive; observed queue/history updates are row-level only. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix]
- If any streaming/proposal generation state is represented, the Stop/Cancel affordance must be keyboard reachable, stable, politely announce cancellation, and return focus to the composer or AI proposal panel. This story should not implement streaming generation unless projection metadata already supplies that state. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Interaction-Primitives]
- Disabled, blocked, redacted, unavailable, invalidated, or pending AI detail must expose a reachable reason through inline text or a focusable explanation. Tooltip-only explanation is not acceptable.
- EN/FR localization is required. Stable machine codes, IDs, lifecycle states, status codes, reason codes, operation IDs, approval IDs, context package IDs, audit operation IDs, and correlation IDs remain untranslated; labels and explanations are translated.

### Project Structure Notes

- Contract DTO and enum changes belong under `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Enums/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server AI outcome projection changes belong under `src/Hexalith.ChatBot.Server/Projections/`.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.7 is the immediate implementation reference for adding a new S1 item kind: additive contract fields, projection event envelope, translator/handler, deterministic item ids, dedicated governed component, localization, CSS, and E2E fixture expansion.
- Story 3.7 review fixed restricted identifier leakage and unsafe free-text metadata tokens. AI outcome translation must constrain optional metadata tokens and must not pass raw prompt/output/provider/tool text through "metadata" strings.
- Story 3.6 established policy/audit visibility rules for approval rows. Reuse that redaction posture for AI policy snapshot and audit operation metadata.
- Story 3.5 established append-only history and correction linkage. AI outcomes should preserve proposal/denial/execution/outcome as separate history rows and link supersession/invalidated-context relationships.
- Story 3.4 review fixed restricted attachment metadata leakage. AI outcome rows must not expose hidden file names, file ids, or context-package contents unless the projection marks references authorized.
- Story 3.3 review fixed raw enum display and actor badge fallback issues. AI outcome labels need localized user-facing labels; machine tokens may appear only in metadata fields.
- Story 3.2 review fixed stale source-email replay and source-version conflation. AI outcome source-version ownership must remain separate from source-email enrichment.
- Story 3.1 review fixed DAPR stale replay handling, cross-project stale UI state, authorized empty conversation reads, and stable rendered item IDs. Preserve those regression targets.
- Prior validation used compiled xUnit v3 executables because VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted contract/client/server/UI/conformance tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Add negative content assertions for raw prompt text, raw model output, provider diagnostics, tool payload/result, raw policy body, raw audit envelope, unauthorized project/file/participant/recipient names, hidden evidence values, hidden diagnostic data, local paths, and tokens in API, UI, fixture, logs/test output where applicable.
- Include ordering/replay tests around execution outcome before proposal, approval link before outcome, failure before outcome, duplicate event delivery, stale replay after current outcome state, correction invalidation after proposal, and projection/source-email/participant/attachment/approval/failure rows arriving before and after AI outcomes.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, DAPR 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints--Dependencies]
- NuGet currently lists newer Fluent UI Blazor v5 prerelease packages than the repo-pinned RC2; keep the repo pin unless a separate dependency-upgrade story is created. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components]
- Microsoft Learn tracks .NET 10 breaking changes separately; do not mix framework migration work into AI outcome rendering. [Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3, Story 3.8, FR22, FR25, FR27, FR28, and cross-cutting UX guidance.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR22, FR24, FR27, FR28, FR39-FR46, NFR9, NFR21, NFR22, NFR36, NFR38-NFR40, NFR60.
- `_bmad-output/planning-artifacts/architecture.md` - Contract Spine, data architecture, projection boundaries, DAPR/event ordering, source tree, and testing standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - conversation stream, AI proposal ready, command/projection pending, live-region behavior, AI/source evidence semantics, keyboard/focus, reduced motion, and stop/cancel guidance.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - governed component tokens: conversation stream, actor badge, evidence chip, risk chip, AI proposal panel, approval panel, blocked state, and status toast/banner.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs` - current S1 item DTO to extend additively.
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs` and `ProjectConversationActorKind.cs` - current item/actor wire tokens to preserve and extend.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - current S1 materialization logic and deterministic item-id helpers to extend.
- `src/Hexalith.ChatBot.Server/Program.cs` - `ToContractItem` mapping chokepoint.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionHandler.cs`, `ApprovalEventView.cs`, `FailureStateProjectionHandler.cs`, and `FailureStateEventView.cs` - latest dedicated projection patterns.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotApprovalConversationItem.razor`, `ChatBotFailureStateConversationItem.razor`, `ChatBotBlockedState.razor`, `ChatBotStatusBanner.razor`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` - current S1 routing, rendering, blocked/status primitives, and UI mapping patterns to extend.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` through `_bmad-output/implementation-artifacts/3-7-failure-retry-and-blocked-state-rendering.md` - prior S1 implementation context, review fixes, and regression targets.
- `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/` - DAPR pub/sub at-least-once delivery semantics.
- `https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components` - current Fluent UI Blazor package stream; repo remains pinned for this story.
- `https://learn.microsoft.com/en-us/dotnet/core/compatibility/10` - .NET 10 breaking change index; no migration work in this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (story authoring); Claude Opus 4.8 (implementation)

### Debug Log References

- Create-story workflow executed 2026-06-01T07:10:57+02:00.
- Source discovery loaded `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, sprint status, Story 3.7, current S1 projection/UI/test files, sibling project-context facts, recent git history, and official DAPR/NuGet/Microsoft technical references.
- Discovery results: loaded `{epics_content}` from 1 file, `{architecture_content}` from 1 file, `{prd_content}` from sharded PRD file, `{ux_content}` from 2 sharded UX files, and `{project_context}` from sibling module project-context files with FrontComposer, Conversations, Folders, and EventStore rules most relevant to this story.
- Dev-story execution 2026-06-01: completed partial contract/server scaffolding left in the working tree, fixed non-compiling nullability in `ProjectConversationItemView.FromAiOutcomeEvent`, regenerated the typed client via NSwag and refreshed `tests/fixtures/hexalith-chatbot-generated-client.sha256` (BOM-stripped UTF-8 hash to match `ClientGenerationTests`).
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> 0 warnings / 0 errors.
- Test runs: Contracts 87, Client 15, Server 270, Conformance 56, UI 91, Architecture 35 all green; UI.E2E `ProjectConversationE2ETests` 7/7 green via the compiled runner against real Chrome (forced-colors + reduced-motion fixtures include AI rows); IntegrationTests 2 passed / 2 skipped (DAPR/Aspire infra unavailable).
- Dev-story verification 2026-06-10: no unchecked story tasks or review follow-ups found; story and sprint status were already `done`.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> passed, 0 warnings / 0 errors.
- `dotnet test Hexalith.ChatBot.slnx --no-build -m:1 /nr:false` -> blocked by local VSTest socket permission (`SocketException (13): Permission denied`); compiled xUnit v3 runners were used.
- Compiled xUnit v3 runners for all 15 ChatBot test assemblies -> 2520 passed / 2 skipped / 0 failed / 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story status set to `ready-for-dev`.
- Sprint status updated for `3-8-ai-outcome-rendering`.
- Contract spine: added `ai-outcome` item kind, `ai-actor` actor kind, additive `AiOutcomeKind`/`AiOutcomeStatus` enums, and 35 additive `ai*` metadata fields on `ProjectConversationItem`; regenerated the typed client (no hand edits) and refreshed the freshness hash fixture.
- Server projection: `AiOutcomeEventView`, `PublishedAiOutcomeEvent`, `AiOutcomeProjectionTranslator`, `AiOutcomeProjectionHandler`; deterministic `ai:{proposal|operation|request}:{kind}:{sourceVersion}` ids; metadata-only token sanitisation, policy-snapshot/audit redaction, duplicate/out-of-order/append-only tolerance; stored through the existing `IProjectConversationProjectionStore` (in-memory + DAPR) and threaded through the `ToContractItem` chokepoint (Story 3.7 failure fields regression-protected).
- Senior developer review auto-fixes: registered and mapped the AI outcome DAPR projection endpoint so real published events reach S1, and changed the AI outcome risk chip to derive from supplied risk-action metadata instead of always rendering `tool-invoking`.
- UI: dedicated `ChatBotAiOutcomeConversationItem.razor` (actor-led accessible name, evidence/risk/status/actor/timestamp ordering, non-color status text, AI-generated vs source-evidence sections kept programmatically distinct); routed from `ChatBotConversationStream`; mapped through `ProjectConversationService`/`ProjectConversationItemModel` (`IChatBotClient` only); EN/FR resources (65 keys) and `chatbot.tokens.css` extended with Fluent tokens only (reduced-motion + forced-colors covered).
- Raw prompt/model-output/provider/tool/policy/audit payloads kept out of contract, UI, fixtures, and tests; negative assertions added across contract, server, and E2E layers.
- Dev-story verification re-run on 2026-06-10 with no implementation changes required; status remains `done`.

### File List

- `_bmad-output/implementation-artifacts/3-8-ai-outcome-rendering.md`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationItemKind.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ProjectConversationActorKind.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeEventView.cs`
- `src/Hexalith.ChatBot.Server/Projections/PublishedAiOutcomeEvent.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionHandler.cs`
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionEndpoints.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.Server/Projections/IProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.Server/Projections/DaprProjectConversationProjectionStore.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fixes.

Findings fixed:

- HIGH: `AiOutcomeProjectionHandler` was implemented and tested directly, but it was not registered in DI or exposed through a DAPR projection endpoint. Real published AI outcome events would not materialize into the S1 conversation. Fixed by adding `AiOutcomeProjectionEndpoints`, mapping it in `Program.cs`, registering the handler, and adding an endpoint replay/idempotency test.
- MEDIUM: `ChatBotAiOutcomeConversationItem` always rendered the risk chip as `ToolInvoking`, ignoring supplied `AiRiskActionClasses`. Fixed by deriving the displayed risk action class from AI outcome metadata and adding a static UI regression assertion.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> passed, 0 warnings / 0 errors.
- `dotnet test ...` attempted for Server/UI/Contracts targeted suites -> blocked by local VSTest socket permission (`SocketException (13): Permission denied`), so compiled xUnit v3 runners were used.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.AiOutcomeProjectionTests` -> 19 passed.
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests` -> 32 passed.
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests` -> 6 passed.
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none` -> 15 passed.
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests -class Hexalith.ChatBot.UI.Tests.ChatBotSemanticTokenContractTests` -> 18 passed.
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none` -> 56 passed.
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -parallel none` -> 35 passed.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` -> 8 passed.

---

Reviewer: Jerome (Claude Opus 4.8) on 2026-06-10 — automated story-automator re-review

Outcome: Approved. No CRITICAL/HIGH/MEDIUM defects found; no code changes required.

Scope: Re-validated all eight acceptance criteria against the committed implementation. Story 3.8 is committed at `9b029ac`, with Story 4.3 enhancements layered on top in the current working tree. Read the full File List: contract DTO/enums, OpenAPI + generated client, server projection (translator/handler/endpoints/view/in-memory + DAPR stores), the `ToContractItem` chokepoint, the UI component/service/state/localization/CSS, and the AI test suites.

Findings:

- AC1-AC8 verified implemented. Actor type leads the accessible name (`AiOutcomeAccessible`); evidence/risk/status/actor/timestamp ordering preserved; status text is non-color; `chatbot.tokens.css` contains zero raw `#`/`rgb(`/`hsl(` literals. All nine `AiOutcomeKind` and ten `AiOutcomeStatus` wire tokens present and stable. Metadata-only projection with token sanitisation (`IsSafeMetadataToken`), policy-snapshot/audit redaction, and append-only/replay-safe ids (`ai:{id}:{kind}:{sourceVersion}`, idempotent upsert keyed on source version). The chokepoint passes through all prior 3.1-3.7 fields alongside the additive `ai*` fields; `failure-state` routing is unchanged.
- Negative-space (AC4) confirmed: no `prompt`/`completion`/`modelOutput`/`providerDiagnostic`/`toolPayload`/`toolResult`/`policyBody`/`auditEnvelope` fields in contract, projection, or UI; `ShouldSuppressUnsafeOptionalTokensAndNeverLeakPromptOrProviderText` asserts suppression of unsafe tokens and local paths.
- Prior-review fixes are present in the code: the AI outcome DAPR projection endpoint is registered and mapped (`MapAiOutcomeProjectionEndpoints` in `Program.cs`), and the risk chip derives its action class from supplied `AiRiskActionClasses`.

Validation (compiled xUnit v3 runners; VSTest sockets blocked in sandbox):

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` -> 0 warnings / 0 errors.
- `Server.Tests` `AiOutcomeProjectionTests` -> 27 passed.
- `Contracts.Tests` `ProjectConversationContractTests` -> 6 passed; `Client.Tests` -> 34 passed.
- `UI.Tests` (`ProjectConversationServiceTests` + `ChatBotLocalizationContractTests` + `ChatBotSemanticTokenContractTests`) -> 26 passed.
- `Conformance.Tests` -> 87 passed; `Architecture.Tests` -> 39 passed.
- `UI.E2E.Tests` `ProjectConversationE2ETests` -> 24 passed (forced-colors + reduced-motion AI rows).

Status unchanged: done.

## Change Log

- 2026-06-01: Implemented Story 3.8 AI outcome rendering across the S1 contract spine, server projection, UI component/state/service/localization/CSS, and contract/server/UI/E2E test coverage. Added `ai-outcome`/`ai-actor` wire tokens, `AiOutcomeKind`/`AiOutcomeStatus` enums, additive `ai*` metadata fields, a dedicated governed AI outcome component with AI-generated vs source-evidence distinction, EN/FR localization, and metadata-only/append-only/replay-safe projection. Status set to `review`.
- 2026-06-01: Senior developer review auto-fixed missing AI outcome projection endpoint/DI registration and AI risk-chip metadata mapping, expanded endpoint/kind/risk regression coverage, and moved story status to `done`.
- 2026-06-10: Re-ran dev-story verification for Story 3.8. No unchecked tasks remained, no implementation changes were required, build passed, and all compiled xUnit v3 ChatBot test assemblies passed with only existing infrastructure skips.
- 2026-06-10: Story-automator adversarial re-review (Claude Opus 4.8). Re-validated all eight acceptance criteria against the committed implementation; build clean (0/0) and all targeted contract/client/server/conformance/UI/architecture/E2E suites green (243 tests across the suites run, including 24 E2E). No CRITICAL/HIGH/MEDIUM findings; no code changes required. Status remains done; sprint-status already synced to done.
