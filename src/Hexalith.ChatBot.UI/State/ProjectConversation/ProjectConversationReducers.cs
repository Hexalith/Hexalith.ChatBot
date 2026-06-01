using Fluxor;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public static class ProjectConversationReducers
{
    [ReducerMethod(typeof(LoadProjectConversationAction))]
    public static ProjectConversationState ReduceLoad(ProjectConversationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            IsLoading = true,
            Conversation = null,
            ErrorCode = null,
            IsWhyPanelLoading = false,
            WhyPanel = null,
            WhyPanelProjectId = null,
            WhyPanelAssociationId = null,
            WhyPanelErrorCode = null,
        };
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
        return state with
        {
            IsLoading = false,
            Conversation = null,
            ErrorCode = action.ErrorCode,
            IsWhyPanelLoading = false,
            WhyPanel = null,
            WhyPanelProjectId = null,
            WhyPanelAssociationId = null,
            WhyPanelErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceOpenWhyPanel(ProjectConversationState state, OpenProjectAssociationWhyPanelAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsWhyPanelLoading = true,
            WhyPanel = null,
            WhyPanelProjectId = action.ProjectId,
            WhyPanelAssociationId = action.AssociationId,
            WhyPanelErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceWhyPanelLoaded(ProjectConversationState state, ProjectAssociationWhyPanelLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return IsCurrentPanelRequest(state, action.ProjectId, action.AssociationId)
            ? state with { IsWhyPanelLoading = false, WhyPanel = action.Panel, WhyPanelErrorCode = null }
            : state;
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceWhyPanelFailed(ProjectConversationState state, ProjectAssociationWhyPanelFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return IsCurrentPanelRequest(state, action.ProjectId, action.AssociationId)
            ? state with { IsWhyPanelLoading = false, WhyPanel = null, WhyPanelErrorCode = action.ErrorCode }
            : state;
    }

    [ReducerMethod(typeof(CloseProjectAssociationWhyPanelAction))]
    public static ProjectConversationState ReduceCloseWhyPanel(ProjectConversationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            IsWhyPanelLoading = false,
            WhyPanel = null,
            WhyPanelProjectId = null,
            WhyPanelAssociationId = null,
            WhyPanelErrorCode = null,
        };
    }

    private static bool IsCurrentPanelRequest(ProjectConversationState state, string projectId, string associationId)
        => string.Equals(state.WhyPanelProjectId, projectId, StringComparison.Ordinal) &&
            string.Equals(state.WhyPanelAssociationId, associationId, StringComparison.Ordinal);
}
