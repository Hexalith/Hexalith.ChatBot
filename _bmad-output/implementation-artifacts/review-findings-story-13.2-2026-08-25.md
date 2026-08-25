# Code Review Findings — Story 13.2 (canonical)

Story key: `13-2-work-converse-and-interrupt-ai-safely-in-project-context`
Spec: `_bmad-output/planning-artifacts/epics.md` §Story 13.2 (line 3266)
Binding context: `ux-designs/ux-Hexalith.ChatBot-2026-05-28/EXPERIENCE.md`, `implementation-conformance-addendum-2026-07-17.md`
Date: 2026-08-25
Target: full file contents at HEAD `04de441` (no diff — re-acceptance of committed work)
Layers run: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor (all completed; none failed)

## Review Findings

### Decision needed

- [ ] [Review][Decision] No live-route evidence exists for any Story 13.2 behavior — Every Stop/Cancel, composer and state-mapping assertion is either a source-text `ShouldContain` scan or a `Page.SetContentAsync()` fixture the test authors itself. The only real-render lane (`tests/Hexalith.ChatBot.UI.E2E.Tests/RealRenderCrossSurfaceE2ETests.cs`) stubs `IChatBotClient` with `FakeChatBotClient`, whose `GetProjectConversationAsync` returns `Items = []`, so the live route never renders an AI-response progress row, never exercises Stop/Cancel and never reaches CommandGateway. `ProjectWorkspaceE2ETests` fixtures are additionally stale (hand-written `.chatbot-status` chrome that Epic 13 removed). The epic's binding rule states a story "cannot close from source scans, static fixtures, screenshots without direct invariant assertions, or a diagnostic fallback. Its primary live route must execute successfully." This governs whether 13.2 can close at all.
- [ ] [Review][Decision] Cancel racing terminal completion permanently disables Stop, but the behavior is pinned by an explicit test — `ProjectConversationReducers.cs:32-46` clears `IsCancellingAiResponse` only on `IsVerifiedStop` (`stopped`/`cancelled`). A generation landing as `completed`/`failed`/`unavailable` leaves the flag set forever, and `ChatBotProjectConversationWorkspace.razor:119` binds it to `Disabled`, so no subsequent generation can be stopped for the life of the circuit. `ProjectConversationEffectsTests.cs:245-248` deliberately asserts the terminal-`Failed` case keeps it `true`, with a rationale comment. Fixing this contradicts a test that encodes intent.
- [ ] [Review][Decision] Hard-coded fixture projects ship on the production landing route — `ProjectWorkspace.razor:107-112` is a `static readonly` array of `("project-alpha", "Alpha project", ...)`, `Beta`, `Gamma`, rendered as *authorized recents* with live deep links, untranslated English names, and no tenant or authorization filtering. AC1 requires the live route show correct governed state with "no marketing or ungoverned-chat fallback". No governed authorized-recents source exists to replace it.
- [ ] [Review][Decision] SignalR nudge transport is inert in any JWT-on deployment — `ChatBotHubEndpoint.AccessTokenProvider` is never supplied at the only registration site (`Program.cs:28-29` passes the base address only), and `ChatBotProjectConversationHub.IsTenantAuthorized` returns `!ChatBotJwtAuthentication.IsConfigured(...)` for anonymous callers. With JWT on, `JoinTenant` throws `tenant-forbidden`, `JoinTenantAsync` swallows it, and AC3's nudge path silently never fires. With JWT off, any anonymous client may join any tenant group. The type's own doc comment defers this to "the hosting deployment".
- [ ] [Review][Decision] The client declares its own risk classification and requester authority — `ProjectConversationService.SubmitAskAiAsync` hand-builds `RiskClassification` with `AiActionRiskClass.ApprovalRequired`, `RequesterAuthorityClass: "authorized"`, `"project-contributor"`, `TenantPolicyClassification: "approval-required"` and the allowlist version. `GovernedOperationAggregate.IsSafeClassification` (line 4670) validates token *shape* only — it never re-derives risk from server policy and never compares `RequesterAuthorityClass` to the caller's claims. Today's client declares the more restrictive value, so this is not self-escalation, but a client sending `LowRisk` is accepted. Whether that actually bypasses the Epic 4 approval path needs tracing.

### Patch — high

