# Sprint Change Proposal — ChatBot: wire AC1 AI-response progress producer + rich nudge transport (Story 10.6b completion)

- **Date:** 2026-06-20
- **Author:** Jerome (Senior Developer review of Story 10.6b)
- **Repo:** `Hexalith.ChatBot` (driving/integration repo)
- **Driving change:** Epic 10, Story 10.6b — "Streaming AI response + Stop/Cancel" — carried **CRITICAL AC1** producer/transport gap
- **Scope classification:** **Major** (multi-component feature across 3 repos + ADR amendment; integration owner)
- **Release order:** **3 of 3** — depends on published EventStore (proposal 1) and FrontComposer (proposal 2) package versions.
- **Companion proposals:**
  - `Hexalith.EventStore/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-20-ai-response-progress-transport.md`
  - `Hexalith.FrontComposer/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-20-ai-response-progress-transport.md`

---

## Section 1 — Issue Summary

Story 10.6b has been through 5 adversarial review passes and is held `in-progress` because **AC1 (progressive rendering) is non-functional end-to-end**:

1. **No producer.** Nothing writes a **non-terminal** `AiResponseProgress` to the conversation projection. `GovernedOperationAggregate` / the AiOutcome projection only persist **terminal** outcomes, so `BuildAiResponseProgress()` never surfaces `pending`/`rendering`, and the UI's `ActiveStreamingProgress` is always null (which also makes AC3's Stop control never enable end-to-end).
2. **No transport.** `ProjectConversationAiResponseNudgeReceivedAction` is defined/reduced/handled but **never dispatched in `src/`** (tests only). There is no SignalR receiver in `Hexalith.ChatBot.UI` and no broadcaster wiring in `Hexalith.ChatBot.Server`.
3. **Reuse target is signal-only.** The ADR-mandated channel (EventStore `IProjectionChangedBroadcaster` / FrontComposer `IProjectionChangeNotifier`) carries only `(projectionType, tenantId)` and cannot carry the rich nudge nor scope to a conversation.

The selected resolution (rich nudge channel) is being delivered by the two companion framework proposals. **This proposal wires the producer + transport in ChatBot, activates the shipped rich-nudge gate end-to-end, amends the ADR, and closes 10.6b.**

> The carried `chatbot-story-10-6b-carried-findings` note warns against *auto-"fixing"* the CRITICAL inside a review (that would fabricate a feature). This proposal is the opposite: a planned, user-authorized, fully-built multi-component feature — not a review patch.

## Section 2 — Impact Analysis

