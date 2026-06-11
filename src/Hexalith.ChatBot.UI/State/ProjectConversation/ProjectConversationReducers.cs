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
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
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
            IsSubmitting = false,
            SubmissionErrorCode = action.ErrorCode,
            IsWhyPanelLoading = false,
            WhyPanel = null,
            WhyPanelProjectId = null,
            WhyPanelAssociationId = null,
            WhyPanelErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceSetComposerMode(ProjectConversationState state, SetProjectConversationComposerModeAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ComposerMode = action.Mode,
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceSubmitComposer(ProjectConversationState state, SubmitProjectConversationComposerAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            ComposerMode = action.Mode,
            IsSubmitting = true,
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceComposerValidationFailed(ProjectConversationState state, ProjectConversationComposerValidationFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            ComposerValidationErrorCode = action.ErrorCode,
            SubmissionErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceSubmissionAccepted(ProjectConversationState state, ProjectConversationSubmissionAcceptedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            PendingSubmission = action.Receipt,
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceSubmissionFailed(ProjectConversationState state, ProjectConversationSubmissionFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            SubmissionErrorCode = action.ErrorCode,
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