- [x] [Review][Patch][APPLIED] Submitting on an empty project crashes the Blazor circuit [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:411]
- [x] [Review][Patch][APPLIED] Every re-query blanks the whole conversation subtree and closes the Why panel [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationReducers.cs:11-24]
- [x] [Review][Patch][APPLIED] "Response stopped" is never announced and focus never returns after a real stop [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor:113-137]
- [ ] [Review][Patch] Cancel handler validates none of the invariants AC3 enumerates (no active-state, no identity binding, no real expected-version check) [src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs:1648-1692]
- [x] [Review][Patch][APPLIED] No per-project authorization for CancelAiResponseGeneration or RecordProjectConversationMessage [src/Hexalith.ChatBot.Server/Gateway/Stages/ParticipantAuthorizationStage.cs:512-520]
- [ ] [Review][Patch] Projection fabricates a terminal "stopped" state from the cancellation *request*, making the client's server-verification circular [src/Hexalith.ChatBot.Server/Projections/TaskIntentProjectionHandler.cs:24-47]
- [x] [Review][Patch][APPLIED] Rejected submissions are invisible: SubmissionErrorCode has zero readers, and stale PendingSubmission still renders "accepted" [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationState.cs:10]
- [x] [Review][Patch][APPLIED] Composer discards the user's text before admission is known [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:164-171]
- [x] [Review][Patch][APPLIED] Every load failure renders as "Access blocked"/Denial — degraded and retryable states are collapsed into unauthorized, with no retry affordance [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:54-68]
- [x] [Review][Patch][APPLIED] Stale subscription closure after an in-tenant project switch: nudges are silently dropped and a reconnect loads the previous project's conversation [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationStreamingSubscriber.cs:46-49]
- [x] [Review][Patch][APPLIED] Nudge dedup permanently suppresses re-queries on conversations with no AI-progress rows — ordinary changes refresh once per page load [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs:117,148-168]
- [x] [Review][Patch][APPLIED] Stop control uses native `Disabled`, so when idle it is neither focusable nor explained — contradicting the module's own accessibility floor [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor:13-27]
- [ ] [Review][Patch] Conversation history is unreachable: NextCursor/HasMore are modelled and populated but never consumed, and the concurrency token derives from one page [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationModels.cs:10-12]
- [ ] [Review][Patch] Missing tests: aggregate cancel handler (zero coverage), composer message-vs-AskAi branch, subscriber tenant filter and reconnect callback [tests/Hexalith.ChatBot.UI.Tests/ProjectConversationEffectsTests.cs]

### Patch — medium

- [x] [Review][Patch][APPLIED] A failed initial hub connect permanently disables streaming (state assigned before StartAsync; catch swallows) [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationStreamingSubscriber.cs:46-49,89-98]
- [x] [Review][Patch][APPLIED] Unrecognized/renamed server Status silently renders as a healthy active conversation [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:335-360]
- [x] [Review][Patch][APPLIED] Composer aria-describedby points at an id that is never rendered, and the validation error id is not in the list [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor:59]
- [x] [Review][Patch][APPLIED] Correlation id is millisecond-resolution and project-scoped, so two sends in the same millisecond collide and the second is dropped as a replay [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs:56]
- [x] [Review][Patch][APPLIED] StreamingNotice "reconnected" is never cleared by a reload, so a genuine terminal state renders as "reconnected" [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationReducers.cs:159-164]
- [x] [Review][Patch][APPLIED] Loading state has no skeleton and never sets or clears aria-busy [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:43-52]
- [x] [Review][Patch][APPLIED] "Project switch success" banner is shown and announced on any first load of `/` [src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor:13]
- [x] [Review][Patch][APPLIED] Math.Max(1, expectedSourceVersion) fabricates a concurrency assertion the client never read, diverging from the sibling path on the same button [src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs:222]
- [x] [Review][Patch][APPLIED] Cross-project nudge guard is skipped whenever Conversation is null — which ReduceLoad makes true on every re-query [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationReducers.cs:260-266]
- [x] [Review][Patch][APPLIED] AiResponseStatusText maps any unrecognized state, including a future terminal one, to "streaming progress" — failing open [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:427-468]
- [x] [Review][Patch][APPLIED] Eyebrow="S1" ships an internal slice token as visible untranslated UI text [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:28]
- [x] [Review][Patch][APPLIED] chatbot-streaming-status and chatbot-muted-text are applied with no CSS behind them; the first carries AC4's visible-text progress requirement [src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css]
- [x] [Review][Patch][APPLIED] Unbounded re-dispatch of LoadProjectConversationAction while a load is failing or in flight [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:243-250]
- [x] [Review][Patch][APPLIED] EnsureSubscribedAsync is re-entrant across overlapping renders and can build multiple HubConnections, leaking all but the last [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:265-286]
- [ ] [Review][Patch] Broad catch (Exception) reports client-side programming errors as service outages, indistinguishable from a network failure [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs:38-41]
- [ ] [Review][Patch] Unsalted SHA-256 of normalized message text is persisted as metadata-only evidence; trivially reversible for short messages [src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs:624-628]
- [x] [Review][Patch][APPLIED] aria-label applied to role-less divs is dropped by assistive tech, so the recents/files/region names do not exist at runtime [src/Hexalith.ChatBot.UI/Components/Pages/ProjectWorkspace.razor:50]
- [x] [Review][Patch][APPLIED] ChatBotProjectContextHeader emits an h2 before the FcPageHeader heading, inverting heading order on both routes [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectContextHeader.razor]
- [x] [Review][Patch][APPLIED] Attachments render twice — once in the stream and again in the Files accordion, duplicating stable ids and accessible names [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor]
- [x] [Review][Patch][APPLIED] Cancellation idempotency key derives from server data the client does not uniquely control; a NoOp is indistinguishable from success [src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs:272-298]

