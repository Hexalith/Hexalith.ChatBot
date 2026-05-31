using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed class AssociationReviewEffects(AssociationReviewService service)
{
    public const string GenericFailureCode = "association-review-unavailable";

    private readonly AssociationReviewService _service = service ?? throw new ArgumentNullException(nameof(service));

    [EffectMethod]
    public async Task HandleLoadAsync(LoadAssociationReviewAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            AssociationReviewModel review = await _service
                .GetAssociationReviewAsync(action.AssociationId)
                .ConfigureAwait(false);
            dispatcher.Dispatch(new AssociationReviewLoadedAction(review));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new AssociationReviewFailedAction(SafeFailureCode(problem.Result?.Code)));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new AssociationReviewFailedAction(GenericFailureCode));
        }
    }

    [EffectMethod]
    public Task HandlePreviewAsync(PreviewAssociationDecisionAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        dispatcher.Dispatch(new AssociationDecisionPreviewRejectedAction("decision-command-not-available"));
        return Task.CompletedTask;
    }

    private static string SafeFailureCode(string? problemCode)
        => string.IsNullOrWhiteSpace(problemCode) ? GenericFailureCode : problemCode;
}
