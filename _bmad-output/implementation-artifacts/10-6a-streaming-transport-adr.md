---
baseline_commit: 51b99bd
---

# Story 10.6a: AI-response streaming transport ADR

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-06-19. -->

## Story

As a frontend/solution architect,
I want an accepted ADR that fixes the AI-response streaming transport,
so that Story 10.6b implements progressive rendering on a decided, safe transport instead of an open question.

## Acceptance Criteria

1. **Accepted ADR records the transport decision.** Given the open decision in `_bmad-output/planning-artifacts/architecture.md` §Frontend Architecture, when this story completes, then `docs/adrs/ai-response-streaming-transport.md` exists with `Status: Accepted`, records the chosen transport, explicitly compares extending the existing SignalR projection-nudge model against introducing a dedicated streaming channel, documents the rejected alternative, and states operational consequences. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.6a; _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]

2. **Safety floor is preserved by the decision.** Given the chosen transport, when the ADR evaluates safety, then it demonstrates that pushed/streamed data is never authoritative, the UI re-queries or verifies against server state before durable completion claims, fail-closed behavior remains intact on transport/session/dependency ambiguity, tenant and authorization boundaries stay server-owned, and no ungoverned write path bypasses CommandGateway. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.6a; _bmad-output/planning-artifacts/architecture.md#Surfaces; _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]

3. **Story 10.6b is unblocked with a concrete handoff.** Given the ADR is accepted, when Story 10.6b is planned or implemented, then the handoff names the exact transport contract 10.6b must implement for progressive rendering and always-reachable Stop/Cancel, including cancellation semantics, reconnect/resume behavior, stale/out-of-order message handling, live-region/focus obligations, and the tests expected for 10.6b. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.6b; _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md#Streaming & interruption]

4. **Architecture references stop calling the decision open.** Given `architecture.md` currently marks AI-response streaming transport as open and owned by Story 10.6a, when the ADR is accepted, then the architecture reference is updated or supplemented to point at `docs/adrs/ai-response-streaming-transport.md` as the accepted decision without changing production architecture beyond the documented decision. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]