### Deferred (pre-existing / owned by another story)

- [x] [Review][Defer] .chatbot-conversation-shell hand-rolled chrome still wraps the FcPageLayout composition; forced grid fights FluentStack flex and a 70rem cap contradicts FullWidth [src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css:59-99,279-283] — deferred, pre-existing (Epic 13 frame is Story 13.1, currently `done`)
- [x] [Review][Defer] Hard-coded element ids make the conversation components single-instance-only [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedComposer.razor] — deferred, pre-existing
- [x] [Review][Defer] SafeLocale can return an empty string for an all-punctuation locale [src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs:638-641] — deferred, pre-existing, unreachable from CurrentUICulture.Name
- [x] [Review][Defer] WireToken falls back to a numeric ordinal for an undefined enum value [src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs:604-619] — deferred, pre-existing
- [x] [Review][Defer] Fire-and-forget hub dispatches can outlive the component and throw unobserved ObjectDisposedException [src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor:276,283] — deferred, pre-existing
- [x] [Review][Defer] ReduceFailed writes SubmissionErrorCode from a load error and StreamingErrorCode into unreachable dead state [src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationReducers.cs:50-70] — deferred, pre-existing


## Application record (2026-08-25)

**28 of 34 patches applied.** Remaining 6 are recorded below with why they were not applied.

### Escalated to decision-needed (cannot be patched — the state they must validate does not exist)

`src/Hexalith.ChatBot.Server/Governance/Conversations/` contains exactly two events
(`AiResponseGenerationCancellationRequested`, `ProjectConversationMessageAppended`), and AI-response progress states
are written by projection translators (`LowRiskAiOutcomeProjectionTranslator`), never by the aggregate. The governed
aggregate therefore holds no AI-generation lifecycle state at all.

- [ ] [Review][Decision] Cancel handler cannot validate active state / identity / expected version — there is no
  generation state in `GovernedOperationState` to validate against. Requires introducing AI-generation lifecycle
  events into the governed spine. `GovernedOperationAggregate.cs:1648-1692`.
- [ ] [Review][Decision] Projection fabricates terminal "stopped" from the cancellation request — cannot be fixed
  without the same lifecycle events; there is no signal today that generation actually stopped.
  `TaskIntentProjectionHandler.cs:24-47`.

### Not applied — needs product/design input

- [ ] [Review][Decision] Conversation history unreachable (`NextCursor`/`HasMore` never consumed). Applying this means
  designing a "load older" / "new updates" affordance and its focus and announcement behavior, not a code fix.

### Reclassified after reading the code

- [x] [Review][Defer] Broad `catch (Exception)` reports client-side defects as service outages — NOT changed.
  `ProjectConversationEffectsTests.StopEffectShouldCollapseUnknownFailuresToTheGenericSafeCodeWithNoRawText`
  deliberately asserts that an `InvalidOperationException` collapses to the generic code with no raw exception text.
  The broad catch is a redaction control, not an oversight; the real gap is telemetry, which is out of this scope.
- [x] [Review][Defer] Unsalted SHA-256 message fingerprint — NOT changed. Salting it would destroy its evidentiary
  purpose (a content fingerprint the server can correlate), so this is a governance decision, not a patch.
- [ ] [Review][Patch] Subscriber tenant-filter / reconnect callback tests — not added. The suggested shape drives
  `ProjectConversationStreamingSubscriber` against the test-server hub in `ChatBotProjectConversationHubE2ETests`;
  blocked on the UI project building (see below).

### Tests added

- `tests/Hexalith.ChatBot.Server.Tests/Operations/CancelAiResponseGenerationAggregateTests.cs` — 10 tests, **all
  passing**. Full server suite: **1792/1792 passing** with the new authorization gate in place.
- `ProjectConversationEffectsTests` — composer message-vs-AskAi routing, blank-text rejection, per-submission
  correlation-id uniqueness, no-progress signal re-query, durable verified-stop announcement token.
- `ProjectConversationStateTests` — same-project reload preserves conversation and Why panel; cross-project nudge
  fails closed while nothing is loaded; the old test that pinned the blanking defect was rewritten.

