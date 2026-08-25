using Fluxor;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.ProjectConversation;

// The fixtures below build the generated read-model types (ProjectConversationResponse / AiResponseProgress), so every
// read-model enum resolves to the generated client. Only ChatBotSurfaceOrigin is contract-only (the IChatBotClient
// command seam), so it is aliased from the contract enums namespace instead of re-importing that whole namespace,
// which would otherwise collide with the generated read-model enums of the same name.
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Behavioral coverage for the project-conversation streaming store: it drives the real
/// <see cref="ProjectConversationEffects"/>, <see cref="ProjectConversationService"/>, and
/// <see cref="ProjectConversationReducers"/> rather than scanning source. It proves the governed Stop/Cancel
/// flow (pending -> governed submit -> typed re-query, never a local "stopped" claim), the metadata-only
/// nudge / reconnect re-query, and the server-verified terminal gate (cancelling stays true until a typed read
/// reports a terminal stopped/cancelled state for the same response and generation). [AC2, AC3, AC5]
/// </summary>
public sealed class ProjectConversationEffectsTests
{
    private const string ResponseId = "proposal-001";
    private const string GenerationId = "operation-001";

    [Fact]
    public async Task StopEffectShouldGoPendingThenGovernedSubmitThenTypedRequeryWithoutClaimingStoppedLocally()
    {
        StubChatBotClient client = new();
        ProjectConversationEffects effects = new(new ProjectConversationService(client), EmptyState());
        RecordingDispatcher dispatcher = new();

        await effects.HandleStopAiResponseAsync(
            new StopProjectConversationAiResponseAction(ActiveProgress()),
            dispatcher);

        // Order matters: optimistic pending -> accepted receipt -> typed re-query.
        dispatcher.Actions[0].ShouldBeOfType<ProjectConversationAiResponseCancellationPendingAction>();
        dispatcher.Actions[1].ShouldBeOfType<ProjectConversationAiResponseCancellationAcceptedAction>();
        dispatcher.Actions[2].ShouldBeOfType<LoadProjectConversationAction>().ProjectId.ShouldBe("project-001");

        ProjectConversationAiResponseCancellationPendingAction pending =
            dispatcher.Actions.OfType<ProjectConversationAiResponseCancellationPendingAction>().Single();
        pending.ResponseId.ShouldBe(ResponseId);
        pending.GenerationId.ShouldBe(GenerationId);

        // The cancellation went through the governed client with UI origin as a CancelAiResponseGeneration command.
        client.LastSubmitOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        client.LastSubmittedCommand.ShouldBeOfType<CancelAiResponseGeneration>();

        // The effect never fabricates a terminal/stopped state; that is left to the typed re-query (the gate test below).
        dispatcher.Actions.ShouldNotContain(static action => action is ProjectConversationLoadedAction);
    }

