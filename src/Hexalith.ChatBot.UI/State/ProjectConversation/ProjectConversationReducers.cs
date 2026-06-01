using Fluxor;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public static class ProjectConversationReducers
{
    [ReducerMethod(typeof(LoadProjectConversationAction))]
    public static ProjectConversationState ReduceLoad(ProjectConversationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { IsLoading = true, Conversation = null, ErrorCode = null };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceLoaded(ProjectConversationState state, ProjectConversationLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsLoading = false, Conversation = action.Conversation, ErrorCode = null };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceFailed(ProjectConversationState state, ProjectConversationFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsLoading = false, Conversation = null, ErrorCode = action.ErrorCode };
    }
}
