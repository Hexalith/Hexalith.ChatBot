using Fluxor;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Shouldly;

using LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class AssociationReviewEffectsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public async Task BlankAssociationLoadShouldClearPriorRouteAndFailWithoutReadingClient(string associationId)
    {
        RecordingClient client = new();
        AssociationReviewState previous = StateOf(client, "choose-candidate", "candidate-001", "Prior note") with
        {
            CorrectionRationale = "Prior rationale",
            LastAcceptedOperation = new AssociationOperationIdentity("old-command", "old-correlation", null, "Proposed"),
        };
        LoadAssociationReviewAction action = new(associationId);
        AssociationReviewState loading = AssociationReviewReducers.ReduceLoad(previous, action);
        loading.Review.ShouldBeNull();
        loading.SelectedCandidateId.ShouldBeNull();
        loading.DecisionNote.ShouldBeEmpty();
        loading.PendingDecisionCode.ShouldBeNull();
        loading.CorrectionRationale.ShouldBeEmpty();
        loading.LastAcceptedOperation.ShouldBeNull();

        int reads = client.RoutingReadCount;
        RecordingDispatcher dispatcher = new();
        await EffectsFor(client, loading).HandleLoadAsync(action, dispatcher);
        client.RoutingReadCount.ShouldBe(reads);
        AssociationReviewFailedAction failure = dispatcher.Actions.ShouldHaveSingleItem().ShouldBeOfType<AssociationReviewFailedAction>();
        failure.ErrorCode.ShouldBe("association-id-required");
        AssociationReviewState failed = AssociationReviewReducers.ReduceFailed(loading, failure);
        failed.IsLoading.ShouldBeFalse();
        failed.Review.ShouldBeNull();
        AssociationReviewFailureCatalog.IsRetryable(failed.ErrorCode).ShouldBeFalse();
    }

    /// <summary>A blank-ID failure can reach the reducer after a newer route was selected.</summary>
    [Fact]
    public async Task StaleBlankAssociationFailureShouldNotOverwriteNewerRoute()
    {
        RecordingClient client = new();
        LoadAssociationReviewAction blankLoad = new(" ");
        AssociationReviewState current = AssociationReviewReducers.ReduceLoad(StateOf(client), blankLoad);
        AssociationReviewEffects effects = EffectsFor(client, current);
        RecordingDispatcher dispatcher = new();

        await effects.HandleLoadAsync(blankLoad, dispatcher);
        AssociationReviewFailedAction failure = dispatcher.Actions.ShouldHaveSingleItem().ShouldBeOfType<AssociationReviewFailedAction>();
        current = AssociationReviewReducers.ReduceLoad(current, new LoadAssociationReviewAction("new-association"));

        AssociationReviewState result = AssociationReviewReducers.ReduceFailed(current, failure);
        result.ShouldBeSameAs(current);
        result.IsLoading.ShouldBeTrue();
        result.ErrorCode.ShouldBeNull();
    }

    /// <summary>A stale blank-ID effect cannot supersede a route already selected by its load reducer.</summary>
    [Fact]
    public async Task SupersededBlankAssociationLoadShouldNotPublishFailure()
    {
        RecordingClient client = new();
        AssociationReviewState current = AssociationReviewReducers.ReduceLoad(StateOf(client), new LoadAssociationReviewAction("new-association"));
        RecordingDispatcher dispatcher = new();

        await EffectsFor(client, current).HandleLoadAsync(new LoadAssociationReviewAction(" "), dispatcher);

        dispatcher.Actions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EarlierLoadShouldNotReplaceBlankIdFailure(bool failEarlierLoad)
    {
        RecordingClient client = new();
        AssociationRoutingStatus oldStatus = await client.GetAssociationRoutingStatusAsync("old-association", cancellationToken: TestContext.Current.CancellationToken);
        AssociationReviewState state = StateOf(client);
        TaskCompletionSource<AssociationRoutingStatus> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.PendingRoutingRead = pending;
        AssociationReviewEffects effects = EffectsFor(client, state);
        RecordingDispatcher dispatcher = new();
        Task earlier = effects.HandleLoadAsync(new LoadAssociationReviewAction("old-association"), dispatcher);
        earlier.IsCompleted.ShouldBeFalse();
        await effects.HandleLoadAsync(new LoadAssociationReviewAction(" "), dispatcher);
        if (failEarlierLoad)
        {
            pending.SetException(new TimeoutException("private delayed failure"));
        }
        else
        {
            pending.SetResult(oldStatus);
        }

        await earlier.ConfigureAwait(true);
        dispatcher.Actions.ShouldHaveSingleItem().ShouldBeOfType<AssociationReviewFailedAction>()
            .ErrorCode.ShouldBe("association-id-required");
    }

    /// <summary>A result dispatched by the previous load cannot overwrite the newer blank route at reduction.</summary>
    /// <param name="failPreviousLoad">Whether the prior request returns a failure instead of a review.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelayedPreviousResultShouldNotOverwriteBlankRoute(bool failPreviousLoad)
    {
        RecordingClient client = new();
        LoadAssociationReviewAction previous = new("old-association");
        AssociationReviewState current = AssociationReviewReducers.ReduceLoad(StateOf(client), previous);
        if (failPreviousLoad)
        {
            client.PendingRoutingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
            client.PendingRoutingRead.SetException(new TimeoutException("previous failure"));
        }

        RecordingDispatcher dispatcher = new();
        await EffectsFor(client, current).HandleLoadAsync(previous, dispatcher);
        object previousResult = dispatcher.Actions.ShouldHaveSingleItem();
        LoadAssociationReviewAction blank = new(" ");
        current = AssociationReviewReducers.ReduceLoad(current, blank);
        dispatcher.Actions.Clear();
        await EffectsFor(client, current).HandleLoadAsync(blank, dispatcher);
        AssociationReviewFailedAction blankResult = dispatcher.Actions.ShouldHaveSingleItem().ShouldBeOfType<AssociationReviewFailedAction>();
        AssociationReviewState blankState = AssociationReviewReducers.ReduceFailed(current, blankResult);

        AssociationReviewState afterPrevious = failPreviousLoad
            ? AssociationReviewReducers.ReduceFailed(blankState, previousResult.ShouldBeOfType<AssociationReviewFailedAction>())
            : AssociationReviewReducers.ReduceLoaded(blankState, previousResult.ShouldBeOfType<AssociationReviewLoadedAction>());
        afterPrevious.ShouldBeSameAs(blankState);
        afterPrevious.ErrorCode.ShouldBe("association-id-required");
    }

    [Theory]
    [MemberData(nameof(UiTransportFailureCases.Cases), MemberType = typeof(UiTransportFailureCases))]
    public async Task TransportFailureShouldUseStatusCatalogWithoutRawText(string kind, int status, string expected)
    {
        AssociationReviewEffects effects = EffectsThatThrow(UiTransportFailureCases.Create(kind, status));
        RecordingDispatcher dispatcher = new();
        await effects.HandleLoadAsync(new LoadAssociationReviewAction("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), dispatcher);
        dispatcher.Actions.OfType<AssociationReviewFailedAction>().Single().ErrorCode.ShouldBe(expected);
    }


    [Theory]
    [InlineData("task-timeout", false)]
    [InlineData("canceled-token-timeout", false)]
    [InlineData("task-timeout", true)]
    [InlineData("canceled-token-timeout", true)]
    public async Task SubmissionTimeoutShouldProduceSafeFailure(string kind, bool correction)
    {
        RecordingClient client = new()
        {
            LifecycleState = correction ? LifecycleState.Associated : LifecycleState.NeedsReview,
            SubmitException = UiTransportFailureCases.Create(kind, 0),
        };
        RecordingDispatcher dispatcher = new();
        AssociationReviewEffects effects = EffectsFor(client, StateOf(client, "choose-candidate"));
        if (correction)
        {
            await effects.HandleCorrectionAsync(new SubmitAssociationCorrectionAction(), dispatcher);
            dispatcher.Actions.OfType<AssociationCorrectionSubmitFailedAction>().Single().ErrorCode.ShouldBe("dependency_degraded");
            dispatcher.Actions.OfType<AssociationCorrectionSubmittedAction>().ShouldBeEmpty();
        }
        else
        {
            await effects.HandleConfirmedDecisionAsync(new ConfirmAssociationDecisionAction(), dispatcher);
            dispatcher.Actions.OfType<AssociationDecisionSubmitFailedAction>().Single().ErrorCode.ShouldBe("dependency_degraded");
            dispatcher.Actions.OfType<AssociationDecisionSubmittedAction>().ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task LoadEffectShouldPreserveServerProblemCodesAndNeverSurfaceRawExceptionText()
    {
        AssociationReviewEffects effects = EffectsThatThrow(new HexalithChatBotApiException<ProblemDetails>(
            "Metadata-only authorization denial.",
            403,
            response: null,
            headers: new Dictionary<string, IEnumerable<string>>(),
            result: new ProblemDetails { Code = "authorization_denied" },
            innerException: null));
        RecordingDispatcher dispatcher = new();

        await effects.HandleLoadAsync(new LoadAssociationReviewAction("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), dispatcher);

        AssociationReviewFailedAction failure = dispatcher.Actions.OfType<AssociationReviewFailedAction>().Single();
        failure.ErrorCode.ShouldBe("authorization_denied");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadEffectShouldRethrowCancellation(bool taskCancellation)
    {
        AssociationReviewEffects effects = EffectsThatThrow(taskCancellation ? new TaskCanceledException("explicit cancellation", null, new CancellationToken(canceled: true)) : new OperationCanceledException());
        RecordingDispatcher dispatcher = new();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            effects.HandleLoadAsync(new LoadAssociationReviewAction("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), dispatcher));

        dispatcher.Actions.OfType<AssociationReviewFailedAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task DecisionEffectShouldRejectSubmitWhenReviewStateIsUnavailable()
    {
        AssociationReviewEffects effects = EffectsThatThrow(new NotSupportedException());
        RecordingDispatcher dispatcher = new();

        await effects.HandleConfirmedDecisionAsync(new ConfirmAssociationDecisionAction(), dispatcher);

        dispatcher.Actions.OfType<AssociationDecisionValidationRejectedAction>()
            .Single()
            .ValidationErrorCode.ShouldBe("generic_denial");
    }

    /// <summary>
    /// Confirming with no pending decision must not submit. The confirmation is the only thing standing
    /// between a click and a durable command.
    /// </summary>
    [Fact]
    public async Task DecisionEffectShouldNotSubmitWithoutAPendingDecision()
    {
        RecordingClient client = new();
        RecordingDispatcher dispatcher = new();
        AssociationReviewEffects effects = EffectsFor(client, StateOf(client, pendingDecisionCode: null));

        await effects.HandleConfirmedDecisionAsync(new ConfirmAssociationDecisionAction(), dispatcher);

        client.SubmitCount.ShouldBe(0);
        dispatcher.Actions.OfType<AssociationDecisionValidationRejectedAction>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DecisionEffectShouldSubmitTheSelectedCandidateAndItsNoteInTheRightRoles()
    {
        RecordingClient client = new();
        RecordingDispatcher dispatcher = new();
        AssociationReviewEffects effects = EffectsFor(
            client,
            StateOf(client, "choose-candidate", selectedCandidateId: "01ARZ3NDEKTSV4RRFFQ69G5FBB", decisionNote: "Reviewed the thread."));

        await effects.HandleConfirmedDecisionAsync(new ConfirmAssociationDecisionAction(), dispatcher);

        client.SubmitCount.ShouldBe(1);
        Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject command = client.SubmittedCommand
            .ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject>();
        command.ProjectId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FBB");
        command.DecisionNote.ShouldBe("Reviewed the thread.");
        dispatcher.Actions.OfType<AssociationDecisionSubmittedAction>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DecisionEffectShouldRefuseToDecideAnAssociationThatWentTerminalBeforeConfirmation()
    {
        RecordingClient client = new() { LifecycleState = LifecycleState.Associated };
        RecordingDispatcher dispatcher = new();
        AssociationReviewEffects effects = EffectsFor(client, StateOf(client, "defer"));

        await effects.HandleConfirmedDecisionAsync(new ConfirmAssociationDecisionAction(), dispatcher);

        client.SubmitCount.ShouldBe(0);
        dispatcher.Actions.OfType<AssociationDecisionValidationRejectedAction>()
            .Single()
            .ValidationErrorCode.ShouldBe("terminal-state");
    }

    [Theory]
    [InlineData(LifecycleState.Associated, true)]
    [InlineData(LifecycleState.Corrected, true)]
    [InlineData(LifecycleState.Correcting, false)]
    [InlineData(LifecycleState.CorrectionDelayed, false)]
    [InlineData(LifecycleState.NeedsReview, false)]
    public async Task CorrectionEffectShouldSubmitOnlyForLifecyclesTheSurfaceOffers(
        LifecycleState lifecycleState,
        bool expectSubmit)
    {
        RecordingClient client = new() { LifecycleState = lifecycleState };
        RecordingDispatcher dispatcher = new();
        AssociationReviewEffects effects = EffectsFor(
            client,
            StateOf(client, selectedCandidateId: "01ARZ3NDEKTSV4RRFFQ69G5FBB"));

        await effects.HandleCorrectionAsync(new SubmitAssociationCorrectionAction(), dispatcher);

        client.SubmitCount.ShouldBe(expectSubmit ? 1 : 0);
        if (!expectSubmit)
        {
            dispatcher.Actions.OfType<AssociationCorrectionValidationRejectedAction>()
                .Single()
                .ValidationErrorCode.ShouldBe("correction-invalid-lifecycle");
        }
    }

    /// <summary>Only the load that started a submission can apply its eventual outcome.</summary>
    [Theory]
    [InlineData(false, "accepted", "current")]
    [InlineData(false, "failed", "current")]
    [InlineData(false, "validation", "current")]
    [InlineData(false, "accepted", "away")]
    [InlineData(false, "failed", "away")]
    [InlineData(false, "accepted", "returned")]
    [InlineData(false, "failed", "returned")]
    [InlineData(false, "validation", "returned")]
    [InlineData(true, "accepted", "current")]
    [InlineData(true, "failed", "current")]
    [InlineData(true, "validation", "current")]
    [InlineData(true, "accepted", "away")]
    [InlineData(true, "failed", "away")]
    [InlineData(true, "accepted", "returned")]
    [InlineData(true, "failed", "returned")]
    [InlineData(true, "validation", "returned")]
    public async Task SubmissionResultCanOnlyChangeTheReviewThatStartedIt(bool correction, string outcome, string destination)
    {
        RecordingClient client = new() { LifecycleState = correction ? LifecycleState.Associated : LifecycleState.NeedsReview };
        AssociationReviewState initial = StateOf(client, pendingDecisionCode: correction ? null : "choose-candidate");
        AssociationReviewModel originalReview = initial.Review.ShouldNotBeNull();
        var originalLoad = new LoadAssociationReviewAction(originalReview.AssociationId);
        FakeState state = new(initial with { CurrentLoad = originalLoad, IsSubmitting = true });
        AssociationReviewEffects effects = new(new AssociationReviewService(client, AssociationReviewTestText.Create()), state);
        RecordingDispatcher dispatcher = new();
        TaskCompletionSource<CommandSubmissionResponse> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        client.PendingSubmit = pending;

        Task submit = correction
            ? effects.HandleCorrectionAsync(new SubmitAssociationCorrectionAction(), dispatcher)
            : effects.HandleConfirmedDecisionAsync(new ConfirmAssociationDecisionAction(), dispatcher);
        client.SubmitCount.ShouldBe(1);
        submit.IsCompleted.ShouldBeFalse();

        void NavigateTo(AssociationReviewModel review)
        {
            LoadAssociationReviewAction load = new(review.AssociationId);
            state.Value = AssociationReviewReducers.ReduceLoad(state.Value, load);
            state.Value.IsSubmitting.ShouldBeFalse();
            state.Value = AssociationReviewReducers.ReduceLoaded(state.Value, new(review) { OriginatingLoad = load });
        }

        if (destination != "current")
        {
            NavigateTo(originalReview with { AssociationId = "different-association" });
            if (destination == "returned")
            {
                NavigateTo(originalReview with { SourceVersion = originalReview.SourceVersion + 1 });
            }
        }

        AssociationReviewState current = state.Value;
        if (outcome == "accepted")
        {
            pending.SetResult(client.SubmitResponse());
        }
        else
        {
            pending.SetException(outcome == "validation"
                ? new InvalidOperationException("stale-evidence")
                : new TimeoutException("delayed submission failure"));
        }

        await submit.ConfigureAwait(true);
        AssociationReviewState updated = dispatcher.Actions.ShouldHaveSingleItem() switch
        {
            AssociationDecisionSubmittedAction action => AssociationReviewReducers.ReduceSubmitted(current, action),
            AssociationDecisionSubmitFailedAction action => AssociationReviewReducers.ReduceSubmitFailed(current, action),
            AssociationDecisionValidationRejectedAction action => AssociationReviewReducers.ReduceDecisionRejected(current, action),
            AssociationCorrectionSubmittedAction action => AssociationReviewReducers.ReduceCorrectionSubmitted(current, action),
            AssociationCorrectionSubmitFailedAction action => AssociationReviewReducers.ReduceCorrectionFailed(current, action),
            AssociationCorrectionValidationRejectedAction action => AssociationReviewReducers.ReduceCorrectionRejected(current, action),
            _ => throw new InvalidOperationException("Unexpected submission result."),
        };

        if (destination != "current")
        {
            updated.ShouldBeSameAs(current);
        }
        else if (outcome == "accepted")
        {
            updated.IsSubmitting.ShouldBeFalse();
            updated.LastAcceptedOperation.ShouldNotBeNull().CommandId.ShouldBe(client.SubmitResponse().CommandId);
        }
        else if (outcome == "validation")
        {
            updated.IsSubmitting.ShouldBeFalse();
            updated.ValidationErrorCode.ShouldBe("stale-evidence");
            updated.ErrorCode.ShouldBeNull();
            updated.FailureActivation.ShouldBe(current.FailureActivation);
            updated.Review.ShouldBeSameAs(originalReview);
        }
        else
        {
            updated.IsSubmitting.ShouldBeFalse();
            updated.ErrorScope.ShouldBe(AssociationReviewErrorScope.Submit);
            updated.FailureActivation.ShouldBe(current.FailureActivation + 1);
        }
    }

    [Fact]
    public async Task CorrectionEffectShouldRequireATarget()
    {
        RecordingClient client = new() { LifecycleState = LifecycleState.Associated };
        RecordingDispatcher dispatcher = new();
        AssociationReviewEffects effects = EffectsFor(client, StateOf(client, selectedCandidateId: null));

        await effects.HandleCorrectionAsync(new SubmitAssociationCorrectionAction(), dispatcher);

        dispatcher.Actions.OfType<AssociationCorrectionValidationRejectedAction>()
            .Single()
            .ValidationErrorCode.ShouldBe("correction-target-required");
    }

    /// <summary>
    /// A code the surface has no text for must never reach the page. The catalog replaces it with the generic
    /// code rather than rendering server-controlled text.
    /// </summary>
    [Fact]
    public async Task LoadEffectShouldReplaceAnUnknownServerCodeWithTheGenericCode()
    {
        AssociationReviewEffects effects = EffectsThatThrow(new HexalithChatBotApiException<ProblemDetails>(
            "boom",
            500,
            response: null,
            headers: new Dictionary<string, IEnumerable<string>>(),
            result: new ProblemDetails { Code = "<script>alert(1)</script>" },
            innerException: null));
        RecordingDispatcher dispatcher = new();

        await effects.HandleLoadAsync(new LoadAssociationReviewAction("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), dispatcher);

        dispatcher.Actions.OfType<AssociationReviewFailedAction>()
            .Single()
            .ErrorCode.ShouldBe("generic_denial");
    }

    private static AssociationReviewEffects EffectsThatThrow(Exception exception)
    {
        AssociationReviewService service = new(new ThrowingClient(exception), AssociationReviewTestText.Create());
        return new AssociationReviewEffects(service, new FakeState(EmptyState));
    }

    private static AssociationReviewEffects EffectsFor(RecordingClient client, AssociationReviewState state)
        => new(new AssociationReviewService(client, AssociationReviewTestText.Create()), new FakeState(state));

    private static AssociationReviewState EmptyState
        => new(false, false, null, null, string.Empty, string.Empty, null, null);

    private static AssociationReviewState StateOf(
        RecordingClient client,
        string? pendingDecisionCode = null,
        string? selectedCandidateId = "01ARZ3NDEKTSV4RRFFQ69G5FBB",
        string decisionNote = "")
    {
        AssociationReviewService service = new(client, AssociationReviewTestText.Create());
        AssociationReviewModel review = service
            .GetAssociationReviewAsync("01ARZ3NDEKTSV4RRFFQ69G5FAZ")
            .GetAwaiter()
            .GetResult();
        client.ResetCounters();
        return EmptyState with
        {
            Review = review,
            SelectedCandidateId = selectedCandidateId,
            DecisionNote = decisionNote,
            PendingDecisionCode = pendingDecisionCode,
        };
    }

    private sealed class FakeState(AssociationReviewState value) : IState<AssociationReviewState>
    {
        public AssociationReviewState Value { get; set; } = value;

        public event EventHandler? StateChanged
        {
            add { }
            remove { }
        }
    }

    private sealed class ThrowingClient(Exception exception) : IChatBotClient
    {
        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw exception;

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    /// <summary>A client that answers routing-status reads and records what was submitted.</summary>
    private sealed class RecordingClient : IChatBotClient
    {
        public Exception? SubmitException { get; init; }

        public LifecycleState LifecycleState { get; init; } = LifecycleState.NeedsReview;

        public int SubmitCount { get; private set; }

        public int RoutingReadCount { get; private set; }

        public TaskCompletionSource<AssociationRoutingStatus>? PendingRoutingRead { get; set; }

        public TaskCompletionSource<CommandSubmissionResponse>? PendingSubmit { get; set; }

        public IChatBotCommand? SubmittedCommand { get; private set; }

        public void ResetCounters() => SubmitCount = 0;

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            SubmitCount++;
            if (SubmitException is not null) throw SubmitException;
            SubmittedCommand = command;
            return PendingSubmit?.Task ?? Task.FromResult(SubmitResponse());
        }

        public CommandSubmissionResponse SubmitResponse()
            => new()
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAC",
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TaskId = null,
                LifecycleState = LifecycleState,
            };

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
        {
            RoutingReadCount++;
            if (PendingRoutingRead is not null)
            {
                return PendingRoutingRead.Task;
            }

            Hexalith.ChatBot.Client.Generated.AssociationEvidenceReference evidence = new()
            {
                EvidenceReference = "thread-token",
                EvidenceFingerprint = "fingerprint-1",
                EvidenceKind = "thread",
            };

            return Task.FromResult(new AssociationRoutingStatus
            {
                AssociationId = associationId,
                IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                SourceMailboxId = "mailbox-metadata",
                SourceConversationId = "conversation-metadata",
                LifecycleState = LifecycleState,
                Outcome = Hexalith.ChatBot.Client.Generated.AssociationScoringOutcome.CandidatesGenerated,
                ThresholdBand = Hexalith.ChatBot.Client.Generated.AssociationThresholdBand.Ambiguous,
                ConfidenceScore = 0.64,
                ReasonCodes = [Hexalith.ChatBot.Client.Generated.AssociationReasonCode.MultipleAuthorizedCandidates],
                Candidates =
                [
                    new Hexalith.ChatBot.Client.Generated.AssociationCandidate
                    {
                        ProjectId = "01ARZ3NDEKTSV4RRFFQ69G5FBB",
                        DisplayName = "Authorized candidate",
                        ConfidenceScore = 0.64,
                        Rank = 1,
                        ReasonCodes = [Hexalith.ChatBot.Client.Generated.AssociationReasonCode.ExplicitProjectIdentifierMatched],
                        EvidenceRefs = [evidence],
                        ConfidenceInputs = [],
                        RequiredEvidenceComplete = true,
                    },
                ],
                Exclusions = [],
                ThresholdPolicyVersion = "association-thresholds.m0.default.v1",
                EvidenceRefs = [evidence],
                KernelVersion = "association-deterministic.kernel.m0.v1",
                DetectedAt = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
                SourceProvenance = AssociationRoutingStatusSourceProvenance.M365MailboxIntake,
                RedactionState = AssociationRoutingStatusRedactionState.Metadata_only,
                RetentionClass = AssociationRoutingStatusRetentionClass.Collaboration_input,
                SchemaVersion = "chatbot.association-routing-status.v1",
                SourceVersion = 1,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                DisabledActionReasonCodes = [],
                NextActionReasonCodes = [ChatBotMessageCode.Association_ambiguous_routed],
                PriorProjectId = "01ARZ3NDEKTSV4RRFFQ69G5FB9",
            });
        }

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
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
