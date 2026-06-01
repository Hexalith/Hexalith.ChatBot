---
baseline_commit: d8f6198
---

# Story 4.6: AI action preview and inspection

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized user,
I want to preview and later inspect the full lifecycle of an AI action,
so that I can make a safe decision and reconstruct what happened.

## Acceptance Criteria

1. Given a proposed AI action, when an authorized user previews it before approval or execution, then the surface shows metadata-only preview sections for outbound communication, file access, command execution, and AI-generated changes, including allowed/blocked status, redaction state, evidence freshness, expected post-state, command name, allowlist version, recipients/destination, and safe next action. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.6; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR45]
2. Given a preview contains file, context, outbound, command, or generated-content detail that is unauthorized, redacted, expired, unavailable, or not yet produced, when the preview renders, then it explains the state with stable reason codes and never exposes raw prompt text, provider payloads, file contents, unauthorized evidence, restricted filenames/paths, raw email bodies, tenant IDs in denial bodies, secrets, or raw exceptions. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR40-NFR48; _bmad-output/planning-artifacts/architecture.md#Implementation-Patterns--Consistency-Rules]
3. Given any AI action lifecycle, when an authorized user inspects it from the project conversation or S3 approval review, then the user can view proposal, approval request, approval decisions, denials/refusals, execution pending/succeeded/failed states, outcome records, failure/retry state, correction invalidation, audit operation/status, correlation ID, source evidence references, policy snapshot, and supersession links as an ordered metadata timeline. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.6; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR44]
4. Given approval, decision, AI outcome, and failure events arrive out of order or are replayed, when projections update the conversation inspection view, then existing append-only rows remain immutable, duplicate deliveries are idempotent, stale replay does not overwrite newer rows, request context enriches later approval/decision rows when available, and inspection still reconstructs the lifecycle by proposal/approval/operation/correlation identifiers. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture; _bmad-output/implementation-artifacts/4-5-approval-gate-and-ai-action-approval-surface-s3.md#Senior-Developer-Review-AI]
5. Given S3 and project conversation render the lifecycle inspection UI, when tested with keyboard, screen reader semantics, reduced motion, forced colors, English/French text, and phone/tablet widths, then preview and inspection sections remain keyboard reachable, expose unique labels, use no hover-only critical detail, preserve focus, do not force-scroll while reading history, announce only current-user relevant updates with the configured politeness, and avoid overlapping text or nested-card layouts. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-to-feedback-matrix; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md#Components]
6. Given inspection or preview data is requested, when the actor lacks project/action/audit authority or dependency state is degraded, then the system fails closed with a redacted blocked state and safe next action instead of confirming hidden resources; read models remain tenant-partitioned and no UI/service code performs authorization, policy, audit, or projection writes directly. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a; _bmad-output/planning-artifacts/architecture.md#Security-and-Privacy-Architecture]
7. Given this story completes, when acceptance coverage runs, then contract, projection, service/model, component, localization, accessibility, leakage, and E2E/browser coverage prove the preview/inspection lifecycle without implementing Story 4.7 allowlisted command execution or dispatching `Project.AppendConversationMessage`. [Source: _bmad-output/planning-artifacts/epics.md#Story-4.7; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Command-Allowlist-v0]

## Tasks / Subtasks

- [x] Define preview/inspection read-model contract extensions without duplicating proposal or approval models (AC: 1, 2, 3, 6, 7)
  - [x] Reuse `ProjectConversationItem`, `ApprovalEventView`, `AiOutcomeEventView`, `ProjectConversationReviewHistoryEntry`, `ProjectConversationItemStatusSummary`, and existing AI/approval enums before adding new public DTOs.
  - [x] Add only additive metadata fields if an existing field cannot safely represent preview status, for example preview section state, preview redaction state, preview unavailable reason, lifecycle group/correlation key, or inspection sequence metadata.
  - [x] Keep public contracts low-dependency and metadata-only. No field may carry raw prompt, completion, provider payload, file content, raw email body, unredacted filename/path, unrestricted policy body, or final authorization claims.
  - [x] If public schema changes, update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and refresh `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- [x] Materialize a lifecycle inspection timeline from existing approval and AI outcome projections (AC: 3, 4, 6)
  - [x] Extend `ProjectConversationItemView.BuildReviewHistory()` or a focused helper so proposal, request, decision, denial/refusal, execution-started, execution-succeeded, execution-failed, outcome-recorded, failure/retry, and corrected-context-invalidated events can be grouped by `proposalId`, `approvalId`, `operationId`, and `correlationId`.
  - [x] Preserve append-only semantics: do not mutate closed approval records or AI outcome rows; supersession uses `Supersedes*` / `SupersededBy*` links.
  - [x] Preserve current request-context enrichment in `ApprovalEventView.WithRequestContext`; if a decision/outcome arrives before request context, show the safe partial row and enrich later without dropping the earlier audit/action metadata.
  - [x] Keep tenant/project partition keys in `IProjectConversationProjectionStore` paths and stores. Reads must not combine lifecycle rows across tenants or projects.
  - [x] For degraded or unauthorized inspection detail, surface reason codes such as `redacted`, `unavailable`, `projection-pending`, `audit-unavailable`, `evidence-expired`, or `not-authorized`; do not infer hidden resource existence.
- [x] Add preview sections to S3/project conversation UI using existing governed components (AC: 1, 2, 5, 6)
  - [x] Extend `ChatBotApprovalConversationItem.razor` and/or a focused child component to render four preview sections: outbound communication, file access/context, command execution, and AI-generated changes.
  - [x] Reuse `ChatBotEvidenceChip`, `ChatBotRiskChip`, `ChatBotBlockedState`, `ChatBotConversationItemStatusSummary`, `ChatBotConversationItemReviewHistory`, and the existing definition-list metadata pattern.
  - [x] Preview sections display state codes and permitted metadata only: recipients/destination refs, affected resource refs, evidence refs/freshness, command/allowlist, policy snapshot visibility, expected post-state redaction, generated-content visibility, audit status, and safe next action.
  - [x] Do not create a new visual design system, nested card layout, modal stack, hover-only preview, or separate chat-like transcript for system decisions.
  - [x] Add EN/FR localization keys for preview section labels, lifecycle inspection labels, redacted/unavailable preview reasons, and audit/projection pending helper text if missing.
- [x] Improve AI outcome inspection for full lifecycle reconstruction (AC: 2, 3, 4, 5)
  - [x] Extend `ChatBotAiOutcomeConversationItem.razor` only as needed to show lifecycle grouping, source evidence, AI generated-content visibility, command/approval/execution/audit metadata, failure/retry, supersession, and review history in a scannable order.
  - [x] Keep generated content separate from source evidence and label it as governed AI output. If generated detail is unavailable, show metadata-only reason text rather than attempting to render provider output.
  - [x] Ensure `AiOutcomeKind` coverage remains complete for `proposal`, `denial`, `refusal`, `approval-linked`, `execution-started`, `execution-succeeded`, `execution-failed`, `outcome-recorded`, and `corrected-context-invalidated`.
  - [x] Link or reference corresponding approval rows using IDs only; do not perform client-side joins that require unauthorized read detail.
- [x] Preserve accessibility, responsive behavior, and localization (AC: 5)
  - [x] Preview/inspection regions have unique accessible names and remain keyboard reachable.
  - [x] Disabled/unavailable preview details use reachable inline explanation or `aria-disabled="true"` with an announced reason; tooltip-only explanation is not acceptable.
  - [x] Current user's new proposal/command pending updates announce once politely; current user's rejected/blocked action announces assertively; historical rows do not announce on initial load.
  - [x] Reduced motion disables nonessential transitions; no forced scroll while reading lifecycle history. Provide a keyboard-reachable new-updates affordance if needed.
  - [x] Verify English and French labels fit at mobile widths and in forced-colors; status meaning must survive via text/icon/border, not color alone.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract/OpenAPI/generated-client tests for any added preview/inspection fields, enum wire tokens, metadata-only redaction states, and generated-client hash.
  - [x] Projection tests for proposal/request/decision/outcome/failure lifecycle grouping, out-of-order delivery, duplicate replay, stale replay, request-context enrichment, tenant isolation, and safe partial rendering.
  - [x] UI service/model tests proving preview/inspection metadata maps from generated contracts without parsing raw source content in the browser.
  - [x] Component/bUnit or equivalent tests proving preview sections, lifecycle timeline, disabled/unavailable reasons, review history, focus/live region attributes, EN/FR text, and no sensitive values render.
  - [x] E2E/Playwright coverage if component tests cannot prove responsive/no-overlap/forced-colors behavior for the new preview sections. Use accessible roles/labels or stable `data-chatbot-*` attributes, not CSS-only selectors or sleeps.
  - [x] Leakage/isolation tests proving prompt, provider payload, generated content without explicit redaction approval, file contents/paths, unauthorized evidence, raw email body, tenant IDs in denial bodies, secrets, raw exceptions, and restricted audit/policy details do not appear in API responses, projections, UI rows, logs, fixtures, or support artifacts.

## Dev Notes

### Scope Boundaries

- This story owns FR44/FR45 for M0: preview before approval/execution and inspection of the AI action lifecycle through metadata-only project conversation/S3 surfaces.
- This story may add preview/inspection metadata fields, projection grouping helpers, UI child components, localization, and tests.
- This story must not implement Story 4.7 allowlisted command execution, dispatch `Project.AppendConversationMessage`, invoke M365 outbound send, invoke Folders content APIs for raw file content, call an AI provider for generated output, build CLI/MCP parity, create tenant policy editor UI, or add a new command bypassing the existing gateway.
- Approval remains a gate; inspection is a read/projection concern. If this story needs a write, it must go through the existing `CommandGateway` spine and have explicit AC coverage. No write is currently required by the story.

### Existing Code To Reuse

- Approval and S3 projection/UI:
  - `src/Hexalith.ChatBot.Contracts/Commands/DecideAiActionApproval.cs`
  - `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
  - `src/Hexalith.ChatBot.Server/Governance/AiMediation/AiActionApprovalEvents.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedApprovalEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/PublishedAiActionApprovalEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs`
  - `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
  - `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
  - `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- AI outcome projection/UI:
  - `src/Hexalith.ChatBot.Server/Projections/PublishedAiOutcomeEvent.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs`
  - `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionHandler.cs`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
  - `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeKind.cs`
  - `src/Hexalith.ChatBot.Contracts/Enums/AiOutcomeStatus.cs`
- Shared UI primitives:
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEvidenceChip.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotRiskChip.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor`
  - `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor`
  - `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
  - `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
  - `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
  - `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`

### Current State To Preserve

- Story 4.5 created durable `AiActionApprovalRequested` / `AiActionApprovalDecisionRecorded` events and fixed chatbot-domain approval events flowing into S3/project conversation projections. Do not regress that projection ingestion.
- `ApprovalEventView.WithRequestContext` supports out-of-order request/decision enrichment. Keep decision metadata and later request context additive.
- `ProjectConversationItemView` already includes approval, AI outcome, status summary, classification, detected intent, review history, and metadata-only redaction fields. Extend these before creating parallel read models.
- `ChatBotApprovalConversationItem.razor` already renders FR42 approval metadata, evidence freshness chips, approval decision controls, disabled approve reason, audit/policy unavailable helper text, and review history.
- `ChatBotAiOutcomeConversationItem.razor` already renders governed AI outcome metadata, source evidence, generated-content visibility, metadata-only reason text, audit unavailable helper text, and review history.
- `AiOutcomeProjectionTests` already cover append-only projection rows, out-of-order execution/proposal arrival, all AI outcome kinds, low-risk execution rows, policy-false approval-linked rows, idempotent duplicate delivery, and stale replay. Build on this coverage.
- Existing worktree has unrelated modified `Hexalith.Tenants` submodule state and `_bmad-output/story-automator/orchestration-4-20260601-145742.md`; do not revert or include them.

### Architecture Guardrails

- Dependency direction remains `Contracts <- Client <- UI/Server`. Public preview/inspection contract additions live in `.Contracts`; projection and grouping logic live in `.Server`; UI consumes generated client contracts and service models.
- Do not let UI components or services authorize, classify risk, read raw audit details, evaluate policy, or write projections. UI renders server-projected metadata and submits only existing decision commands when applicable.
- Tenant ID and actor identity come from authenticated server context and EventStore envelopes, not request bodies or client projection models.
- Read models are derived records and must carry tenant/provenance/redaction/retention/schema/source-version where applicable.
- Aggregate handlers remain pure. This story should not add I/O, Dapr, authorization, policy, logging, or async to `GovernedOperationAggregate`.
- Rejections, approvals, denials, failures, and outcomes are append-only history. Closed records are superseded, not mutated.
- Use repo-pinned stack only: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, Fluent UI v5 RC via FrontComposer, xUnit v3, Shouldly, NSubstitute, Playwright where needed. Do not add package versions inline or upgrade dependencies.
- Root submodule policy applies: initialize/update only root `.gitmodules` submodules; never use recursive submodule commands.

### Testing Notes

- Minimum validation before dev handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none`
  - `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none`
- Sandbox note inherited from prior stories: `dotnet test` through VSTest can fail with `SocketException (13): Permission denied`; prefer compiled in-process xUnit v3 runners after build.
- Add Playwright only for behavior not provable by component/service tests, especially responsive no-overlap, forced-colors, reduced-motion, and real browser focus behavior.

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`; Epic 4 is "Governed AI Action Mediation" and Story 4.6 sits between S3 approval gate (4.5) and allowlisted command execution (4.7).
- Loaded PRD detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, especially FR44, FR45, FR81a, NFR40, NFR46-NFR48, NFR50, NFR60, NFR65, and Functional Acceptance Guidance for FR39-FR46.
- Loaded addendum detail from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md`, especially Command Allowlist v0/v1, Shared Command Pipeline, Tenant Policy Schema, and Idempotency Keys.
- Loaded architecture from `_bmad-output/planning-artifacts/architecture.md`, especially Contract Spine, CommandGateway order, governed AI mediation, fail-closed/audit/idempotency guardrails, tenant isolation, project structure, and testing standards.
- Loaded UX detail from `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` and `EXPERIENCE.md`, especially AI proposal panel, approval panel, audit timeline, evidence drawer/chips, state-to-feedback matrix, focus/live-region rules, and mobile/forced-colors requirements.
- Loaded persistent project-context facts from sibling `project-context.md` files. Relevant rules: .NET SDK `10.0.300`, `net10.0`, warnings-as-errors, central package management, metadata-only diagnostics, tenant isolation, pure EventStore aggregates, Dapr duplicate/order tolerance, FrontComposer/Fluent UI inheritance, and root-level submodules only.
- Loaded previous-story intelligence from `_bmad-output/implementation-artifacts/4-5-approval-gate-and-ai-action-approval-surface-s3.md`; the key review learning is that durable aggregate approval events must be wired into S3/project-conversation projection paths, not only synthetic approval events.
- Inspected current code and tests for likely update surfaces: `ProjectConversationItem`, `ApprovalEventView`, `ApprovalProjectionTranslator`, `ProjectConversationItemView`, `AiOutcomeProjectionTranslator`, `PublishedAiOutcomeEvent`, `ChatBotApprovalConversationItem`, `ChatBotAiOutcomeConversationItem`, `ChatBotConversationItemReviewHistory`, `ProjectConversationService`, `ProjectConversationModels`, `ProjectConversationProjectionTests`, `AiOutcomeProjectionTests`, `ProjectConversationServiceTests`, localization files, and UI E2E coverage.
- Latest-technology web research not required for story creation: this story selects no new external service, package, or framework. Implementation should use repo-pinned versions and local patterns.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 4 and Story 4.6 acceptance criteria.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR44, FR45, FR81a, NFR40, NFR46-NFR48, NFR50, NFR60, NFR65.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md` - Command Allowlist, Shared Command Pipeline, Tenant Policy Schema, Idempotency Keys.
- `_bmad-output/planning-artifacts/architecture.md` - Contract Spine, governed AI mediation, project structure, fail-closed/audit/idempotency/testing guardrails.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - AI proposal panel, approval panel, evidence drawer, audit timeline visual component rules.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - AI Action Review, state-to-feedback matrix, focus/live-region behavior, interaction constraints, audit semantics.
- `_bmad-output/implementation-artifacts/4-5-approval-gate-and-ai-action-approval-surface-s3.md` - previous story scope, implementation notes, validation evidence, and review auto-fix learning.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs` - current read-model contract to extend.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalEventView.cs` - approval projection context enrichment.
- `src/Hexalith.ChatBot.Server/Projections/ApprovalProjectionTranslator.cs` - synthetic and chatbot-domain approval event translation.
- `src/Hexalith.ChatBot.Server/Projections/AiOutcomeProjectionTranslator.cs` - AI outcome projection translation and metadata filtering.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` - conversation item materialization, status summary, classification, detected intent, review history.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor` - existing S3 row to extend for preview.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor` - existing AI lifecycle/outcome row to extend for inspection.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemReviewHistory.razor` - existing metadata timeline primitive.
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs` - existing AI outcome lifecycle projection coverage.
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` - approval projection and conversation read-model coverage.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Red/green: added failing UI localization/source contract for `ChatBotAiActionPreviewSections.razor`, then implemented the component and EN/FR resources.
- Red/green: added failing AI outcome lifecycle review-history test for operation/correlation reconstruction, then changed AI outcome review history to prefer `AiOperationId` over audit operation ID.
- Validation executed 2026-06-01: solution build, Contracts.Tests, Client.Tests, Server.Tests, UI.Tests, Architecture.Tests, Conformance.Tests, and UI.E2E.Tests all passed with in-process xUnit runners.

### Completion Notes List

- Reused existing project conversation, approval, AI outcome, status summary, and review-history contracts; no public schema or generated client changes were required.
- Added a governed metadata-only AI action preview component with outbound, file/context, command, and generated-change sections, each with stable state/reason/redaction metadata and keyboard-reachable `aria-disabled` handling.
- Wired preview sections into approval and AI outcome conversation items while preserving existing evidence, risk, status summary, generated-content separation, audit visibility, and review history.
- Improved lifecycle inspection history so AI outcome rows carry the AI operation identifier plus proposal/correlation identifiers for reconstruction without exposing restricted audit detail.
- Added EN/FR preview localization, focused projection and localization/component-source coverage, and verified existing leakage/isolation, accessibility, responsive, and E2E suites remain green.

### File List

- `_bmad-output/implementation-artifacts/4-6-ai-action-preview-and-inspection.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiActionPreviewSections.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/AiOutcomeProjectionTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`

### Change Log

- 2026-06-01: Implemented metadata-only AI action preview sections and lifecycle inspection operation-id reconstruction; added localization and focused validation coverage.
- 2026-06-01: Senior review auto-fixed preview unavailable-state handling and forced-colors/focus styling coverage; synced story and sprint status to done.

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approve after auto-fix. No CRITICAL issues remain.

### Findings Fixed

- HIGH: Preview generated-content states such as `not-yet-produced` rendered as `allowed` / `available` because the preview helper did not treat that stable state as unavailable. Fixed `ChatBotAiActionPreviewSections.razor` so `not-yet-produced` remains a blocked reason code instead of being normalized to available.
- MEDIUM: The new keyboard-focusable preview reason and section elements were missing from the forced-colors and focus-outline selector sets in `chatbot.tokens.css`. Fixed the selectors and added regression checks.
- MEDIUM: The story File List omitted actual changed validation artifacts (`ProjectConversationE2ETests.cs` and `tests/test-summary.md`). Updated the File List for traceability.

### Validation

- MCP resource discovery was attempted during review and returned no resources; no external package/API documentation was needed because the fix used repo-pinned local UI/CSS/test patterns.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` - passed.
- `./tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -parallel none` - 97 passed.
- `./tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -parallel none` - 15 passed.
- `./tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -parallel none` - 416 passed.
- `./tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -parallel none` - 97 passed.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -parallel none` - 35 passed.
- `./tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -parallel none` - 58 passed.
- `./tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -parallel none` - 48 passed.