- **Epic impact:** Epic 10 — Story 10.6b moves to `review`/`done` once this lands. The Epic 10 readiness guard (`Epic10ReleaseReadinessE2ETests`) asserts on 10.6b status and **will need an update** when status moves (see `chatbot-epic10-readiness-guard-status-coupling`).
- **Story impact:**
  - **10.6b** — completed (producer + transport + activated gate + tests).
  - Resolves the carried **MEDIUM** (effect/reducer divergence) — now integration-testable with a real transport.
  - The carried **LOW** (cancellation aggregate dedups by `CancellationId`, doesn't confirm in-flight) may be folded in or kept as a follow-up.
- **Artifact conflicts:**
  - **ADR** `docs/adrs/ai-response-streaming-transport.md` — **amend** to authorize the metadata-rich, scope-aware projection-nudge extension (the ADR's own "reconsider only with a separate ADR + proven need" clause). See 4.6.
  - `_bmad-output/planning-artifacts/architecture.md` — Frontend/transport section updated for the producer + scoped nudge.
  - `Directory.Packages.props` + submodule pointers — bump to the new EventStore/FrontComposer versions.
- **Technical impact (files):**
  - **Server (producer):** `GovernedOperationAggregate` / `GovernedOperationState`, a new non-terminal progress event (e.g. `AiResponseProgressObserved`), `AiOutcomeProjectionTranslator` / `AiOutcomeEventView` / `PublishedAiOutcomeEvent`, `ProjectConversationItemView` (already maps progress).
  - **Server (broadcast):** `Program.cs` (enable `EventStore:SignalR:Enabled`, register SignalR broadcaster, `MapHub`), a thin `ChatBotAiResponseProgressBroadcaster` that calls EventStore's detail broadcaster on conversation-projection progress change.
  - **UI (receiver):** new `ChatBotProjectConversationStreamingSubscription` using FrontComposer's `IProjectionChangeDetailNotifier`; join the project/conversation-scoped group on conversation load; map detail → `ProjectConversationAiResponseNudgeModel`; dispatch the action.
  - **UI (gate fix):** `ProjectConversationEffects.HandleAiResponseNudgeAsync` apply the reducer's fail-closed gate (inject post-reducer `IState`).
  - **Tests:** end-to-end transport, reconnect-scoped, stale/out-of-order, metadata-only guard, architecture boundary, readiness-guard update.

## Section 3 — Recommended Approach

**Direct Adjustment — complete 10.6b as a coordinated 3-repo feature.** No rollback (the shipped contracts/UI/projection are correct and reused); no MVP cut (AC1 is the story's headline).

- **Effort:** ~L (1–1.5 dev-weeks in ChatBot **after** the two framework versions publish), dominated by the producer (non-terminal progress lifecycle) and end-to-end transport tests.
- **Risk:** Medium. Key risks: (a) producer must stay **metadata-only** (no chunks/text into events or the nudge map); (b) version coordination across 3 repos; (c) keep UI architecture boundary intact (UI references Client/ServiceDefaults/FrontComposer only, **not** Server). All covered by tests/guards.
- **Sequencing:** Land **after** EventStore + FrontComposer publish. Do **not** start the ChatBot package bump until both are pinned.

## Section 4 — Detailed Change Proposals

### 4.1 Producer — emit non-terminal AI response progress (the genuine missing capability)

```
OLD (effect): the AI generation lifecycle persists only terminal AiOutcome events;
              ProjectConversationItemView.AiResponseProgressState is only ever a terminal value.

NEW: emit a metadata-only non-terminal progress event as generation advances, e.g.
     AiResponseProgressObserved(projectId, conversationId, responseId, generationId,
                                correlationId, sequence, state /* pending|rendering|cancelling */),
     projected into PublishedAiOutcomeEvent so BuildAiResponseProgress() surfaces
     pending/rendering with a monotonic Sequence + SourceVersion.
```

**Rationale:** without non-terminal progress, `ActiveStreamingProgress` stays null and AC1/AC3 cannot run. **Constraint:** the event and projection carry **no** response text, provider chunks, prompts, workspace/file/mailbox content, policy internals, exceptions, or stack traces — `RedactionState`/`VisibilityState` stay `metadata_only`.

### 4.2 Server broadcast wiring (`Program.cs` + broadcaster)

- Set `EventStore:SignalR:Enabled = true` for the ChatBot host; register `SignalRProjectionChangedBroadcaster`; `MapHub<ProjectionChangedHub>(ProjectionChangedHub.HubPath)`; configure Redis backplane for multi-instance.
- Add `ChatBotAiResponseProgressBroadcaster`: on conversation-projection AI-progress change, call EventStore's **detail** broadcaster (proposal 1) with:
  - `ProjectionType` = the ProjectConversation projection type name,
  - `TenantId` = tenant,
  - `GroupScope` = `{projectId}:{conversationId}` (conversation-scoped),
  - `Metadata` = `{ responseId, generationId, correlationId, sourceVersion, sequence, state, redactionState=metadata_only, visibilityState=metadata_only }`.
- Authorization: hub `[Authorize]` + `ITenantValidator` enforce tenant; project/conversation scope authorized server-side before join (ChatBot supplies the authorized scope to the UI on conversation load).

### 4.3 UI receiver (`Hexalith.ChatBot.UI`)

```
NEW component/service: ChatBotProjectConversationStreamingSubscription
  - On LoadProjectConversation success, JoinGroupAsync(projectionType, tenantId, scope="{projectId}:{conversationId}")
    via FrontComposer ProjectionSubscriptionService.
  - Subscribe IProjectionChangeDetailNotifier.ProjectionChangedDetail; filter to this conversation's scope;
    map metadata -> ProjectConversationAiResponseNudgeModel;
    dispatch ProjectConversationAiResponseNudgeReceivedAction(model).
  - On conversation switch/dispose, LeaveGroupAsync the prior scope. Reconnect/rejoin handled by the Shell.
```

This is the line that finally dispatches the action in `src/`. The shipped reducer `IsAcceptableNudge` (cross-project, metadata-only, version/sequence monotonic) and the re-query effect now run end-to-end.

### 4.4 Gate fix — resolve the carried MEDIUM (effect/reducer divergence)

```
OLD (ProjectConversationEffects.HandleAiResponseNudgeAsync):
  re-queries on ANY structurally-metadata-only nudge using the nudge's own ProjectId,
  with no staleness/cross-project gate (reducer gates; effect does not).

NEW:
  inject post-reducer IState<ProjectConversationState>; only dispatch LoadProjectConversationAction
  when the reducer ACCEPTED the nudge (same fail-closed gate), so stale/cross-project nudges
  never trigger a re-query.
```

### 4.5 Tests (ChatBot)

- **Producer:** non-terminal `pending`→`rendering` progress is projected and surfaced by the read endpoint with monotonic sequence/version.
- **End-to-end transport (integration):** progress change → detail broadcast → UI receive → reducer gate → typed re-query → render server state only.
- **Reconnect:** rejoin the **conversation-scoped** group; missed nudges don't corrupt state.
- **Stale/out-of-order & cross-tenant/cross-project:** ignored / fail-closed (both reducer and effect now).
- **Metadata-only guard:** nudge metadata map carries only ids/version/sequence/state; never content.
- **Architecture:** UI still references Client/ServiceDefaults/FrontComposer only — **not** Server/gateway/DAPR; nudges stay advisory/non-authoritative.
- **Readiness guard:** update `Epic10ReleaseReadinessE2ETests` for the 10.6b status move (expected per `chatbot-epic10-readiness-guard-status-coupling`).

### 4.6 ADR amendment — `docs/adrs/ai-response-streaming-transport.md`

```
ADD a "Amendment 2026-06-20 (Story 10.6b implementation)" subsection under Decision:

  The projection-nudge model is extended, additively, to carry a BOUNDED, OPAQUE,
  METADATA-ONLY detail map and an optional conversation-level GROUP SCOPE, delivered over
  the existing EventStore ProjectionChangedHub (no second/dedicated channel).

  Proven need (the ADR's bar for revisiting): the client stale/out-of-order fail-closed gate
  (AC5) and conversation-scoped rejoin require per-nudge response/generation id + version +
  sequence + state, which the signal-only (projectionType, tenantId) message cannot provide,
  and tenant-only groups would force tenant-wide re-query.

  Unchanged invariants: nudges remain ADVISORY; the UI still re-queries typed server state and
  renders only server-returned data; the detail map is metadata-only (ids/version/sequence/state),
  never response text/chunks/prompts/content; CommandGateway remains the only write/cancel path;
  completion/stopped/cancelled/failed/unavailable remain durable only after a server read.
```

Also update the ADR `Alternatives Considered` to record that the "dedicated streaming channel" remains rejected — this amendment extends the **existing** projection-nudge channel, it does not add a new one.

### 4.7 Version coordination

- After EventStore (proposal 1) + FrontComposer (proposal 2) publish: bump `Directory.Packages.props` pins and the two submodule pointers; rebuild `Hexalith.ChatBot.slnx`; confirm 0 warnings (warnings-as-errors).

## Section 5 — Implementation Handoff

- **Scope:** Major. Route to **DEV (integration owner)** with **Architect** sign-off on the ADR amendment and **PM/PO** awareness that 10.6b completion is gated by two upstream framework releases.
- **Deliverables:** non-terminal progress producer, server SignalR broadcast wiring, UI scoped-group receiver, effect gate fix, ADR amendment, full end-to-end test evidence, readiness-guard update, package/submodule bumps, Story 10.6b set to `review`→`done`.
- **Success criteria:**
  1. With both framework versions pinned, AC1 runs end-to-end: a generating response renders progressively from server reads, the Stop control enables (AC3), and a verified stop announces politely (AC4) — all proven by integration tests, not source-scan fallbacks.
  2. Stale/out-of-order/cross-tenant/cross-project nudges fail closed in **both** reducer and effect.
  3. No content leaves the server in events, the nudge map, logs, metrics, or fixtures.
  4. UI architecture boundary intact; all six suites green; Epic 10 readiness guard updated and green.
- **Blocking dependencies:** EventStore proposal 1 **published**; FrontComposer proposal 2 **published**. Do not begin the ChatBot package bump before both are pinned.
- **Do NOT:** introduce a dedicated/content-bearing channel; make the nudge authoritative; let the UI reference Server/gateway internals; mark 10.6b done before the end-to-end (non-fallback) tests pass.
