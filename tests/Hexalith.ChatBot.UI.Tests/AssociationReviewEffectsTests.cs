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

    [Fact]
    public async Task LoadEffectShouldRethrowCancellation()
    {
        AssociationReviewEffects effects = EffectsThatThrow(new OperationCanceledException());
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
            .ValidationErrorCode.ShouldBe("association-review-unavailable");
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
            .ErrorCode.ShouldBe("association-review-unavailable");
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
        public LifecycleState LifecycleState { get; init; } = LifecycleState.NeedsReview;

        public int SubmitCount { get; private set; }

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
            SubmittedCommand = command;
            return Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAC",
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TaskId = null,
                LifecycleState = LifecycleState,
            });
        }

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
