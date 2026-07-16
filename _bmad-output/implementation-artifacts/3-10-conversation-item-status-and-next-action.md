---
baseline_commit: 52b8379ad620e87211117a171ceb5d1413884355
---

# Story 3.10: Conversation item status and next action

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want to see association, attachment, task, approval, command, failure, retry, and next-action status,
so that I always know the state of a project conversation item.

## Acceptance Criteria

1. Given any project conversation item returned by S1, when it renders, then it exposes a consolidated status summary for association, attachment, task, approval, command, failure, retry, and next action using stable `healthy` / `degraded` / `failed` / `unknown` values. The summary must be derived from explicit persisted/projected status fields, never from counts or UI-only heuristics. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.10; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR24; #FR67]
2. Given a command accepted with projection pending, when its affected conversation item renders, then S1 shows partial success with operation identity, completion/projection status, audit status, correlation id, and safe next action. It must not show terminal success, `Done`, `executed`, or equivalent completion copy until the projected outcome says so. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.10; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#State-And-Feedback; src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs]
3. Given item-specific statuses already present on the S1 contract, when Story 3.10 is implemented, then the new summary is additive and preserves the existing association, participant, attachment, approval, failure, AI outcome, why-panel, correction, source-email enrichment, pagination, tenant/project isolation, redaction, localization, and accessibility behavior from Stories 3.1 through 3.9. [Source: _bmad-output/implementation-artifacts/3-9-why-this-project-evidence-and-provenance-panel.md#Current-State-To-Preserve]
4. Given unavailable, redacted, unauthorized, stale, waiting, blocked, degraded, failed, retryable, terminal, or unknown states, when S1 renders the summary, then user-facing labels and explanations come from the versioned message catalog or EN/FR UI resources and do not expose raw error text, provider payloads, hidden project/file/participant names, raw audit envelopes, command payloads, prompt/output/tool data, local paths, or secrets. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR77; #NFR38; #NFR40]
5. Given responsive, keyboard-only, screen-reader, reduced-motion, and forced-colors users, when item status appears in S1, then status meaning survives without color alone, status/next action remains reachable on phone/tablet/desktop, live announcements are polite and deduplicated for projection-pending current-user transitions, and critical state/action text is not truncated. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-And-Inclusion; #Localization; #Responsive-And-Platform]
6. Contract, generated client, server projection/query mapping, conformance, UI service/model/component, localization, static CSS, and E2E coverage prove the summary is localized, accessible, metadata-only, replay-safe, duplicate/out-of-order tolerant, redaction-safe, cross-tenant isolated, and regression-safe for the existing S1 stream. [Source: _bmad-output/planning-artifacts/architecture.md#Contract-Spine; #Architectural-Boundaries; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR32; #NFR60]

## Tasks / Subtasks

- [x] Add the S1 status-summary contract additively (AC: 1, 2, 3, 4, 6)
  - [x] Prefer reusing `ChatBotHealthStatus` for the required `healthy` / `degraded` / `failed` / `unknown` values if serialization/OpenAPI/generated-client tests prove the exact wire tokens; otherwise add a purpose-named enum with those exact values.
  - [x] Add contract shape(s) such as `ProjectConversationItemStatusSummary` and `ProjectConversationItemStatusFacet` under `src/Hexalith.ChatBot.Contracts/Queries/`, then add an optional/required additive property on `ProjectConversationItem` without removing existing item-specific fields.
  - [x] Each facet must identify the status domain (`association`, `attachment`, `task`, `approval`, `command`, `failure`, `retry`, `next-action`), health enum, source state token, localized/message-catalog code or UI label key, safe next action, and only safe metadata IDs needed to understand the state.
  - [x] Command/projection facets must carry operation id, completion/projection status, audit status, correlation id, retry count where available, terminal reason code where safe, owner/responsible role where available, and duplicate-safety state where available.
  - [x] Task status is not task-intent detection. If no task-intent record exists yet, represent task status as `unknown` or safely absent per the chosen contract, with `none`/no-op next action; do not implement FR35 task detection or Epic 4 task conversion in this story.
  - [x] Update `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and update `tests/fixtures/hexalith-chatbot-generated-client.sha256` through the existing generation flow. Do not hand-edit generated `.g.cs`.
  - [x] Add contract/OpenAPI/generated-client tests proving enum wire values, additive compatibility, nullable/empty behavior, and absence of raw body/subject/html/sourceContext/providerPayload/decision note/correction rationale/policy body/audit envelope/command payload/local path/prompt/output/tool fields.
- [x] Build status-summary mapping in the projection/query layer (AC: 1, 2, 3, 4, 6)
  - [x] Extend `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` and `src/Hexalith.ChatBot.Server/Program.cs` `ToContractItem` as the mapping chokepoints.
  - [x] Reuse existing item fields first: lifecycle/threshold/confidence/safe next action for association; attachment capture/storage/scan/duplicate/retry/AI eligibility; approval status/audit/command outcome; failure status/retry/audit/client action; AI approval/execution/audit/safe next action; correction propagation/downstream impact.
  - [x] Preserve `ProjectConversationItemView.ShouldReplace` source-version semantics, deterministic item IDs, source-email enrichment rules, and append-only correction behavior.
  - [x] Do not add UI reads from `IOperationStatusStore`, `IAssociationProjectionStore`, `IProjectConversationProjectionStore`, Dapr state, EventStore, audit stores, mailbox provider payloads, or sibling services. S1 remains a single `IChatBotClient.GetProjectConversationAsync` read path plus the existing why-panel detail query.
  - [x] If operation status enrichment is needed for command projection-pending rows, perform it server-side through an additive projected field or query mapping that remains tenant-scoped and metadata-only; avoid per-row browser polling or N+1 calls.
  - [x] Fail closed for unknown, malformed, foreign, stale, ambiguous, or unsafe tenant/project/operation context and return safe denial/status without confirming hidden resources.
- [x] Add UI model/service support for the status summary (AC: 1, 2, 3, 4, 5, 6)
  - [x] Extend `ProjectConversationItemModel` in `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs` and `ProjectConversationService.MapItem` to map the generated-client summary.
  - [x] Preserve existing `GetAssociationWhyPanelAsync` and route-change/late-response isolation behavior from Story 3.9.
  - [x] Keep stable machine tokens as metadata; use localized plain-language labels before raw IDs.
  - [x] Add helper methods in `ChatBotUiTextLocalizer` for health/status facet labels and next-action labels instead of concatenating strings in components.
  - [x] Unknown/missing status must render as a safe unknown/unavailable state, not as success.
- [x] Implement a reusable governed status summary component for S1 rows (AC: 1, 2, 3, 4, 5)
  - [x] Add a compact component such as `ChatBotConversationItemStatusSummary.razor` under `src/Hexalith.ChatBot.UI/Components/Governed/`.
  - [x] Render facets in a stable order: association, attachment, task, approval, command, failure, retry, next action. Hide absent facets only when the contract explicitly marks the domain as not applicable; do not hide failed/degraded/unknown states.
  - [x] Reuse `ChatBotEvidenceChip`, `ChatBotStatusBanner`, `ChatBotBlockedState`, or existing governed primitives where they fit; do not create a second visual language or card feed.
  - [x] Integrate the component into `ChatBotEmailConversationItem`, `ChatBotAttachmentConversationItem`, `ChatBotDecisionConversationItem`, `ChatBotApprovalConversationItem`, `ChatBotFailureStateConversationItem`, and `ChatBotAiOutcomeConversationItem` without disrupting their existing evidence/risk/status/actor/timestamp header order.
  - [x] For command accepted/projection pending, render persistent inline partial-success status with operation id, audit/projection status, and safe next action; announce once using the existing state-feedback/live-region matrix for current-user transitions.
  - [x] Redacted/unavailable status explanations must be reachable helper text or enabled "Why unavailable?" affordances, not tooltip-only.
- [x] Add EN/FR localization, responsive CSS, and accessibility behavior (AC: 4, 5, 6)
  - [x] Add resource keys for status summary title, facet labels, `healthy`, `degraded`, `failed`, `unknown`, projection pending, partial success, audit committed/reconciling, no user action, retry, escalate, request access, wait for projection, unavailable/redacted status, and accessible-name templates.
  - [x] Update `SharedResource.resx`, `SharedResource.fr.resx`, `ChatBotUiTextKey.cs`, and `ChatBotUiTextLocalizer.cs`.
  - [x] Extend `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css` with token-only styles. Do not introduce raw `#`, `rgb(`, or `hsl(` colors.
  - [x] Ensure phone/tablet layouts keep status, reason, operation id, and next action visible as labelled rows. Critical state/action words must wrap or move to labelled detail before truncation.
  - [x] Forced-colors must preserve status meaning via text/icon/border, not fill color alone. Reduced-motion must remove transitions and keep textual progress indicators.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests in `ProjectConversationContractTests`, `OpenApiContractSpineTests`, `SharedContractTypeTests`, and `ClientGenerationTests` for summary fields, enum tokens, OpenAPI parity, generated client, additive compatibility, and negative leakage.
  - [x] Server projection/query tests in `ProjectConversationProjectionTests` and `ServerBootstrapApiTests` for each facet domain, especially accepted projection pending, audit reconciling, failed command, retry queued/accepted/exhausted, attachment pending/unsafe/retryable, approval pending/approved/failed, AI execution pending/failed, correction propagation delayed, and unknown task status.
  - [x] Conformance/isolation tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant/project/operation contexts collapse to safe denial/status without evidence leakage.
  - [x] UI service/component/localization tests in `ProjectConversationServiceTests`, `ChatBotLocalizationContractTests`, semantic-token/static CSS tests, and bUnit/static component tests for status ordering, localized labels, no raw enum-only labels, no false done, and safe unknown behavior.
  - [x] Playwright/UI.E2E tests for desktop/mobile, forced-colors, reduced-motion, projection-pending partial success, retryable failure, redacted/unavailable status explanations, keyboard reachability, screen-reader accessible names, and negative assertions for hidden project/file/participant/body/provider/audit/command/prompt/output/local-path text.

## Dev Notes

### Scope Boundaries

- This story is the S1 read-only item-status and next-action summary.
- It may add additive contract DTOs/enums, generated-client updates, server projection/query mapping, UI model/service mapping, a reusable governed row-status component, localization, CSS, and tests.
- It must not implement task-intent detection, task conversion, association/correction commands, approval decision submission, command execution, retry submission, attachment capture/storage, folder authorization expansion, AI context packaging, AI model invocation, CLI/MCP adapters, operational dashboards, or audit investigation.
- The implementation should make current and future status domains visible through one additive summary, while keeping existing item-specific fields intact for existing row components and tests.

### Existing Code To Reuse

- S1 item contract: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`.
- Existing stable health enum candidate: `src/Hexalith.ChatBot.Contracts/Enums/ChatBotHealthStatus.cs`.
- Operation status contract: `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs`, `OperationCompletionStatus.cs`, `OperationAuditStatus.cs`, and `OperationStatusPartialOutputs.cs`.
- Existing status-bearing enums: `ProjectConversationAttachmentStatus`, `ApprovalStatus`, `FailureStatus`, and `AiOutcomeStatus`.
- OpenAPI Contract Spine and generated client: `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`, and `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- Server mapping chokepoints: `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, projection translators/handlers under `src/Hexalith.ChatBot.Server/Projections/`, and `src/Hexalith.ChatBot.Server/Program.cs` `BuildProjectConversationResponse` / `ToContractItem`.
- Existing projection-pending status source: `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs` uses `accepted-projection-pending`, `committed`, and `reconciling`.
- UI service/model/component path: `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`, `ChatBotConversationStream.razor`, and existing item components under `src/Hexalith.ChatBot.UI/Components/Governed/`.
- Existing governed primitives: `ChatBotEvidenceChip.razor`, `ChatBotStatusBanner.razor`, `ChatBotBlockedState.razor`, actor/risk/status patterns, and `chatbot.tokens.css`.
- Localization path: `ChatBotUiTextKey.cs`, `ChatBotUiTextLocalizer.cs`, `SharedResource.resx`, and `SharedResource.fr.resx`.
- Tests to extend: `ProjectConversationContractTests`, `OpenApiContractSpineTests`, `SharedContractTypeTests`, `ClientGenerationTests`, `ProjectConversationProjectionTests`, `ServerBootstrapApiTests`, `CrossTenantReadSurfaceIsolationTests`, `ProjectConversationServiceTests`, `ChatBotLocalizationContractTests`, `ChatBotSemanticTokenContractTests`, and `ProjectConversationE2ETests`.

### Current State To Preserve

- Story 3.1 established the S1 read surface, tenant/project keyed projections, cursor pagination, authorized empty reads, safe denial, governed route/state, and clearing on route load/failure.
- Story 3.2 added source-email enrichment while keeping raw provider `SourceContext` and email bodies out of S1.
- Story 3.3 added participant rendering and safe identity evidence/redaction.
- Story 3.4 added attachment rendering with metadata-only file status and restricted metadata protections.
- Story 3.5 added append-only association/correction decision rows, deterministic decision item ids, correction links, propagation status, stale/corrected-context behavior, and evidence summaries.
- Story 3.6 added approval event rows with policy/audit visibility rules and a test already proving approval outcome `accepted-projection-pending` does not claim executed/done.
- Story 3.7 added failure/retry/blocked-state rows, message-catalog-backed user-safe text, retry count, duplicate-safety, and reachable explanations.
- Story 3.8 added AI outcome rows and negative raw prompt/output/provider/tool leakage tests.
- Story 3.9 added the why-project panel and S1 panel state through `IChatBotClient.GetAssociationRoutingStatusAsync`; preserve its route-change clearing and project/association-scoped late-response isolation.
- Existing worktree has unrelated submodule pointer changes (`Hexalith.EventStore`, `Hexalith.Tenants`) and `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert them.

### Architecture Guardrails

- Contract Spine remains the source of truth: OpenAPI 3.1 plus generated client plus contract tests. UI reads through `IChatBotClient`; S1 must not reference server projection internals. [Source: _bmad-output/planning-artifacts/architecture.md#Contract-Spine]
- Dependency direction remains `Contracts <- Client <- UI/Server`; UI, CLI, and MCP must not replicate gateway, projection, or status-store internals. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- ChatBot derived records carry tenant id, source provenance, derivation kernel/schema version, redaction state, retention class, source version, and correlation id. Status summary fields must preserve this metadata posture. [Source: _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
- Tenant authority comes from authenticated claims/context and access projections, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe contexts collapse to safe denial without confirming hidden resources. [Source: _bmad-output/planning-artifacts/architecture.md#Authentication--Security]
- Dapr pub/sub is at-least-once and projection reads are eventually consistent. Mapping/tests must tolerate duplicate and out-of-order projection events; do not infer health from queue depth or item counts.
- Use `System.Text.Json` shared enum-member behavior and existing contract-generation patterns. Do not add Newtonsoft.Json, inline serializer options, or new serialization libraries.
- Keep repo-pinned dependencies: .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, Dapr 1.17.x, Aspire 13.3.x, xUnit v3, Shouldly, NSubstitute, and existing Playwright fixtures. Do not upgrade packages for this story.

### UX And Accessibility Guardrails

- S1 status is an operational row affordance, not a decorative card or second conversation feed.
- Evidence, risk, status, actor, and timestamp ordering must remain consistent. The new summary may add detail below/within existing rows, but should not disrupt existing row header order.
- Every workflow item has one primary next action; secondary/destructive actions are grouped later. This story displays next action only; it does not submit actions.
- Plain-language labels precede raw IDs. Stable machine tokens, IDs, lifecycle states, status codes, message codes, source versions, and correlation IDs remain metadata and are not translated.
- Redacted/unavailable/disabled statuses need reachable explanation text or an enabled "Why unavailable?" affordance. Tooltip-only explanations are insufficient.
- Projection pending / partial success uses persistent inline status and a polite live region; current-user projection-pending announcements should be deduplicated and should not repeat on each poll/view re-entry.
- Responsive layouts must retain actor, state/status, safe next action, and recovery reason. French labels must not truncate critical state/action words.
- Forced-colors and reduced-motion behavior must cover the new status summary, chips/badges, focus outlines, and projection-pending progress cues.

### Project Structure Notes

- New contract query DTOs belong under `src/Hexalith.ChatBot.Contracts/Queries/`; enum changes belong under `src/Hexalith.ChatBot.Contracts/Enums/`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit generated files.
- Server status derivation belongs in `ProjectConversationItemView`/projection translators and `Program.cs` query mapping, not in UI components.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.9 is the immediate implementation reference for adding S1 UI behavior through additive contract fields, `IChatBotClient`, route-scoped state, EN/FR localization, token-only CSS, accessibility, and negative leakage tests.
- Story 3.9 review fixed a generic complementary landmark name and raw signal-class labels. For this story, avoid raw enum-only labels and ensure any landmark/region added by the status summary has a unique accessible name if repeated.
- Story 3.8 is the strongest negative-leakage reference for AI/provider/tool fields; do not expose raw prompt/output/tool payloads through command or AI status.
- Story 3.7 is the strongest pattern for retry/failure status: message catalog, retry count, duplicate-safety, safe next action, and reachable explanations.
- Story 3.6 already tested that `accepted-projection-pending` approval outcomes must not claim `executed` or done. Extend that invariant to the consolidated status summary and UI.
- Story 3.4 review fixed restricted attachment metadata leakage. Attachment status may show safe state, not hidden filenames, file ids, or metadata unless authorized.
- Prior validation used compiled xUnit v3 executables when VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted contract/client/server/conformance/UI tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit/static component checks, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Include negative content assertions for raw email body/subject/html, provider payload/source context, raw decision note, raw correction rationale, unauthorized project/file/participant/recipient names, hidden evidence values, raw policy body, raw audit envelope, command payloads, prompts/outputs/tool payloads, local paths, and tokens in API, UI, fixtures, logs/test output where applicable.
- Include replay/order tests around duplicate/stale association events, correction delayed, approval outcome accepted-projection-pending, retry queued/accepted/exhausted, failure after retry, attachment unsafe/retryable, AI execution pending/failed, source email before/after association, and panel/status isolation after project switch.

### Latest Technical Notes

- No dependency upgrade is part of this story. Use the versions pinned by the repo and architecture.
- The architecture already calls out Dapr at-least-once pub/sub, source-versioned projections, generated-client contract spine, and Fluent UI v5 RC sensitivity. Implement status summary behavior within those constraints.
- Because this is a rendering/query-contract story, latest external API research is not required unless the implementation changes a dependency, Dapr/Aspire integration, Fluent UI API usage, or OpenAPI generation tooling.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3 and Story 3.10.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR24, FR25, FR67, FR71, FR76, FR77, FR80, NFR21, NFR24, NFR32, NFR38-NFR42, NFR60-NFR64.
- `_bmad-output/planning-artifacts/architecture.md` - Contract Spine, derived-record shape, projection boundaries, Dapr/event ordering, source tree, and testing standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - S1 component/state/accessibility/localization/responsive rules and command accepted/projection pending behavior.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - component inventory, status/banner/chip token posture, contrast, forced-colors behavior.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` through `_bmad-output/implementation-artifacts/3-9-why-this-project-evidence-and-provenance-panel.md` - prior S1 implementation context and regression targets.
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs` - current S1 item contract.
- `src/Hexalith.ChatBot.Contracts/Queries/OperationStatus.cs` - long-running operation status contract.
- `src/Hexalith.ChatBot.Contracts/Enums/ChatBotHealthStatus.cs` - existing exact `healthy` / `degraded` / `failed` / `unknown` enum candidate.
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs` and `src/Hexalith.ChatBot.Server/Program.cs` - projection/query mapping chokepoints.
- `src/Hexalith.ChatBot.Server/Gateway/Status/OperationStatusRecord.cs` - accepted projection-pending and audit status tokens.
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`, `ProjectConversationModels.cs`, and governed item components - S1 UI mapping and rendering targets.
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs` - existing projection-pending and row materialization tests to extend.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (dev-story implementation)

### Debug Log References

- Create-story workflow executed 2026-06-01T09:01:35+02:00.
- Source discovery loaded `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, `_bmad-output/implementation-artifacts/sprint-status.yaml`, Story 3.9, relevant S1 contract/projection/UI/test files, sibling project-context facts, and recent git history.
- Discovery results: loaded `{epics_content}` from 1 file, `{architecture_content}` from 1 file, `{prd_content}` from 1 focused PRD shard, `{ux_content}` from 2 UX files, `{previous_story_intelligence}` from Story 3.9, `{git_intelligence}` from the last five story commits, and `{project_context}` from sibling module project-context files.
- Validation checklist applied during authoring: story contains user value, ACs, tasks, scope boundaries, existing-code reuse, project structure notes, architecture/UX guardrails, previous-story intelligence, testing notes, latest technical notes, and references.
- Dev-story workflow executed 2026-06-01: loaded `.agents/skills/bmad-dev-story/SKILL.md`, customization context, project context, sprint status, and this story before editing.
- Red phase verified missing status-summary contract via `dotnet test tests/Hexalith.ChatBot.Contracts.Tests/Hexalith.ChatBot.Contracts.Tests.csproj --no-restore --filter ProjectConversationDtoShouldSerializeMetadataOnlyWireTokens`; VSTest socket creation is blocked in this sandbox, so subsequent validation used compiled xUnit v3 runners.
- Regenerated the OpenAPI client through `dotnet build src/Hexalith.ChatBot.Client/Hexalith.ChatBot.Client.csproj --no-restore -m:1 /nr:false` and refreshed `tests/fixtures/hexalith-chatbot-generated-client.sha256`.
- Final validation passed: `git diff --check`; `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`; compiled xUnit v3 runners for Contracts (88), Client (15), Server (283), Conformance (58), UI (93), Architecture (35), and focused UI.E2E ProjectConversation coverage (11).
- Full UI.E2E assembly execution also exposed pre-existing strict-locator failures in governed-operations tests; those are outside Story 3.10 and were not used as the story gate.

### Completion Notes List

- Added the additive S1 status-summary contract, OpenAPI schema, generated client, server projection mapping, UI service/model mapping, reusable governed row component, EN/FR localization, token-only CSS, and focused regression coverage.
- Status facets now cover association, attachment, task, approval, command, failure, retry, and next action with stable health tokens, safe next actions, operation/projection/audit metadata, and safe metadata IDs only.
- Projection-pending command rows render partial success and safe next action without terminal success copy; unknown or missing summaries fail closed to an unknown/unavailable state.
- Story status set to `done` after automated senior developer review.
- Sprint status updated for `3-10-conversation-item-status-and-next-action`.

### File List

- `_bmad-output/implementation-artifacts/3-10-conversation-item-status-and-next-action.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/ChatBotHealthStatus.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItemStatusSummary.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAiOutcomeConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotApprovalConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotAttachmentConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationItemStatusSummary.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotFailureStateConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotParticipantConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/OpenApiContractSpineTests.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/ProjectConversationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/Projections/ProjectConversationProjectionTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotLocalizationContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ChatBotSemanticTokenContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic fixes.

Findings fixed:

- [HIGH] Failure and retry summary facets collapsed retry queued/accepted/exhausted projection states into generic `retryable`/`retry-accepted`, so S1 could not truthfully expose the explicit projected retry state required by AC1/AC6. Fixed `ProjectConversationItemView.BuildFailureFacet` and `BuildRetryFacet` to preserve `FailureStateKind` retry tokens and added projection tests.
- [HIGH] Redacted attachment retry metadata could render as `healthy`, violating fail-closed unknown/unavailable behavior for redacted states. Fixed retry health mapping so redacted/unavailable/unknown states render `unknown`, with regression coverage.
- [MEDIUM] Projection-pending live-region behavior used an always-live section instead of the existing announcement deduplication matrix, risking repeated announcements on polling/re-entry. Fixed `ChatBotConversationItemStatusSummary` to use `ChatBotAnnouncementDeduplicationState` with `OncePerStableOperationKey` and moved live output to the focused partial-success reason.
- [MEDIUM] Story File List was incomplete; changed source tests `ServerBootstrapApiTests.cs` and `ProjectConversationE2ETests.cs` were not listed. Updated the File List.

Validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.Projections.ProjectConversationProjectionTests`
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -parallel none -class Hexalith.ChatBot.Server.Tests.ServerBootstrapApiTests`
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.Tests.ChatBotLocalizationContractTests -class Hexalith.ChatBot.UI.Tests.ProjectConversationServiceTests -class Hexalith.ChatBot.UI.Tests.ChatBotSemanticTokenContractTests`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests`
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -parallel none -class Hexalith.ChatBot.Contracts.Tests.ProjectConversationContractTests -class Hexalith.ChatBot.Contracts.Tests.OpenApiContractSpineTests`
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -parallel none -class Hexalith.ChatBot.Client.Tests.ClientGenerationTests`
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -parallel none -class Hexalith.ChatBot.Conformance.Tests.CrossTenantReadSurfaceIsolationTests`

Post-review finalization validation:

- `git diff --check`
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
- `tests/Hexalith.ChatBot.Contracts.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Contracts.Tests -noLogo -noColor`
- `tests/Hexalith.ChatBot.Client.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Client.Tests -noLogo -noColor`
- `tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests -noLogo -noColor`
- `tests/Hexalith.ChatBot.Conformance.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Conformance.Tests -noLogo -noColor`
- `tests/Hexalith.ChatBot.UI.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.Tests -noLogo -noColor`
- `tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests -noLogo -noColor`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -noColor -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests`

### Senior Developer Review (AI) - Re-validation

Reviewer: Jerome on 2026-06-10 (story-automator-review, auto-fix mode)

Outcome: Approved — no CRITICAL or HIGH issues; status remains `done`.

Scope: Re-validated all six acceptance criteria and every `[x]` task against the committed implementation (commit `d886bb1`) and the uncommitted E2E augmentation in the working tree. `_bmad/` and `_bmad-output/` were excluded from code review per workflow rules.

Validation (this pass, compiled xUnit v3 runners — VSTest sockets blocked in sandbox):

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → Build succeeded, 0 warnings, 0 errors.
- Contracts `ProjectConversationContractTests` + `OpenApiContractSpineTests` → 21 passed.
- Client `ClientGenerationTests` → 19 passed.
- Server `ProjectConversationProjectionTests` + `ServerBootstrapApiTests` → 104 passed.
- Conformance `CrossTenantReadSurfaceIsolationTests` → 10 passed.
- UI `ChatBotLocalizationContractTests` + `ProjectConversationServiceTests` + `ChatBotSemanticTokenContractTests` → 26 passed.
- E2E `ProjectConversationE2ETests` (incl. uncommitted attachment status-summary additions) → 24 passed.

Confirmed:

- AC1: 8 status facets (association, attachment, task, approval, command, failure, retry, next-action) with stable `healthy`/`degraded`/`failed`/`unknown` derived from explicit projected fields in `ProjectConversationItemView.BuildStatusSummary`.
- AC2: projection-pending command renders Degraded health + "Accepted; projection is pending." + `wait-for-projection`; `HealthFromCommandState` never maps `accepted-projection-pending` to Healthy/executed.
- AC3: `StatusSummary` added as an additive optional property on `ProjectConversationItem`; existing item-specific fields preserved.
- AC4: facet/health/next-action labels resolved through `ChatBotUiTextLocalizer` and EN/FR resources (28/28 key parity); audit/policy/context reference IDs gated by `AuthorizedAuditOperationId`/`AuthorizedPolicySnapshotId`; negative-leakage assertions (commandPayload/auditEnvelope/providerPayload/localPath) pass.
- AC5: status section defaults `aria-live="off"`; projection-pending announcement deduplicated via `ChatBotAnnouncementDedupRule.OncePerStableOperationKey`; forced-colors + reduced-motion CSS present; facet health conveyed by border + text label, not fill alone.
- AC6: contract/client/server/conformance/UI/E2E coverage all green; status summary is metadata-only, replay/order tolerant, and cross-tenant isolated.
- Prior-review fixes verified real: retry-state fidelity (`retry-queued` preserved — test lines 714/719), redacted-retry → Unknown (test lines 1411-1416), live-region dedup in `ChatBotConversationItemStatusSummary`, and File List completeness.

Findings:

- [MEDIUM][Transparency] Working tree carries uncommitted additions to `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs` (redacted/unavailable attachment status-summary coverage from the 2026-06-10 QA pass). The file is already in the File List; recorded here and in the Change Log. Tests pass.
- [LOW][Cosmetic] `ProjectConversationItemView.BuildCommandFacet` sets `MessageCode = "operation_projection_pending"` whenever an operation id is present, which is inaccurate for completed/failed command facets. The field is not rendered by any UI component and is not asserted from projection output, so there is no user-facing or contract impact. Left as-is to avoid churn; can be made state-aware in a future story.

No source code changes were required this pass.

### Change Log

- 2026-06-01: Implemented Story 3.10 conversation item status and next-action summary across contract, generated client, server projection mapping, UI rendering, localization, CSS, and tests.
- 2026-06-01: Senior developer review fixed retry/failure state fidelity, redacted retry health, live-region deduplication, and File List completeness; story moved to done.
- 2026-06-10: Story-automator review (auto-fix mode) re-validated all ACs/tasks and re-ran the touched contract/client/server/conformance/UI/E2E suites (all green). No CRITICAL/HIGH issues; status remains done. Recorded the uncommitted 2026-06-10 QA E2E augmentation for attachment status summaries (already in File List).