    [Fact]
    public async Task StopEffectShouldSurfaceServerProblemCodeAndNotRequeryOrAcceptOnFailure()
    {
        StubChatBotClient client = new()
        {
            SubmitException = new HexalithChatBotApiException<ProblemDetails>(
                "Metadata-only authorization denial.",
                403,
                response: null,
                headers: new Dictionary<string, IEnumerable<string>>(),
                result: new ProblemDetails { Code = "authorization_denied" },
                innerException: null),
        };
        ProjectConversationEffects effects = new(new ProjectConversationService(client), EmptyState());
        RecordingDispatcher dispatcher = new();

        await effects.HandleStopAiResponseAsync(
            new StopProjectConversationAiResponseAction(ActiveProgress()),
            dispatcher);

        dispatcher.Actions[0].ShouldBeOfType<ProjectConversationAiResponseCancellationPendingAction>();
        ProjectConversationAiResponseCancellationFailedAction failed =
            dispatcher.Actions.OfType<ProjectConversationAiResponseCancellationFailedAction>().Single();
        failed.ErrorCode.ShouldBe("authorization_denied");
        dispatcher.Actions.OfType<ProjectConversationAiResponseCancellationAcceptedAction>().ShouldBeEmpty();
        dispatcher.Actions.OfType<LoadProjectConversationAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task StopEffectShouldCollapseUnknownFailuresToTheGenericSafeCodeWithNoRawText()
    {
        StubChatBotClient client = new()
        {
            SubmitException = new InvalidOperationException("raw /home/secret exception text"),
        };
        ProjectConversationEffects effects = new(new ProjectConversationService(client), EmptyState());
        RecordingDispatcher dispatcher = new();

        await effects.HandleStopAiResponseAsync(
            new StopProjectConversationAiResponseAction(ActiveProgress()),
            dispatcher);

        ProjectConversationAiResponseCancellationFailedAction failed =
            dispatcher.Actions.OfType<ProjectConversationAiResponseCancellationFailedAction>().Single();
        failed.ErrorCode.ShouldBe(ProjectConversationEffects.GenericFailureCode);
        failed.ErrorCode.ShouldNotContain("raw", Case.Insensitive);
        failed.ErrorCode.ShouldNotContain("/home/", Case.Insensitive);
        failed.ErrorCode.ShouldNotContain("exception", Case.Insensitive);
    }

    [Fact]
    public async Task StopEffectShouldRethrowCancellationAndNeverCollapseItToAFailure()
    {
        StubChatBotClient client = new() { SubmitException = new OperationCanceledException() };
        ProjectConversationEffects effects = new(new ProjectConversationService(client), EmptyState());
        RecordingDispatcher dispatcher = new();

        await Should.ThrowAsync<OperationCanceledException>(() => effects.HandleStopAiResponseAsync(
            new StopProjectConversationAiResponseAction(ActiveProgress()),
            dispatcher));

        dispatcher.Actions.OfType<ProjectConversationAiResponseCancellationFailedAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task NudgeEffectShouldRequeryIffReducerAcceptedTheNudgeSoEffectAndReducerAgree()
    {
        // The MEDIUM fix: the effect no longer applies its own gate; it re-queries IFF the reducer (which runs first in
        // Fluxor) accepted this exact nudge. Drive the REAL reducer, then the effect against the post-reducer state, and
        // prove they agree for a safe nudge (re-query) and a non-metadata-only nudge (reject, no re-query).
        ProjectConversationState initial = EmptyState().Value;

        ProjectConversationAiResponseNudgeReceivedAction safeAction =
            new(Nudge(sourceVersion: 11, sequence: 5, redaction: "metadata_only"));
        ProjectConversationState afterSafeReducer = ProjectConversationReducers.ReduceAiResponseNudge(initial, safeAction);
        RecordingDispatcher safeNudge = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), new FakeState(afterSafeReducer))
            .HandleAiResponseNudgeAsync(safeAction, safeNudge);

        ProjectConversationAiResponseNudgeReceivedAction unsafeAction =
            new(Nudge(sourceVersion: 11, sequence: 5, redaction: "full"));
        ProjectConversationState afterUnsafeReducer = ProjectConversationReducers.ReduceAiResponseNudge(initial, unsafeAction);
        RecordingDispatcher unsafeNudge = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), new FakeState(afterUnsafeReducer))
            .HandleAiResponseNudgeAsync(unsafeAction, unsafeNudge);

        safeNudge.Actions.OfType<LoadProjectConversationAction>().Single().ProjectId.ShouldBe("project-001");
        unsafeNudge.Actions.OfType<LoadProjectConversationAction>().ShouldBeEmpty();
        unsafeNudge.Actions.OfType<ProjectConversationAiResponseNudgeRejectedAction>().Single()
            .ErrorCode.ShouldBe("ai-response-nudge-unsafe");
    }

    [Fact]
    public async Task ProjectionSignalEffectShouldSynthesizeForwardNudgeForLoadedConversationAndFailClosedOnMismatch()
    {
        // Signal-only transport: a project-conversation change for the loaded conversation becomes a metadata-only,
        // forward-looking nudge (one past the last-rendered progress) that drives a typed re-query. A signal for a
        // different project is ignored (fail closed).
        ProjectConversationModel conversation = await ConversationWithProgressAsync(AiResponseProgressState.Rendering, isTerminal: false);
        FakeState loaded = new(new ProjectConversationState(false, conversation, null));

        RecordingDispatcher matched = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), loaded)
            .HandleProjectionSignalAsync(new ProjectConversationProjectionSignalReceivedAction("project-001", "tenant-001"), matched);

        ProjectConversationAiResponseNudgeModel nudge =
            matched.Actions.OfType<ProjectConversationAiResponseNudgeReceivedAction>().Single().Nudge;
        nudge.ProjectId.ShouldBe("project-001");
        nudge.RedactionState.ShouldBe("metadata_only");
        nudge.VisibilityState.ShouldBe("metadata_only");
        nudge.SourceVersion.ShouldBe(11); // last-rendered 10 + 1
        nudge.Sequence.ShouldBe(5);        // last-rendered 4 + 1

        RecordingDispatcher mismatched = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), loaded)
            .HandleProjectionSignalAsync(new ProjectConversationProjectionSignalReceivedAction("project-OTHER", "tenant-001"), mismatched);
        mismatched.Actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProjectionSignalEffectShouldSilentlyDedupBenignDuplicateSignalWithoutSurfacingAStaleError()
    {
        // The tenant-wide, at-least-once change broadcast routinely delivers duplicate / no-advance signals (a duplicate
        // delivery, or a change to ANOTHER conversation in the tenant). When the loaded conversation's last-rendered
        // progress has not advanced, the synthesized forward nudge equals the one already accepted, so the effect must
        // SILENTLY dedup it (dispatch nothing) rather than emit a nudge the reducer rejects and surfaces to the user as a
        // spurious "stale" streaming error. [AC: nudge handlers must be duplicate-safe]
        ProjectConversationModel conversation = await ConversationWithProgressAsync(AiResponseProgressState.Rendering, isTerminal: false);

        // First signal: a genuine advance -> the effect synthesizes and dispatches the forward nudge.
        FakeState loaded = new(new ProjectConversationState(false, conversation, null));
        RecordingDispatcher first = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), loaded)
            .HandleProjectionSignalAsync(new ProjectConversationProjectionSignalReceivedAction("project-001", "tenant-001"), first);
        ProjectConversationAiResponseNudgeModel accepted =
            first.Actions.OfType<ProjectConversationAiResponseNudgeReceivedAction>().Single().Nudge;

        // Second identical signal with no intervening server advance (LastAcceptedAiResponseNudge == what we would
        // synthesize again): silently deduped -> NOTHING dispatched -> no nudge, hence no "ai-response-nudge-unsafe" banner.
        FakeState afterAccept = new(new ProjectConversationState(false, conversation, null) { LastAcceptedAiResponseNudge = accepted });
        RecordingDispatcher duplicate = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), afterAccept)
            .HandleProjectionSignalAsync(new ProjectConversationProjectionSignalReceivedAction("project-001", "tenant-001"), duplicate);

        duplicate.Actions.OfType<ProjectConversationAiResponseNudgeReceivedAction>().ShouldBeEmpty();
        duplicate.Actions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReconnectEffectShouldRequeryAuthorizedProject()
    {
        ProjectConversationEffects effects = new(new ProjectConversationService(new StubChatBotClient()), EmptyState());
        RecordingDispatcher dispatcher = new();

        await effects.HandleAiResponseReconnectAsync(
            new ProjectConversationAiResponseReconnectAction("project-001"),
            dispatcher);

        dispatcher.Actions.OfType<LoadProjectConversationAction>().Single().ProjectId.ShouldBe("project-001");
    }

    [Fact]
    public async Task TerminalGateShouldKeepCancellingUntilTypedReadReportsServerVerifiedStop()
    {
        ProjectConversationState cancelling = new(false, null, null)
        {
            IsCancellingAiResponse = true,
            CancellingResponseId = ResponseId,
            CancellingGenerationId = GenerationId,
        };

        ProjectConversationState afterRenderingReload = ProjectConversationReducers.ReduceLoaded(
            cancelling,
            new ProjectConversationLoadedAction(await ConversationWithProgressAsync(AiResponseProgressState.Rendering, isTerminal: false)));
        ProjectConversationState afterFailedReload = ProjectConversationReducers.ReduceLoaded(
            cancelling,
            new ProjectConversationLoadedAction(await ConversationWithProgressAsync(AiResponseProgressState.Failed, isTerminal: true)));
        ProjectConversationState afterStoppedReload = ProjectConversationReducers.ReduceLoaded(
            cancelling,
            new ProjectConversationLoadedAction(await ConversationWithProgressAsync(AiResponseProgressState.Stopped, isTerminal: true)));

        // Non-terminal reload: still cancelling, identity retained (a nudge alone is never completion evidence).
        afterRenderingReload.IsCancellingAiResponse.ShouldBeTrue();
        afterRenderingReload.CancellingResponseId.ShouldBe(ResponseId);

        // Terminal but not a verified stop (the stop raced natural completion/failure): the tracking CLEARS -- leaving it
        // set stranded the Stop control disabled for every later generation -- but no stop is announced.
        afterFailedReload.IsCancellingAiResponse.ShouldBeFalse();
        afterFailedReload.CancellingResponseId.ShouldBeNull();
        afterFailedReload.VerifiedStopAnnouncementGenerationId.ShouldBeNull();

        // Only a server-verified terminal stop clears the tracked identity AND publishes the announcement token.
        afterStoppedReload.IsCancellingAiResponse.ShouldBeFalse();
        afterStoppedReload.CancellingResponseId.ShouldBeNull();
        afterStoppedReload.CancellingGenerationId.ShouldBeNull();
        afterStoppedReload.VerifiedStopAnnouncementGenerationId.ShouldBe(GenerationId);
    }

    private static ProjectConversationAiResponseProgressModel ActiveProgress()
        => new(
            "project-001",
            "conversation-001",
            ResponseId,
            GenerationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            10,
            4,
            "rendering",
            "none",
            "wait-for-projection",
            "metadata_only",
            "metadata_only",
            false);

    [Fact]
    public async Task ComposerEffectShouldRouteMessageAndAskAiModesToTheirOwnGovernedCommands()
    {
        // Nothing previously executed this branch, so inverting the ternary -- routing a plain "Send" down the risky
        // AI-proposal path, or an Ask-AI request down the plain append path -- broke no test. [AC2]
        StubChatBotClient messageClient = new();
        RecordingDispatcher messageDispatcher = new();
        await new ProjectConversationEffects(new ProjectConversationService(messageClient), EmptyState())
            .HandleSubmitComposerAsync(
                new SubmitProjectConversationComposerAction(
                    "project-001",
                    ProjectConversationComposerMode.Message,
                    "a plain message",
                    "en-US",
                    10),
                messageDispatcher);

        StubChatBotClient askAiClient = new();
        RecordingDispatcher askAiDispatcher = new();
        await new ProjectConversationEffects(new ProjectConversationService(askAiClient), EmptyState())
            .HandleSubmitComposerAsync(
                new SubmitProjectConversationComposerAction(
                    "project-001",
                    ProjectConversationComposerMode.AskAi,
                    "please draft a reply",
                    "en-US",
                    10),
                askAiDispatcher);

        messageClient.LastSubmittedCommand.ShouldBeOfType<RecordProjectConversationMessage>();
        askAiClient.LastSubmittedCommand.ShouldBeOfType<ProposeAIAction>();
        messageClient.LastSubmitOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        askAiClient.LastSubmitOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        messageDispatcher.Actions.OfType<ProjectConversationSubmissionAcceptedAction>().ShouldHaveSingleItem();
        askAiDispatcher.Actions.OfType<ProjectConversationSubmissionAcceptedAction>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ComposerEffectShouldRejectBlankTextBeforeReachingTheGovernedClient()
    {
        StubChatBotClient client = new();
        RecordingDispatcher dispatcher = new();

        await new ProjectConversationEffects(new ProjectConversationService(client), EmptyState())
            .HandleSubmitComposerAsync(
                new SubmitProjectConversationComposerAction(
                    "project-001",
                    ProjectConversationComposerMode.Message,
                    "   ",
                    "en-US",
                    10),
                dispatcher);

        dispatcher.Actions.OfType<ProjectConversationComposerValidationFailedAction>().Single()
            .ErrorCode.ShouldBe(ProjectConversationEffects.EmptyComposerCode);
        client.LastSubmittedCommand.ShouldBeNull();
    }

    [Fact]
    public async Task ComposerEffectShouldGiveEverySubmissionADistinctCorrelationIdWithinTheSameMillisecond()
    {
        // A millisecond-resolution correlation id is hashed straight into the MessageId the aggregate dedups on, so two
        // sends inside one millisecond made the second vanish as a replay.
        StubChatBotClient first = new();
        StubChatBotClient second = new();
        SubmitProjectConversationComposerAction action = new(
            "project-001",
            ProjectConversationComposerMode.Message,
            "same text",
            "en-US",
            10);

        await new ProjectConversationEffects(new ProjectConversationService(first), EmptyState())
            .HandleSubmitComposerAsync(action, new RecordingDispatcher());
        await new ProjectConversationEffects(new ProjectConversationService(second), EmptyState())
            .HandleSubmitComposerAsync(action, new RecordingDispatcher());

        RecordProjectConversationMessage firstMessage = first.LastSubmittedCommand.ShouldBeOfType<RecordProjectConversationMessage>();
        RecordProjectConversationMessage secondMessage = second.LastSubmittedCommand.ShouldBeOfType<RecordProjectConversationMessage>();
        secondMessage.MessageId.ShouldNotBe(firstMessage.MessageId);
    }

    [Fact]
    public async Task ProjectionSignalShouldRequeryDirectlyWhenTheConversationCarriesNoAiProgressRow()
    {
        // With no progress row the synthesized nudge was a CONSTANT record, so after the first signal every later one
        // deduped against itself and ordinary conversation changes refreshed exactly once per page load.
        ProjectConversationModel withoutProgress = await ConversationWithoutProgressAsync();
        FakeState loaded = new(new ProjectConversationState(false, withoutProgress, null));

        RecordingDispatcher first = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), loaded)
            .HandleProjectionSignalAsync(new ProjectConversationProjectionSignalReceivedAction("project-001", "tenant-001"), first);

        RecordingDispatcher second = new();
        await new ProjectConversationEffects(new ProjectConversationService(new StubChatBotClient()), loaded)
            .HandleProjectionSignalAsync(new ProjectConversationProjectionSignalReceivedAction("project-001", "tenant-001"), second);

        // Every signal re-queries; none is swallowed by nudge dedup.
        first.Actions.OfType<LoadProjectConversationAction>().Single().ProjectId.ShouldBe("project-001");
        second.Actions.OfType<LoadProjectConversationAction>().Single().ProjectId.ShouldBe("project-001");
        first.Actions.OfType<ProjectConversationAiResponseNudgeReceivedAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifiedStopShouldPublishADurableAnnouncementTokenForThisSessionsCancellation()
    {
        // The announcement gate used to live in a Stop-control field, so the remount caused by the post-stop re-query
        // swallowed "Response stopped" entirely. The token is store-owned so it survives a remount. [AC4]
        ProjectConversationState cancelling = new(false, null, null)
        {
            IsCancellingAiResponse = true,
            CancellingResponseId = ResponseId,
            CancellingGenerationId = GenerationId,
        };

        ProjectConversationState afterStoppedReload = ProjectConversationReducers.ReduceLoaded(
            cancelling,
            new ProjectConversationLoadedAction(await ConversationWithProgressAsync(AiResponseProgressState.Stopped, isTerminal: true)));

        afterStoppedReload.VerifiedStopAnnouncementGenerationId.ShouldBe(GenerationId);

        // A historically stopped response the user did not cancel in this session publishes no token.
        ProjectConversationState notCancelling = new(false, null, null);
        ProjectConversationState afterHistoricReload = ProjectConversationReducers.ReduceLoaded(
            notCancelling,
            new ProjectConversationLoadedAction(await ConversationWithProgressAsync(AiResponseProgressState.Stopped, isTerminal: true)));

        afterHistoricReload.VerifiedStopAnnouncementGenerationId.ShouldBeNull();
    }

    private static async Task<ProjectConversationModel> ConversationWithoutProgressAsync()
    {
        StubChatBotClient client = new() { IncludeAiResponseProgress = false };
        ProjectConversationService service = new(client);
        return await service.GetProjectConversationAsync("project-001").ConfigureAwait(false);
    }

    private static ProjectConversationAiResponseNudgeModel Nudge(long sourceVersion, long sequence, string redaction)
        => new(
            "project-001",
            "conversation-001",
            ResponseId,
            GenerationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            sourceVersion,
            sequence,
            "rendering",
            redaction,
            "metadata_only");

    private static async Task<ProjectConversationModel> ConversationWithProgressAsync(AiResponseProgressState state, bool isTerminal)
    {
        StubChatBotClient client = new() { ProgressState = state, ProgressIsTerminal = isTerminal };
        ProjectConversationService service = new(client);
        return await service.GetProjectConversationAsync("project-001").ConfigureAwait(false);
    }

    private static FakeState EmptyState() => new(new ProjectConversationState(false, null, null));

    private sealed class FakeState(ProjectConversationState value) : IState<ProjectConversationState>
    {
        public ProjectConversationState Value { get; set; } = value;

        public event EventHandler? StateChanged
        {
            add { }
            remove { }
        }
    }

    private sealed class StubChatBotClient : IChatBotClient
    {
        public Exception? SubmitException { get; init; }

        public AiResponseProgressState ProgressState { get; init; } = AiResponseProgressState.Rendering;

        public bool ProgressIsTerminal { get; init; }

        public bool IncludeAiResponseProgress { get; init; } = true;

        public IChatBotCommand? LastSubmittedCommand { get; private set; }

        public ChatBotSurfaceOrigin? LastSubmitOrigin { get; private set; }

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            if (SubmitException is not null)
            {
                throw SubmitException;
            }

            LastSubmittedCommand = command;
            LastSubmitOrigin = origin;
            return Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "accepted-command-001",
                CorrelationId = correlationId ?? "correlation-generated",
                TaskId = taskId,
                LifecycleState = LifecycleState.Proposed,
                AcceptedAt = new DateTimeOffset(2026, 6, 1, 0, 8, 0, TimeSpan.Zero),
            });
        }

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProjectConversationResponse
            {
                ProjectId = projectId,
                ProjectDisplayName = "Authorized Project",
                Status = ProjectConversationReadStatus.Current,
                ConversationState = LifecycleState.Associated,
                Items =
                [
                    new ProjectConversationItem
                    {
                        ItemId = "ai:proposal-001:proposal:10",
                        Kind = ProjectConversationItemKind.AiOutcome,
                        ActorKind = ProjectConversationActorKind.AiActor,
                        ActorLabel = "AI actor",
                        OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 6, 0, TimeSpan.Zero),
                        LifecycleState = LifecycleState.NeedsReview,
                        ThresholdBand = AssociationThresholdBand.Auto,
                        ConfidenceScore = 0,
                        AssociationId = ResponseId,
                        SourceMailboxId = "ai-outcome",
                        SourceConversationId = "conversation-001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 10,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        SafeNextAction = "review-ai-action",
                        AiResponseProgress = !IncludeAiResponseProgress ? null : new AiResponseProgress
                        {
                            ProjectId = projectId,
                            ConversationId = "conversation-001",
                            ResponseId = ResponseId,
                            GenerationId = GenerationId,
                            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                            SourceVersion = 10,
                            Sequence = 4,
                            State = ProgressState,
                            TerminalReason = ProgressIsTerminal
                                ? ProgressState == AiResponseProgressState.Stopped
                                    ? AiResponseTerminalReason.UserStopped
                                    : AiResponseTerminalReason.Failed
                                : AiResponseTerminalReason.None,
                            SafeNextAction = "wait-for-projection",
                            RedactionState = AiResponseProgressRedactionState.Metadata_only,
                            VisibilityState = AiResponseProgressVisibilityState.Metadata_only,
                            IsTerminal = ProgressIsTerminal,
                        },
                    },
                ],
                Page = new ProjectConversationCursorPage { NextCursor = null, HasMore = false, PageSize = 25 },
                SourceProvenance = ProjectConversationResponseSourceProvenance.M365MailboxIntake,
                RedactionState = ProjectConversationResponseRedactionState.Metadata_only,
                RetentionClass = ProjectConversationResponseRetentionClass.Collaboration_input,
                SchemaVersion = ProjectConversationResponseSchemaVersion.Chatbot_projectConversationResponse_v1,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                SafeNextAction = "none",
            });

        public Task<OperationStatus> GetOperationStatusAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(string associationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TaskIntentReview> GetTaskIntentReviewAsync(string projectId, string taskIntentId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingDispatcher : IDispatcher
    {
        public List<object> Actions { get; } = [];

        public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;

        public void Dispatch(object action)
        {
            Actions.Add(action);
            ActionDispatched?.Invoke(this, new ActionDispatchedEventArgs(action));
        }
    }
}
