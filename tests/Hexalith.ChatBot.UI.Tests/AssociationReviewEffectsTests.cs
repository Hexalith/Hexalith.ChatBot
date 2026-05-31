using Fluxor;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Shouldly;

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
    public async Task PreviewEffectShouldRejectSubmitWhenReviewStateIsUnavailable()
    {
        AssociationReviewEffects effects = EffectsThatThrow(new NotSupportedException());
        RecordingDispatcher dispatcher = new();

        await effects.HandlePreviewAsync(new PreviewAssociationDecisionAction("choose-candidate"), dispatcher);

        dispatcher.Actions.OfType<AssociationDecisionPreviewRejectedAction>()
            .Single()
            .ValidationErrorCode.ShouldBe("association-review-unavailable");
    }

    private static AssociationReviewEffects EffectsThatThrow(Exception exception)
        => new(new AssociationReviewService(new ThrowingClient(exception)));

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
