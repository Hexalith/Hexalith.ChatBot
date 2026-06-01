using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed class ProjectConversationEffects(ProjectConversationService service)
{
    public const string GenericFailureCode = "project-conversation-unavailable";

    private readonly ProjectConversationService _service = service ?? throw new ArgumentNullException(nameof(service));

    [EffectMethod]
    public async Task HandleLoadAsync(LoadProjectConversationAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            ProjectConversationModel conversation = await _service
                .GetProjectConversationAsync(action.ProjectId, action.Cursor)
                .ConfigureAwait(false);
            dispatcher.Dispatch(new ProjectConversationLoadedAction(conversation));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new ProjectConversationFailedAction(
                string.IsNullOrWhiteSpace(problem.Result?.Code) ? GenericFailureCode : problem.Result.Code));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new ProjectConversationFailedAction(GenericFailureCode));
        }
    }
}