5. **No production streaming implementation lands in this story.** Given this story is decision work only, when reviewing the diff, then production code changes are absent unless strictly needed for documentation-link hygiene; progressive rendering, Stop/Cancel transport wiring, hubs/channels, provider integration, and UI behavior changes remain out of scope for Story 10.6b. [Source: _bmad-output/planning-artifacts/epics.md#Story 10.6a; _bmad-output/planning-artifacts/epics.md#Story 10.6b]

6. **Verification is concrete and reproducible.** Given the story completes, when reviewing the Dev Agent Record, then it lists source checks proving the ADR exists, is accepted, names the selected/rejected transports, includes the safety/fail-closed analysis, updates the architecture reference, leaves production code untouched, and passes `git diff --check`. [Source: .agents/skills/bmad-create-story/checklist.md; docs/adrs/domainservice-sdk-host-adoption.md#Verification]

## Tasks / Subtasks

- [x] Re-read the binding source context before drafting the ADR (AC: 1, 2, 3, 5)
  - [x] Read `_bmad-output/planning-artifacts/epics.md` Story 10.6a and 10.6b plus the Epic 10 goal/dependencies.
  - [x] Read `_bmad-output/planning-artifacts/architecture.md` §Frontend Architecture, §Surfaces, §Architectural Boundaries, and project structure notes.
  - [x] Read `_bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md` §Streaming & interruption.
  - [x] Read existing ADR style from `docs/adrs/domainservice-sdk-host-adoption.md` and `docs/adrs/audit-two-phase.md`.

- [x] Author the accepted ADR at `docs/adrs/ai-response-streaming-transport.md` (AC: 1, 2, 3)
  - [x] Use the established ADR shape: title, Status, Context, Decision, Consequences, Alternatives Considered, Verification.
  - [x] Choose one transport: extend the existing SignalR projection-nudge model or introduce a dedicated streaming channel.
  - [x] State the rejected alternative and why it is not the default for 10.6b.
  - [x] Include the required safety analysis: never-trust-payload, re-query/verify before durable completion, fail-closed on ambiguity, tenant/authorization authority stays server-side, metadata-only diagnostics, and no CommandGateway bypass.
  - [x] Include the 10.6b handoff: progressive partial response semantics, Stop/Cancel command/transport semantics, reconnect/resume handling, stale/out-of-order handling, focus/live-region obligations, and required test evidence.

- [x] Close the open architecture reference without broad planning churn (AC: 4)
  - [x] Update the AI-response streaming transport bullet in `_bmad-output/planning-artifacts/architecture.md` or add a narrow dated note next to it that points to the accepted ADR.
  - [x] Preserve the existing Epic 10 correction note and Epic 12 forward-closure notes; do not reopen shipped Epic 10 shell/component stories.
  - [x] Do not edit the `Hexalith.FrontComposer` submodule or generated FrontComposer output.

- [x] Prove scope stayed ADR-only (AC: 5, 6)
  - [x] Confirm `git diff --name-only -- src Hexalith.FrontComposer` is empty (no production or FrontComposer changes).
  - [x] Confirm the only `tests/` changes are the two justified test guards (ADR conformance test + Epic 10 readiness-guard update forced by `10-6a → review`); see the documented exception under Testing Notes. (Corrected during AI review: the original "git diff -- src tests must be empty" assertion was inaccurate — tests/ is not empty.)
  - [x] Confirm no new hub/channel/client/service/UI transport code was added.
  - [x] Confirm Story 10.6b remains the owner of implementation, progressive rendering, and production Stop/Cancel wiring.

- [x] Run documentation verification (AC: all)
  - [x] Run `rg -n "Status|Accepted|SignalR|projection-nudge|dedicated streaming channel|never trust payload|fail-closed|CommandGateway|10.6b|Stop/Cancel|Verification" docs/adrs/ai-response-streaming-transport.md`.
  - [x] Run `rg -n "ai-response-streaming-transport.md|Story 10.6a|accepted ADR|AI-response streaming transport" _bmad-output/planning-artifacts/architecture.md`.
  - [x] Run `git diff --name-only -- src Hexalith.FrontComposer` (empty: no production/FrontComposer changes).
  - [x] Build + run the two justified test guards: `Hexalith.ChatBot.Architecture.Tests` (incl. `AiResponseStreamingTransportAdrTests`) and `Hexalith.ChatBot.UI.E2E.Tests` (incl. `Epic10ReleaseReadinessE2ETests`).
  - [x] Run `git diff --check`.

## Dev Notes

### Discovery Results

- Loaded `epics_content` from `_bmad-output/planning-artifacts/epics.md`. Epic 10 is M2 release-readiness closure for FrontComposer Shell adoption and the governed interactive chat surface. Story 10.6a is the ADR gate; Story 10.6b remains blocked until the ADR is accepted.
- Loaded `architecture_content` from `_bmad-output/planning-artifacts/architecture.md`. Relevant sections: Surfaces, Frontend Architecture, Architectural Boundaries, project structure, SignalR projection-nudge posture, and the open AI-response streaming transport decision.
- Loaded `prd_content` from `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`. The governed interactive surface is in MVP scope, but it must not introduce an ungoverned/freeform textbox or bypass the CommandGateway spine.
- Loaded `ux_content` from `DESIGN.md`, `EXPERIENCE.md`, and `epic10-chat-surface-elaboration.md`. Binding UX for this story is UX-DR32: progressive AI response rendering plus always-reachable Stop/Cancel, stable focus, polite live-region announcement, and reduced-motion behavior.
- Loaded persistent project-context facts from sibling `_bmad-output/project-context.md` files. Relevant facts: root-level-only submodule policy, centralized package versions, metadata-only diagnostics, read-only FrontComposer consumption, xUnit v3/Shouldly testing style, and no generated-output edits.
- Loaded previous Story 10.5 and later Story 10.7 for implementation intelligence. Story 10.5 added the governed composer and explicitly left streaming/Stop/Cancel to 10.6a/10.6b. Story 10.7 verified only the existing Stop/Cancel primitive/readiness caveat, not production streaming.

### Source Artifact Analysis

Epic 10's invariant is that the interactive chat surface is a governed write surface on the existing CommandGateway spine. Risky requests become Epic 4 approval-required proposals, never direct execution. The ADR must preserve that invariant regardless of transport choice. [Source: _bmad-output/planning-artifacts/epics.md#Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption]

Architecture currently says the platform has REST commands/queries plus SignalR projection-nudge, where clients re-query on nudge and never trust the pushed payload. The 10.6a decision is whether to extend that model for progressive AI responses or add a dedicated streaming channel. [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]

UX-DR32 is transport-neutral but behavior-specific: progressive render, always keyboard-reachable Stop/Cancel in a stable focusable position, polite "Response stopped" announcement, focus return to composer/proposal panel, reduced-motion, and no motion-only status. [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md#Streaming & interruption]

The ADR should not choose a transport by aesthetic preference. It must evaluate at least: safety authority, complexity, cross-tenant isolation, replay/reconnect behavior, stale/out-of-order partials, cancellation authority, server load/backpressure, browser/client simplicity, observability, and how 10.6b can test the behavior without trusting streamed content as durable state.

### Previous Story Intelligence

Story 10.5 created the governed composer and established these constraints:

- User messages submit through `ProjectConversationService` and `IChatBotClient.SubmitAsync(..., ChatBotSurfaceOrigin.Ui)`.
- Ask-AI submissions use the Epic 4 proposal path with `Project.AppendConversationMessage` as the intended command token.
- Durable transcript rows come only from server projection refresh; UI state may show accepted/projection-pending status but must not fake durable completion.
- Streaming transport, progressive response rendering, and production Stop/Cancel were explicitly out of scope.

Story 10.7 adds this caution:

- The existing `ChatBotStreamingStopControl.razor` is only a primitive/readiness contract. Do not treat it as production streaming completion.
- Epic 10 shipped shell adoption but left Fluent component-level remediation to Epic 12; do not use this ADR to reopen 10.1-10.5 UI component work.

Recent git history is Epic 10/11 story automation and host-reuse completion. No recent commit establishes an accepted AI streaming transport decision.

### Current Implementation State

Documentation files to create/update:

- `docs/adrs/ai-response-streaming-transport.md` - new ADR deliverable for this story.
- `_bmad-output/planning-artifacts/architecture.md` - narrow update from open decision to accepted ADR reference.

Files to read for constraints, not change in this story:

- `src/Hexalith.ChatBot.Client/IChatBotClient.cs` - typed client surface; UI/CLI/MCP use this boundary.
- `src/Hexalith.ChatBot.Client/ChatBotClient.cs` - command submissions carry `ChatBotSurfaceOrigin`; reads such as `GetProjectConversationAsync` remain typed re-query paths.
- `src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs` - composer submission seam; user message and ask-AI calls already go through `IChatBotClient`.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor` - governed composer exists and is not the streaming implementation target for this ADR story.
- `src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor` - primitive for Stop/Cancel accessibility; final transport wiring belongs to 10.6b.
- `src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs` and `ProjectConversationReducers.cs` - current projection refresh/state paths; ADR should describe how 10.6b integrates without fake durable completion.

### Project Structure Notes

- ADRs live under `docs/adrs/` using lowercase hyphenated filenames.
- Planning artifact updates should be narrow and traceable. Do not broad-edit `epics.md`, PRD files, UX files, or shipped story files for this ADR unless a direct reference fix is required.
- Production code, tests, generated files, and submodule contents should remain untouched for this decision story.
- Root-level submodule policy still applies: do not initialize/update nested submodules and do not modify `Hexalith.FrontComposer` without explicit user intent.

### Architecture and Safety Guardrails

- Do not create a second command pipeline. Streaming transport can carry transient response progress or state-change notification, but writes and cancellation authority must still resolve through the governed server path defined by the ADR.
- Do not make pushed data authoritative. The ADR must state what data is advisory, what identifier/version/correlation data is safe to carry, and which server read/query confirms durable completion.
- Do not leak raw AI provider payloads, prompts, workspace content, file contents, mailbox addresses, hidden IDs, raw exceptions, stack traces, or policy internals in streams, logs, metrics, docs examples, or tests.
- Fail closed on ambiguous tenant/session/correlation/cancellation state. If the client cannot prove it is observing the right project/tenant/session, it must stop rendering or degrade to a safe pending/unavailable state.
- Preserve cross-surface parity: CLI/MCP remain separate governed client surfaces, not visual streaming variants. Any future transport must not require CLI/MCP to bypass `IChatBotClient`.

### Latest Technical Research

No external technology research is required for this story. The decision is constrained by repo-pinned platform choices already documented in `Directory.Packages.props` and architecture: .NET 10, Blazor, FrontComposer, and the existing SignalR projection-nudge posture. Do not turn this ADR story into a package upgrade or framework migration.

### Testing Notes

- This story's required validation is documentation/source verification, not application behavior verification.
- Minimum commands:
  - `rg -n "Status|Accepted|SignalR|projection-nudge|dedicated streaming channel|never trust payload|fail-closed|CommandGateway|10.6b|Stop/Cancel|Verification" docs/adrs/ai-response-streaming-transport.md`
  - `rg -n "ai-response-streaming-transport.md|Story 10.6a|accepted ADR|AI-response streaming transport" _bmad-output/planning-artifacts/architecture.md`
  - `git diff --name-only -- src tests Hexalith.FrontComposer`
  - `git diff --check`
- If production code or tests are touched despite the intended ADR-only scope, expand verification to the narrow affected build/test lanes and explain why the code change was necessary.

#### Documented test-scope exception (justified `tests/` changes)

No production (`src/`) or FrontComposer code changed. Three `tests/` files changed; each is justified:

1. `tests/Hexalith.ChatBot.Architecture.Tests/AiResponseStreamingTransportAdrTests.cs` (new) — source-level ADR conformance guard mirroring the existing `DomainServiceSdkHostAdoptionAdrTests` convention; pins the ADR's accepted decision, safety floor, 10.6b handoff, expected-tests list, architecture link, and decision-only scope. Build + run verified: 6 tests, 0 failed.
2. `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs` (modified) — **forced** by this story: the prior `StreamingVerificationShouldRemainPrimitiveOnlyUntilStory106IsImplemented` hard-asserted `10-6a-streaming-transport-adr: backlog`, which the required `backlog → review` sprint-status transition would break. Re-scoped to allow 10.6a in `review`/`done` while 10.6b still owns production streaming. Verified: 4 tests, 0 failed.
3. `tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs` (modified) — **pre-existing Epic-11 test rot, out of this ADR's scope but repaired to keep the E2E assembly green**: the no-browser source-fallback still read `src/Hexalith.ChatBot.Server/Program.cs`, but Epic 11's SDK-host reduction moved that logic into `Gateway/ChatBotCompatibilityEndpointExtensions.cs`, `Queries/ChatBotReadQueryHandlers.cs`, and `Gateway/ChatBotCommandAdmissionPipeline.cs` (confirmed: the old strings are absent from `Program.cs`, present in the new files). Verified: 4 tests, 0 failed. Full assemblies green: Architecture 58/58 (was 57; +1 from the AI-review conformance-test addition), UI.E2E 122/122.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md]
- [Source: .agents/skills/bmad-create-story/discover-inputs.md]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 10: Interactive Chat Surface & FrontComposer Shell Adoption]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 10.6a: AI-response streaming transport ADR]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 10.6b: Streaming AI response + Stop/Cancel]
- [Source: _bmad-output/planning-artifacts/architecture.md#Surfaces]
- [Source: _bmad-output/planning-artifacts/architecture.md#Frontend Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-Hexalith.ChatBot-2026-05-28/epic10-chat-surface-elaboration.md]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md]
- [Source: _bmad-output/implementation-artifacts/10-5-governed-chat-composer.md]
- [Source: _bmad-output/implementation-artifacts/10-7-cross-surface-a11y-visual-parity-reverification.md]
- [Source: docs/adrs/domainservice-sdk-host-adoption.md]
- [Source: docs/adrs/audit-two-phase.md]
- [Source: src/Hexalith.ChatBot.Client/IChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.Client/ChatBotClient.cs]
- [Source: src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor]
- [Source: src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-19: Red check confirmed ADR verification failed before `docs/adrs/ai-response-streaming-transport.md` existed.
- 2026-06-19: Ran source-context reads for Epic 10, Stories 10.6a/10.6b, architecture Frontend/Surfaces/Boundaries, UX-DR32, and existing ADR style.
- 2026-06-19: Verification passed with required `rg` commands and clean `git diff --check`.
- 2026-06-19 (AI review correction): The original Debug Log claimed an empty `git diff --name-only -- src tests Hexalith.FrontComposer`. That claim was inaccurate — `src` and `Hexalith.FrontComposer` are clean, but `tests/` carries three changed files. The scope is restated honestly under Testing Notes → "Documented test-scope exception", and each test change was rebuilt and re-run during review (Architecture 58/58, UI.E2E 122/122).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Accepted ADR created at `docs/adrs/ai-response-streaming-transport.md`.
- Transport decision: extend the existing SignalR projection-nudge model with metadata-only AI response progress nudges.
- Rejected default: dedicated streaming channel, because it adds parallel authorization/session/reconnect/backpressure surface and increases pressure to treat pushed data as authoritative.
- Safety floor recorded: streamed/pushed data is advisory only; UI re-queries server state before durable completion claims; ambiguous tenant/session/correlation/cancellation state fails closed; tenant/authorization authority remains server-owned; cancellation stays on the CommandGateway path.
- Story 10.6b handoff recorded for progressive rendering, Stop/Cancel semantics, reconnect/resume, stale/out-of-order handling, live-region/focus behavior, and required verification.
- Architecture reference narrowed from open decision to accepted ADR link without touching production (`src/`) code, generated FrontComposer output, or the FrontComposer submodule.
- AI review (2026-06-19): added the "Tests expected for Story 10.6b" subsection to the ADR to fully satisfy AC3's "the tests expected for 10.6b" requirement, with a matching conformance-test assertion. Corrected the File List, Debug Log, and scope claims to honestly disclose the three justified `tests/` changes (no production code touched).

### File List

Deliverables:

- `docs/adrs/ai-response-streaming-transport.md` (new) — the accepted ADR.
- `_bmad-output/planning-artifacts/architecture.md` (modified) — open decision → accepted ADR reference.

Justified `tests/` changes (see Testing Notes → Documented test-scope exception):

- `tests/Hexalith.ChatBot.Architecture.Tests/AiResponseStreamingTransportAdrTests.cs` (new) — ADR conformance guard.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/Epic10ReleaseReadinessE2ETests.cs` (modified) — readiness guard re-scoped for `10-6a → review`.
- `tests/Hexalith.ChatBot.UI.E2E.Tests/DuplicateRetryFailureStatesE2ETests.cs` (modified) — pre-existing Epic-11 no-browser-fallback rot repair.

Story-automation / tracking artifacts:

- `_bmad-output/implementation-artifacts/10-6a-streaming-transport-adr.md` (this story file).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (status sync).
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (rewritten for Story 10.6a).
- `_bmad-output/implementation-artifacts/tests/test-summary-story-10.6a.md` (new per-story test summary).
- `_bmad-output/story-automator/orchestration-10-20260619-173555.md` (automator-managed orchestration state).

### Change Log

- 2026-06-19: Created accepted AI-response streaming transport ADR, updated architecture reference, proved ADR-only scope, and moved story to review.
- 2026-06-19: AI adversarial code review (story-automator). Fixed AC3 gap (added "Tests expected for Story 10.6b" to the ADR + conformance assertion); corrected false "git diff -- src tests empty" claim; completed the File List; documented the three justified `tests/` changes; re-verified all affected tests (Architecture 58/58, UI.E2E 122/122). 0 critical issues remain → status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-19 · **Outcome:** Approve (after auto-fix)

### Summary

The ADR's substance is sound: it makes a defensible, safety-preserving transport decision (extend SignalR projection-nudge with metadata-only progress nudges; reject a dedicated channel as default), preserves the never-trust-payload / fail-closed / CommandGateway invariants, and hands off 10.6b. No production (`src/`) or FrontComposer code changed — verified clean. The review found one substantive AC gap and several story-integrity defects, all auto-fixed.

### Findings and resolutions

- **[Critical → Fixed] AC3 — "tests expected for 10.6b" was missing.** AC3 requires the handoff to name the expected 10.6b tests; the ADR enumerated behaviors but not tests. Added a "Tests expected for Story 10.6b" subsection (re-query, durable-completion gate, fail-closed, Stop/Cancel governance, reconnect/resume, stale/out-of-order, metadata-only payload guard, UX-DR32 a11y) plus a conformance-test assertion.
- **[Critical → Fixed] False `[x]` task.** "Confirm `git diff -- src tests` is empty" was checked but untrue (`tests/` has 3 changed files). Re-scoped the task honestly and documented the exception.
- **[High → Fixed] False Debug Log verification claim** of an empty `git diff -- src tests`. Corrected.
- **[Medium → Fixed] File List omitted 6 of 10 changed files.** Completed.
- **[Medium → Fixed/Documented] Undisclosed test-scope drift.** Two changes are justified (Epic 10 readiness guard *must* change for `10-6a → review`; ADR conformance test follows the existing `DomainServiceSdkHostAdoptionAdrTests` convention). One (`DuplicateRetryFailureStatesE2ETests`) repairs **pre-existing Epic-11 no-browser-fallback rot** unrelated to this ADR — kept (reverting would re-break a test, since `Program.cs` no longer holds the asserted strings) and explicitly documented as out-of-scope-but-correct.
- **[Low → Fixed] ADR Verification section** restated the inaccurate "src tests must be empty" claim; corrected to the production-only scope plus the documented test exception.

### Verification performed during review

- `git diff --name-only -- src Hexalith.FrontComposer` → empty (production untouched); `git diff --check` → clean.
- Built + ran affected tests independently: `AiResponseStreamingTransportAdrTests` 6/6, `Epic10ReleaseReadinessE2ETests` 4/4, `DuplicateRetryFailureStatesE2ETests` 4/4; full assemblies Architecture 58/58, UI.E2E 122/122 — all 0 failed.
- Confirmed the Epic-11 refactor moved the asserted strings out of `Program.cs` into the three Server files the repaired E2E now reads.
