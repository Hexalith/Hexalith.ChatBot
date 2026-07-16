---
baseline_commit: 9b029ac40c6cb6eb50128e21f63f4f989196cda8
---

# Story 3.9: Why this project evidence and provenance panel

Status: done

<!-- Validation: create-story checklist applied 2026-06-01. -->

## Story

As an authorized contributor,
I want to inspect why an email belongs to a project,
so that I can trust the association and see corrections.

## Acceptance Criteria

1. Given an associated email, when I open the "why this project" panel from S1, then the panel displays the originating signal class, matched value, confidence score, threshold band (`auto`/`ambiguous`/`fail-closed`), threshold policy version, scorer/kernel version, decision actor, decision actor type, decision timestamp, source version, correlation id, and source provenance. Originating signal classes must include explicit identifier, mailbox routing rule, thread identifier, human selection, and correction. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.9; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR23]
2. The panel shows authorized evidence as metadata-only evidence rows or chips, including evidence kind/signal class, matched value or safe display token, fingerprint/reference, freshness/redaction state, and confidence contribution when present. Raw mailbox body text, provider payloads, hidden project names, hidden participant/file names, raw decision notes, raw correction rationale, and unrestricted audit detail must not reach the contract, UI, logs, fixtures, or snapshots. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2; _bmad-output/planning-artifacts/architecture.md#Format-Patterns]
3. Given restricted evidence, when the panel renders for a user lacking authority, then inaccessible details are redacted consistently, the panel remains understandable to visual and screen-reader users, and it does not confirm hidden resource existence. Redacted/unavailable states must be visibly and programmatically distinct from unknown or missing values. [Source: _bmad-output/planning-artifacts/epics.md#Story 3.9; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md#Accessibility-And-Inclusion]
4. Given later association corrections or supersession, when the panel renders, then it links to any superseding correction and that correction exposes its own evidence panel without mutating or hiding the original association evidence. Original association, human selection, correction rationale redaction state, propagation status, and downstream impact metadata remain append-only history. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR91a; _bmad-output/implementation-artifacts/3-5-association-and-correction-decision-rendering.md#Previous-Story-Intelligence]
5. S1 exposes the panel from email-derived, association/correction decision, and corrected-context rows without introducing a new data plane. UI reads through `IChatBotClient` only and reuses the existing metadata-only association routing status query unless implementation proves additive contract fields on `ProjectConversationItem` are safer. [Source: src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs; src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs]
6. The panel preserves Stories 3.1 through 3.8 behavior: tenant/project partitioning, cursor pagination, source-email enrichment, participant/attachment/decision/approval/failure/AI outcome rendering, source-version replay safety, route-load/failure state clearing, EN/FR localization, responsive layout, forced-colors, reduced-motion, and metadata-only AI/source-evidence separation. [Source: _bmad-output/implementation-artifacts/3-8-ai-outcome-rendering.md#Current-State-To-Preserve]
7. Contract, generated client, server projection/query mapping, conformance, UI service/state/component, localization, static CSS, and UI E2E coverage prove the panel is localized, accessible, metadata-only, append-only, replay-safe, redaction-safe, cross-tenant isolated, and regression-safe for the existing S1 stream. [Source: _bmad-output/planning-artifacts/epics.md#FR23; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR60]

## Tasks / Subtasks

- [x] Extend or reuse the association evidence contract for the S1 "why" panel (AC: 1, 2, 3, 5, 7)
  - [x] Start from existing `AssociationRoutingStatus`, `AssociationCandidate`, `AssociationEvidenceReference`, and `AssociationConfidenceInput`; do not create a parallel provenance DTO if these can be extended additively.
  - [x] Extend `AssociationSignalClass` or an equivalent additive enum to cover `human-selection` and `correction` in addition to existing `explicit-project-identifier`, `mailbox-routing-rule`, and `conversation-thread-identifier`.
  - [x] Add additive fields only where needed for FR23 gaps: safe matched-value display token, evidence visibility/redaction/freshness state, confidence contribution, decision actor id/type, decision timestamp, threshold policy version, kernel version, superseding correction id/link, and correction panel availability.
  - [x] Preserve stable wire tokens and existing generated-client signatures as much as possible; regenerate `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs` and update the generated-client hash fixture if OpenAPI changes.
  - [x] Add negative contract/OpenAPI/generated-client tests proving no raw `body`, `subject`, `html`, `sourceContext`, `providerPayload`, `decisionNote`, `correctionRationale`, `policyBody`, `auditEnvelope`, local path, prompt/output, or hidden resource-name fields are exposed.
- [x] Wire the panel data through the existing query/read surface (AC: 1, 2, 3, 5, 7)
  - [x] Prefer the existing `GET /api/v1/associations/{associationId}/routing-status` contract and `IChatBotClient.GetAssociationRoutingStatusAsync` for panel detail.
  - [x] If S1 item-level summary fields are needed for fast rendering, add them to `ProjectConversationItem` additively and map them through `Program.cs` `ToContractItem`; keep full detail behind the routing-status query.
  - [x] Reuse `Program.cs` `BuildAssociationEvidenceRefs`, `BuildAssociationReasonCodes`, and `BuildAssociationRoutingStatus` patterns; do not add direct UI reads from `IAssociationProjectionStore`, `IProjectConversationProjectionStore`, Dapr state, EventStore, audit stores, or sibling services.
  - [x] Preserve authenticated tenant/project authority from the existing API gate and projection access checks; route/body/query values are not authority.
  - [x] Ensure restricted candidates/exclusions collapse to safe redacted evidence rows and disabled reasons instead of hidden raw details.
- [x] Add UI state/service support for opening and rendering panel detail (AC: 1, 2, 3, 5, 6, 7)
  - [x] Add a small S1 panel state/effect/service path that calls `IChatBotClient.GetAssociationRoutingStatusAsync(associationId)` and clears stale panel data on project route changes, failed loads, and association changes.
  - [x] Reuse `AssociationReviewService` mapping logic where practical, or extract a shared metadata-only mapper if that removes real duplication without coupling S1 to S2 UI state.
  - [x] Keep the selected/open panel association scoped by project id and association id; a late response from a previous project must not render in the current project.
  - [x] Expose loading, empty/unavailable, redacted, stale, and corrected-context states with safe next action text from the message catalog/localization resources.
  - [x] Do not implement association decision submission, correction submission, task intent, audit investigation, attachment capture, AI context packaging, or approval actions in this story.
- [x] Implement the governed "why this project" panel component (AC: 1, 2, 3, 4, 5, 6, 7)
  - [x] Add a dedicated governed component such as `ChatBotWhyProjectPanel.razor` or `ChatBotAssociationProvenancePanel.razor`; do not overload `ChatBotDecisionConversationItem`, `ChatBotEmailConversationItem`, or `ChatBotBlockedState` as the full detail panel.
  - [x] Provide open affordances from email-derived and decision/correction rows using `ChatBotEvidenceChip` or an equivalent icon/button with a localized accessible name.
  - [x] Render evidence, confidence, threshold/status, actor, and timestamp in a stable order consistent with S1 rows. Plain-language labels must precede raw IDs; stable IDs remain metadata.
  - [x] Use a complementary panel/drawer pattern with a unique `aria-label`; the panel must not appear as another conversation item or a second transcript.
  - [x] Redacted/unavailable evidence must include reachable explanation text, not tooltip-only help. The explanation must not name hidden resources.
  - [x] Links to superseding corrections should focus/open the correction's own panel or move focus to the relevant conversation row without losing current route context.
- [x] Preserve append-only provenance and correction semantics (AC: 2, 4, 6, 7)
  - [x] Original association evidence remains immutable for display; later human selections/corrections link through `predecessorAssociationId`, `supersedesAssociationId`, `supersededByAssociationId`, `correctionId`, and propagation metadata.
  - [x] Same source-version replacement may update the same projection row deterministically, but later corrections must not rewrite earlier evidence, decision actor, confidence score, or timestamp.
  - [x] During `correcting` or `correction-delayed`, render stale/corrected-context state and safe next action; do not imply AI context or downstream stores are ready until propagation metadata says so.
  - [x] Preserve `ProjectConversationItemView.IsSourceEmailEnrichableKind` semantics. The panel may use source-email display tokens, but it must not pull raw email/provider body fields into S1.
- [x] Add EN/FR localization, responsive CSS, and accessibility behavior (AC: 1, 2, 3, 5, 6, 7)
  - [x] Add resource keys for panel title, open action, loading/unavailable/redacted states, signal classes, matched value, confidence contribution, threshold band, threshold policy, kernel version, decision actor/type, decision timestamp, superseding correction, evidence freshness, source provenance, source version, correlation id, safe next action, and accessible names.
  - [x] Avoid concatenated strings for accessible names; use localized templates with parameters.
  - [x] Extend `chatbot.tokens.css` using existing Fluent/FrontComposer tokens only; do not introduce raw `#`, `rgb(`, or `hsl(` color literals.
  - [x] Ensure phone/tablet layouts stack the panel without overlap and retain complete read-only evidence summary, redaction state, and safe next action.
  - [x] Forced-colors and reduced-motion behavior must cover panel open/close, evidence chips, status/freshness chips, focus outlines, and correction links.
- [x] Add focused validation coverage (AC: all)
  - [x] Contract tests for association routing status/provenance fields, enum wire tokens, generated client availability, OpenAPI schema parity, additive compatibility, and absence of raw evidence/body/provider/policy/audit fields.
  - [x] Server tests for `BuildAssociationRoutingStatus` and projection/query mapping across auto association, ambiguous association, fail-closed, human selection, correction, superseded correction, restricted evidence, stale/correction-delayed, duplicate delivery, stale replay, source email before/after association, and tenant/project partitioning.
  - [x] Conformance/read-surface tests proving foreign, unknown, malformed, missing, ambiguous, stale, and unsafe tenant/project/association contexts collapse to safe denial without evidence leakage.
  - [x] UI service/state/component tests for panel load/clear behavior, late response isolation, accessible names, keyboard focus, evidence/risk/status/actor/timestamp order, localized labels, redaction explanations, correction links, and no raw colors.
  - [x] Playwright/UI.E2E coverage for opening the panel from email and decision rows, redacted evidence, correction link navigation, forced-colors, reduced-motion, responsive layout, and negative assertions for hidden project/file/participant/body/provider text.

## Dev Notes

### Scope Boundaries

- This story is the S1 read-only "why this project" evidence/provenance panel for associated email and association/correction decision rows.
- It may add additive contract fields, generated-client updates, server query/projection mapping, S1 state/effects, a dedicated governed panel component, localization, CSS, and tests needed to show association evidence and provenance.
- Reuse the existing association routing status contract and S2 evidence mapping wherever practical. The implementation should not duplicate association scoring, association decision submission, or correction submission.
- Do not implement new association commands, task intent detection, next-action consolidation, audit investigation, attachment capture/storage, attachment authorization expansion, AI context packaging, approval policy logic, outbound communication, model invocation, or CLI/MCP parity UI in this story.
- Do not add a direct browser data plane to server projection stores, EventStore, Dapr, audit stores, sibling services, mailbox provider payloads, or raw email bodies.

### Existing Code To Reuse

- Association detail contract and generated client path: `src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs`, `src/Hexalith.ChatBot.Contracts/Commands/AssociationCandidate.cs`, `AssociationConfidenceInput.cs`, `AssociationEvidenceReference.cs`, `AssociationExclusion.cs`, `src/Hexalith.ChatBot.Contracts/Enums/AssociationSignalClass.cs`, OpenAPI `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`, and generated client output under `src/Hexalith.ChatBot.Client/Generated/`.
- Existing routing-status endpoint and mapping: `src/Hexalith.ChatBot.Server/Program.cs` `BuildAssociationRoutingStatus`, `BuildAssociationEvidenceRefs`, `BuildAssociationReasonCodes`, `BuildAssociationDisabledReasons`, and `BuildAssociationNextActionCodes`.
- S1 conversation contract and mapping: `src/Hexalith.ChatBot.Contracts/Queries/ProjectConversationItem.cs`, `src/Hexalith.ChatBot.Server/Projections/ProjectConversationItemView.cs`, `src/Hexalith.ChatBot.Server/Program.cs` `ToContractItem`, and `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`.
- Existing S2 UI evidence mapper and command-safe service: `src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs` and `src/Hexalith.ChatBot.UI/State/AssociationReview/*`.
- Existing S1 UI route/state/components: `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`, `ChatBotEmailConversationItem.razor`, `ChatBotDecisionConversationItem.razor`, `ChatBotEvidenceChip.razor`, `ChatBotStatusBanner.razor`, `ChatBotBlockedState.razor`, `src/Hexalith.ChatBot.UI/State/ProjectConversation/*`, localization resources, and `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`.
- Existing tests to extend: `AssociationContractTests`, `ProjectConversationContractTests`, `OpenApiContractSpineTests`, `ClientGenerationTests`, `ProjectConversationProjectionTests`, `CrossTenantReadSurfaceIsolationTests`, `AssociationReviewServiceTests`, `ProjectConversationServiceTests`, `ProjectConversationStateTests`, `ChatBotLocalizationContractTests`, `ChatBotSemanticTokenContractTests`, `ProjectConversationE2ETests`, and CSS/static contract tests.

### Current State To Preserve

- Story 3.1 added the S1 read surface, tenant/project keyed projection storage, cursor pagination, authorized empty reads, safe denial for unauthorized reads, governed UI route/state, and reducer clearing on route load/failure.
- Story 3.2 added source-email enrichment while keeping raw provider `SourceContext` and email bodies out of S1. The why panel may show source provenance display tokens, not raw provider or body content.
- Story 3.3 and 3.4 added participant and attachment materialization with redaction rules. The why panel must not leak hidden participant or attachment metadata through evidence rows.
- Story 3.5 established append-only association/correction decision items with deterministic `decision:{associationId}:{sourceVersion}` ids and correction propagation metadata. This story must link to correction history instead of rewriting prior evidence.
- Story 3.6 established policy/audit visibility rules for approval rows. Apply the same posture to decision actor, audit/correlation metadata, and policy/threshold references: show authorized metadata, redact restricted detail.
- Story 3.7 added catalog-backed failure/retry/blocked rows and reachable explanation patterns. Reuse reachable explanation behavior for redacted/unavailable evidence.
- Story 3.8 added AI outcome rows and strengthened metadata-only raw prompt/output/provider/tool leakage tests. Do not regress those negative assertions or make AI rows look like source evidence.
- Recent git history shows Story 3.8, 3.7, 3.6, 3.5, and 3.4 as the current implementation sequence. Build on their dedicated component/projection patterns.
- Existing worktree has an unrelated modification in `_bmad-output/story-automator/orchestration-2-20260531-161212.md`; do not touch or revert it.

### Architecture Guardrails

- Contract Spine remains the source of truth: OpenAPI 3.1 plus generated client plus contract tests. UI reads through `IChatBotClient`; S1 must not reference server projection internals. [Source: _bmad-output/planning-artifacts/architecture.md#Contract-Spine]
- Dependency direction remains `Contracts <- Client <- UI/Server`; UI, CLI, and MCP must not replicate gateway or projection internals. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural-Boundaries]
- ChatBot derived records carry tenant id, source provenance, derivation kernel version, redaction state, retention class, schema version, source version, and correlation id. Evidence/provenance display must preserve these fields. [Source: _bmad-output/planning-artifacts/architecture.md#Data-Architecture]
- Tenant authority comes from authenticated claims/context and access projections, never route/body/query values. Unknown, foreign, malformed, missing, ambiguous, stale, or unsafe context collapses to safe denial without confirming hidden resources. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#NFR2]
- Dapr pub/sub delivery is at-least-once; projection/query behavior and tests must remain duplicate and out-of-order tolerant. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Use `System.Text.Json` shared options and existing enum-member converters. Do not add Newtonsoft.Json, inline serializer options, or new serialization libraries.
- Lifecycle-state strings are exact and shared: `Received`, `Proposed`, `Associated`, `Rejected`, `Deferred`, `NeedsReview`, `Failed`, `Skipped`, `Corrected`, `Correcting`, and `Correction-delayed`.

### UX And Accessibility Guardrails

- The UX package is binding even without mockups. Use the IA/component/state/accessibility tables rather than inventing a separate visual pattern.
- The panel should be a compact operational evidence/provenance surface, not a decorative card feed or a second conversation. Use Fluent/FrontComposer components and existing governed primitives.
- Evidence drawer/panel semantics apply: source evidence expands without requiring users to read the whole email thread, and inaccessible details are redacted.
- Actor type, permitted identity label, timestamp, source surface, and state label must remain available; actor/type context should precede content in accessible names where row affordances open the panel.
- Keyboard users must be able to open the panel, reach every evidence row/explanation, follow correction links, close the panel, and return focus to the invoking row/control.
- Repeated `complementary` landmarks must have unique `aria-label` values. Avoid concatenated accessible-name strings.
- EN/FR localization is required. Stable machine tokens, IDs, lifecycle states, evidence fingerprints, source versions, schema versions, and correlation IDs remain untranslated; labels/explanations are translated.
- Off-surface affordances such as copy/export/download must apply the same redaction as the visual surface or remain unavailable for this story.

### Project Structure Notes

- Contract DTO and enum changes belong under `src/Hexalith.ChatBot.Contracts/Queries/`, `src/Hexalith.ChatBot.Contracts/Commands/`, `src/Hexalith.ChatBot.Contracts/Enums/`, and `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`.
- Regenerated client output belongs in `src/Hexalith.ChatBot.Client/Generated/`; do not hand-edit `.g.cs` files.
- Server query/projection mapping changes belong in `src/Hexalith.ChatBot.Server/Program.cs` and `src/Hexalith.ChatBot.Server/Projections/` only where existing projection views need additive metadata.
- UI changes belong under `src/Hexalith.ChatBot.UI/Components/Governed/`, `Components/Pages/`, `Services/`, `State/ProjectConversation/`, `Localization/`, and `wwwroot/css/`.
- Tests mirror source boundaries under `tests/Hexalith.ChatBot.Contracts.Tests/`, `Client.Tests/`, `Server.Tests/Projections/`, `Conformance.Tests/`, `UI.Tests/`, and `UI.E2E.Tests/`.

### Previous Story Intelligence

- Story 3.8 is the immediate implementation reference for adding S1 UI surface behavior without leaking raw AI/provider/tool fields. Preserve its negative leakage assertions.
- Story 3.7 proved catalog-backed reachable explanations for blocked/unavailable states. Use that pattern for redacted evidence explanation instead of tooltip-only help.
- Story 3.6 fixed policy/audit identifier leakage. The why panel must not show threshold/policy/audit metadata beyond authorized display tokens.
- Story 3.5 is the strongest pattern for this story: append-only association/correction history, deterministic decision item ids, correction links, propagation status, stale/corrected-context behavior, and evidence summaries.
- Story 3.4 review fixed restricted attachment metadata leakage. Evidence rows must not surface hidden file names, file ids, or attachment metadata unless authorized.
- Story 3.3 review fixed raw enum display and actor fallback issues. Evidence/signal labels need localized user-facing labels; machine tokens may appear only as metadata.
- Story 3.2 review fixed source-email replay and source-version conflation. Panel data must keep association source-version ownership separate from source-email enrichment.
- Story 3.1 review fixed cross-project stale UI state and authorized empty reads. Panel state must clear on route change/failure and reject late responses for another project/association.
- Prior validation used compiled xUnit v3 executables when VSTest socket creation was blocked in this sandbox. Prefer that fallback if `dotnet test` fails with local socket permission errors.

### Testing Notes

- Minimum validation before handoff:
  - `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`
  - Targeted contract/client/server/UI/conformance tests touched by this story
  - `tests/Hexalith.ChatBot.UI.E2E.Tests/bin/Debug/net10.0/Hexalith.ChatBot.UI.E2E.Tests -noLogo -parallel none -class Hexalith.ChatBot.UI.E2E.Tests.ProjectConversationE2ETests` if the compiled runner is available
- Use xUnit v3, Shouldly, NSubstitute, bUnit, existing Playwright fixtures, and existing CSS/static contract tests only. Do not add assertion, mocking, UI, or E2E libraries.
- Include negative content assertions for raw email body/subject/html, provider payload/source context, raw decision note, raw correction rationale, unauthorized project/file/participant/recipient names, hidden evidence values, raw policy body, raw audit envelope, prompt/output/tool payloads, local paths, and tokens in API, UI, fixture, logs/test output where applicable.
- Include ordering/replay tests around duplicate/stale association events, correction arriving before/after source email enrichment, source email before/after association, restricted evidence, correction-delayed, prior project/corrected project, and panel late-response isolation after project switch.

### Latest Technical Notes

- Architecture pins .NET SDK `10.0.302`, `net10.0`, warnings-as-errors, central package management, Blazor + Fluent UI v5 RC via FrontComposer, Fluxor, Dapr 1.17.x, Aspire 13.3.x, xUnit v3 3.2.x, Shouldly, NSubstitute, and Testcontainers. Do not upgrade packages for this rendering story. [Source: _bmad-output/planning-artifacts/architecture.md#Technical-Constraints--Dependencies]
- Dapr pub/sub documentation states at-least-once delivery semantics; association/provenance projection tests must keep duplicate and out-of-order tolerance. [Source: https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- NuGet lists `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.2-26098.1` as a prerelease package and notes newer prerelease versions exist. Keep the repo-pinned Fluent/FrontComposer stack unless a separate dependency-upgrade story is created. [Source: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.0-rc.2-26098.1]
- Microsoft Learn tracks .NET 10 breaking changes separately; do not mix framework migration work into the evidence/provenance panel. [Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/10]

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 3, Story 3.9, FR23, FR25, FR28, and cross-cutting UX guidance.
- `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` - FR23, FR57, FR60, FR91a, FR96, NFR2, NFR11, NFR36, NFR48, NFR60, and NFR64.
- `_bmad-output/planning-artifacts/architecture.md` - Contract Spine, derived-record shape, evidence/confidence capture, projection boundaries, Dapr/event ordering, source tree, and testing standards.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md` - evidence drawer/panel behavior, conversation/audit semantics, keyboard/focus behavior, redaction, responsive layout, reduced motion, and forced-colors expectations.
- `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md` - evidence chip/drawer, conversation shell, status semantics, component/token posture, and high-contrast constraints.
- `src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs` - current metadata-only association detail query contract to reuse/extend.
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationCandidate.cs`, `AssociationConfidenceInput.cs`, `AssociationEvidenceReference.cs`, and `AssociationExclusion.cs` - current candidate/evidence/provenance shapes.
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationSignalClass.cs` - current signal-class enum to extend for human selection and correction.
- `src/Hexalith.ChatBot.Server/Program.cs` - association routing status mapping and `ToContractItem` mapping chokepoints.
- `src/Hexalith.ChatBot.Server/Projections/AssociationCandidateView.cs` and `AssociationProjectionHandler.cs` - current association projection state and S1 materialization path.
- `src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs` - existing metadata-only association evidence mapper and `IChatBotClient` usage pattern.
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`, `ChatBotConversationStream.razor`, `ChatBotEmailConversationItem.razor`, `ChatBotDecisionConversationItem.razor`, and `ChatBotEvidenceChip.razor` - current S1 render and evidence primitive surfaces to extend.
- `_bmad-output/implementation-artifacts/3-1-render-email-derived-project-conversation-s1.md` through `_bmad-output/implementation-artifacts/3-8-ai-outcome-rendering.md` - prior S1 implementation context, review fixes, and regression targets.
- `https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/` - Dapr pub/sub at-least-once delivery semantics.
- `https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.0-rc.2-26098.1` - repo-pinned Fluent UI Blazor package stream.
- `https://learn.microsoft.com/en-us/dotnet/core/compatibility/10` - .NET 10 breaking change index; no migration work in this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (story authoring)

### Debug Log References

- Create-story workflow executed 2026-06-01T08:08:05+02:00.
- Source discovery loaded `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`, `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/DESIGN.md`, sprint status, Story 3.8, current S1 association/projection/UI/test files, sibling project-context facts, recent git history, and official Dapr/NuGet/Microsoft technical references.
- Discovery results: loaded `{epics_content}` from 1 file, `{architecture_content}` from 1 file, `{prd_content}` from focused PRD shards/excerpts, `{ux_content}` from 2 sharded UX files, and `{project_context}` from sibling module project-context files with FrontComposer, Conversations, Folders, EventStore, Tenants, Parties, and Commons rules most relevant to this story.
- Validation checklist applied during authoring: story contains user value, ACs, tasks, scope boundaries, existing-code reuse, project structure notes, architecture/UX guardrails, previous-story intelligence, testing notes, latest technical notes, and references.
- Dev-story workflow executed 2026-06-01T08:28:43+02:00.
- Build validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
- `dotnet test` attempted for Contracts and aborted due VSTest socket permission error in sandbox; compiled xUnit v3 runners were used per story testing notes.
- Full regression fallback executed with compiled runners: AppHost, Architecture, Aspire, Client, Conformance, Contracts, Integration, Server, ServiceDefaults, Testing, UI.E2E, UI, and Workers test assemblies. Integration Tier-3 Aspire tests remained skipped by their existing Docker/DAPR opt-in gate.
- Story-automator review executed 2026-06-01. Review validation loaded the story, architecture/UX guardrails, git changes, and checklist. Fixed review findings automatically.
- Review validation: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false`.
- Review targeted runner validation: Contracts `AssociationContractTests`, Client `ClientGenerationTests`, Server `ServerBootstrapApiTests` and `AssociationProjectionTests`, Conformance `CrossTenantReadSurfaceIsolationTests`, UI `AssociationReviewComponentContractTests`, `ProjectConversationServiceTests`, `AssociationReviewServiceTests`, and UI.E2E `ProjectConversationE2ETests`.
- Dev-story validation rerun 2026-06-10: story was already `done` with all tasks checked; no implementation tasks remained.
- Validation build: `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- Validation test attempt: `DiffEngine_Disabled=true dotnet test Hexalith.ChatBot.slnx --no-build -m:1 /nr:false` was blocked by VSTest socket permission errors in the sandbox.
- Validation fallback: compiled xUnit v3 runners executed for AppHost, Architecture, Aspire, CLI, Client, Conformance, Contracts, Integration, MCP, Server, ServiceDefaults, Testing, UI.E2E, UI, and Workers test assemblies. Result: 2522 total, 0 failed, 0 errors, 2 skipped.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story status set to `ready-for-dev`.
- Sprint status updated for `3-9-why-this-project-evidence-and-provenance-panel`.
- Extended the association routing-status contract and OpenAPI schema with metadata-only why-panel evidence fields, human-selection/correction signal tokens, decision actor metadata, superseding correction links, and regenerated the typed client/hash fixture.
- Routed S1 panel loading through `IChatBotClient.GetAssociationRoutingStatusAsync` from `ProjectConversationService` and Fluxor state/effects, with route-change clearing and project/association-scoped late-response isolation.
- Added `ChatBotWhyProjectPanel.razor`, S1 open affordances on email and decision rows, EN/FR resources, and token-only CSS for responsive, forced-colors, and reduced-motion behavior.
- Preserved append-only correction/provenance display by rendering correction links, propagation/stale context metadata, safe next action, source version, correlation id, and redaction/freshness states without pulling raw email/provider/note details into S1.
- Added/updated contract, generated-client, UI service/state/component static tests, and exercised server/conformance/E2E/full regression fallback suites.
- Review fixed the why-panel landmark naming so the complementary panel exposes a unique accessible name instead of being overridden by a generic heading label.
- Review added localized EN/FR signal-class labels while preserving stable machine tokens as metadata.
- Review added server endpoint coverage proving routing-status why-panel evidence is enriched as metadata-only output and does not expose raw decision notes or correction rationale.
- Finish validation fixed the why-panel hidden-state CSS so native `hidden` close behavior is honored by the browser and E2E tests.
- 2026-06-10 validation rerun found no unchecked tasks; build and compiled xUnit runner fallback passed with only the existing Integration opt-in skips.
- 2026-06-10 story-automator review pass fixed why-panel metadata-token fidelity (band/provenance/redaction were rendered as PascalCase enum names instead of the documented wire tokens) and hardened the superseding-correction affordance so it only renders when a navigable association target exists.

### Senior Developer Review (AI)

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- HIGH: The why-panel landmark used both `aria-labelledby` and a unique `aria-label`; accessible-name precedence meant the panel could be exposed only as the generic "Why this project" heading instead of the required unique complementary label. Fixed in `ChatBotWhyProjectPanel.razor` by making the panel an explicit complementary landmark with the association-specific label.
- MEDIUM: The task for localized signal-class labels was marked complete, but the panel rendered raw signal-class tokens as the only user-facing label. Fixed by adding EN/FR signal-class resources and a localizer mapper, while still showing stable wire tokens as metadata.
- MEDIUM: Server-side routing-status evidence enrichment had no server endpoint regression coverage. Fixed by adding a `ServerBootstrapApiTests` routing-status test for metadata-only enriched evidence and raw-note/rationale exclusion.
- MEDIUM: Story File List was missing changed implementation/test files discovered by git. Updated the File List to include review-added and previously omitted test/localizer files.

Review checklist summary:

- Acceptance criteria cross-checked against contract, server mapper, UI service/state/components, localization, CSS, conformance, and E2E coverage.
- Git vs story File List discrepancies reviewed. Unrelated `_bmad-output/story-automator/orchestration-2-20260531-161212.md` and submodule pointer/worktree signals were not modified.
- MCP/web documentation search was not performed because this review did not require current external API documentation; local architecture/UX/PRD/story sources were authoritative for the implementation.

#### Review pass 2026-06-10 (story-automator review)

Outcome: Approved after automatic fixes. No critical issues remain; story stays `done`.

Findings fixed:

- MEDIUM: The why-panel surfaced several stable-token metadata fields as PascalCase .NET enum names instead of the documented wire tokens — `ProjectConversationService.GetAssociationWhyPanelAsync` mapped `ThresholdBand`, `SourceProvenance`, and `RedactionState` with `.ToString()`, rendering `Auto`/`M365MailboxIntake`/`Metadata_only` rather than `auto`/`m365-mailbox-intake`/`metadata_only`. This contradicted AC1 (which references the `auto`/`ambiguous`/`fail-closed` band), AC2's stable-machine-token requirement, the test fixture's own expected tokens, and regressed the Story 3.3 review fix for raw enum display. Fixed by mapping all routing-status enum metadata through the existing `WireToken<TEnum>` helper (also applied to the stored `LifecycleState`/`Outcome` fields for consistency). Locked in with new panel-level assertions in `ProjectConversationServiceTests.ServiceShouldReadWhyPanelThroughRoutingStatusAndMapSafeEvidence`.
- LOW: The superseding-correction button rendered whenever `SupersedingCorrectionLink` was present, but its click handler navigates only via `SupersededByAssociationId` (the only valid why-panel association target). A non-null link with no association id would have produced a dead button, weakening AC4's requirement that the correction link focus/open the correction's own panel. Fixed in `ChatBotWhyProjectPanel.razor` by rendering the affordance only when a navigable `SupersededByAssociationId` exists and deriving the link token from it when absent.

Notes (no code change):

- The S1 "E2E" coverage is a hand-authored static-fixture + component-source-grep pattern (no live browser render in this sandbox), consistent with Stories 3.1–3.8. The QA-added coverage for the `mailbox-routing-rule` and `conversation-thread-identifier` signal classes plus the test summary were uncommitted working-tree changes at review time; they are sound and pass.

Review validation:

- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — 0 warnings, 0 errors.
- Compiled xUnit runner fallback (VSTest sockets blocked in sandbox): `ProjectConversationServiceTests` 6/6, full `UI.Tests` 131/131, `ProjectConversationE2ETests` 24/24, `AssociationContractTests` 10/10, `ClientGenerationTests` 19/19, `ServerBootstrapApiTests` 45/45, `CrossTenantReadSurfaceIsolationTests` 10/10.

### Change Log

- 2026-06-01: Implemented Story 3.9 why-this-project evidence/provenance panel and moved story to review.
- 2026-06-01: Senior developer review fixed accessibility landmark naming, localized signal-class labels, server endpoint coverage, and File List completeness; moved story to done.
- 2026-06-10: Re-ran dev-story validation for already-complete Story 3.9; no code or checkbox changes were required.
- 2026-06-10: Story-automator review fixed why-panel metadata-token fidelity (band/provenance/redaction wire tokens) and hardened the superseding-correction affordance; added panel-level wire-token assertions. Story remains done (no critical issues).

### File List

- `_bmad-output/implementation-artifacts/3-9-why-this-project-evidence-and-provenance-panel.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.ChatBot.Client/Generated/HexalithChatBotClient.g.cs`
- `src/Hexalith.ChatBot.Contracts/Commands/AssociationEvidenceReference.cs`
- `src/Hexalith.ChatBot.Contracts/Enums/AssociationSignalClass.cs`
- `src/Hexalith.ChatBot.Contracts/Queries/AssociationRoutingStatus.cs`
- `src/Hexalith.ChatBot.Contracts/openapi/hexalith.chatbot.v1.yaml`
- `src/Hexalith.ChatBot.Server/Program.cs`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationStream.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotDecisionConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotEmailConversationItem.razor`
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotWhyProjectPanel.razor`
- `src/Hexalith.ChatBot.UI/Components/Pages/ProjectConversation.razor`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextKey.cs`
- `src/Hexalith.ChatBot.UI/Localization/ChatBotUiTextLocalizer.cs`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx`
- `src/Hexalith.ChatBot.UI/Localization/SharedResource.resx`
- `src/Hexalith.ChatBot.UI/Services/AssociationReviewService.cs`
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationActions.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationReducers.cs`
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationState.cs`
- `src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css`
- `tests/Hexalith.ChatBot.Client.Tests/ClientGenerationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/CrossTenantReadSurfaceIsolationTests.cs`
- `tests/Hexalith.ChatBot.Conformance.Tests/Harness/IsolationHttpHost.cs`
- `tests/Hexalith.ChatBot.Contracts.Tests/AssociationContractTests.cs`
- `tests/Hexalith.ChatBot.Server.Tests/ServerBootstrapApiTests.cs`
- `tests/Hexalith.ChatBot.UI.E2E.Tests/ProjectConversationE2ETests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewComponentContractTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/AssociationReviewServiceTests.cs`
- `tests/Hexalith.ChatBot.UI.Tests/ProjectConversationServiceTests.cs`
- `tests/fixtures/hexalith-chatbot-generated-client.sha256`