### Verification status

- Server: **built and fully green (1792/1792).**
- UI: **NOT verified.** `Hexalith.ChatBot.UI` does not currently build, for a reason unrelated to this review — a
  concurrent session is mid-edit on the Story 13.3 Association Review surface in the same project (modified
  `AssociationReview*` files plus three untracked new files, written 23:02-23:05). The only remaining compile error is
  `AssociationReview.razor(198,36): PreviewAssociationDecisionAction could not be found`, in that changeset.
  Zero compile errors reference any file patched here. The UI unit tests could not be run.

## Decision resolution (2026-08-26)

All 6 decisions resolved to the recommended option. Outcome:

### Applied and verified (server — 1800/1800 passing)

- **[D6] Governed generation lifecycle tracking.** The lifecycle events already existed
  (`LowRiskAiAssistanceExecutionStarted` -> non-terminal "rendering"; Succeeded/Failed/RoutedToApproval -> terminal);
  the aggregate simply never tracked them. `GovernedOperationState` now keeps `ActiveAiResponseGenerations` keyed
  `{responseId}:{generationId}` (proposal id : execution id), populated on start and removed on any terminal outcome.
  `Handle(CancelAiResponseGeneration)` now fails closed on: unknown or already-terminal generation
  (`ai_response_generation_not_active`), a project that does not match the generation
  (`ai_response_cancellation_project_mismatch`), and a version below the generation baseline
  (`ai_response_cancellation_stale_version`). 8 new tests.
- **[D5] Risk-classifier input precedence.** `DeterministicAiActionRiskClassifier.BuildProposalInput` now takes server
  metadata over client-supplied `EffectSurface` / `TenantPolicyClassification` / `CommandDefaultRisk` /
  `CommandAllowlistVersion`. `ProposedActionClasses` is a **union**, not an override: a client may escalate risk but can
  never de-escalate by omitting a class. (A straight inversion was wrong and a test caught it.)

### Applied, NOT verified (UI project does not build — see below)

- **[D2] Cancel racing terminal completion.** `ReduceLoaded` now clears cancellation tracking when the tracked
  generation reaches ANY terminal state, while publishing the announcement token only for a verified stopped/cancelled.
  Ends the permanently disabled Stop control. `ProjectConversationEffectsTests` realigned.
- **[D3] Fabricated authorized recents removed** from `ProjectWorkspace.razor`, with the now-dead `CodeRow` helper and
  `ProjectWorkspaceAuthorizedRecentProject` record. `ProjectWorkspaceRouteContractTests` previously *required* the
  fixture to be present; it now asserts it stays gone.

### Blocked — could not be implemented as chosen

- **[D4] Wire the hub access-token provider — BLOCKED.** The chosen option assumes a token exists to forward. It does
  not: `Hexalith.ChatBot.UI` has **no authentication whatsoever** (no `AddAuthentication`, no `HttpContext` token
  access; even the typed `IChatBotClient` carries no bearer token). The fallback guard is also not implementable from
  the UI, because `ChatBotJwtAuthentication.IsConfigured` lives in the Server and `Program.cs` states the UI "never
  references the Server". Giving the UI host an identity is a hosting/identity change beyond this review, and it must
  land before the provider can be wired.
- **[D1] Real-render lane — BLOCKED** on the UI project building.
- **[D7 residual] The projection still writes terminal "stopped" from the cancellation request.** Flipping it to a
  non-terminal "cancelling" would strand the UI in "cancelling" forever, because nothing emits a terminal cancelled
  event once an execution is actually stopped -- that needs executor cancellation support. **However, D6 removes the
  harm that made this severe:** a cancellation naming a response/generation that never existed, or one already
  terminal, is now rejected before any event is emitted, so terminal state can no longer be fabricated for a
  non-existent generation.
- **Disagreement recording — not implemented deliberately.** `AiActionRiskClassifierDisagreementRecorded` is shaped for
  a *human reviewer* disagreeing with the classifier (`ReviewerActorId`, `ReviewerDecision`, `Resolution`), not for a
  client-declared-vs-server-derived mismatch. Reusing it would corrupt the event model. Note also that
  `AcceptedCommandDispatcher.cs:619` already overwrites the client's declared record with the server-derived one, so a
  disagreement is neutralized at admission; recording it is observability and wants a purpose-built record.

### UI build blocker (not caused by this review)

`Hexalith.ChatBot.UI` fails on `AssociationReview.razor(198,36): PreviewAssociationDecisionAction could not be found`,
from a concurrent session's in-flight Story 13.3 work (modified `AssociationReview*` sources plus new untracked files
and test files, still changing during this session). Zero compile errors reference any file touched by this review.
