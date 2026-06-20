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
            StreamingErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceLoaded(ProjectConversationState state, ProjectConversationLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        bool verifiedCancel = state.CancellingResponseId is not null &&
            action.Conversation.Items.Any(item =>
                string.Equals(item.AiResponseProgress?.ResponseId, state.CancellingResponseId, StringComparison.Ordinal) &&
                string.Equals(item.AiResponseProgress?.GenerationId, state.CancellingGenerationId, StringComparison.Ordinal) &&
                item.AiResponseProgress is { IsTerminal: true } &&
                ProjectConversationAiResponseProgressStates.IsVerifiedStop(item.AiResponseProgress.State));
        return state with
        {
            IsLoading = false,
            Conversation = action.Conversation,
            ErrorCode = null,
            IsCancellingAiResponse = verifiedCancel ? false : state.IsCancellingAiResponse,
            CancellingResponseId = verifiedCancel ? null : state.CancellingResponseId,
            CancellingGenerationId = verifiedCancel ? null : state.CancellingGenerationId,
        };
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
            StreamingErrorCode = action.ErrorCode,
            StreamingNotice = null,
            IsCancellingAiResponse = false,
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
            StreamingNotice = null,
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
    public static ProjectConversationState ReduceAiResponseNudge(ProjectConversationState state, ProjectConversationAiResponseNudgeReceivedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return IsAcceptableNudge(state, action.Nudge)
            ? state with { LastAcceptedAiResponseNudge = action.Nudge, StreamingErrorCode = null, StreamingNotice = null }
            : state with { StreamingErrorCode = "ai-response-nudge-unsafe" };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceAiResponseNudgeRejected(ProjectConversationState state, ProjectConversationAiResponseNudgeRejectedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { StreamingErrorCode = action.ErrorCode };
    }

    // Reconnect is advisory (AC5): surface a transient localized "reconnected" notice and let the re-query effect
    // refresh authoritative state. The notice clears on the next accepted nudge or user submission.
    [ReducerMethod(typeof(ProjectConversationAiResponseReconnectAction))]
    public static ProjectConversationState ReduceAiResponseReconnect(ProjectConversationState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { StreamingNotice = "reconnected", StreamingErrorCode = null };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceAiResponseCancellationPending(ProjectConversationState state, ProjectConversationAiResponseCancellationPendingAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsCancellingAiResponse = true,
            CancellingResponseId = action.ResponseId,
            CancellingGenerationId = action.GenerationId,
            StreamingErrorCode = null,
            StreamingNotice = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceAiResponseCancellationAccepted(ProjectConversationState state, ProjectConversationAiResponseCancellationAcceptedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            PendingSubmission = action.Receipt,
            StreamingErrorCode = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceAiResponseCancellationFailed(ProjectConversationState state, ProjectConversationAiResponseCancellationFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsCancellingAiResponse = false,
            CancellingResponseId = null,
            CancellingGenerationId = null,
            StreamingErrorCode = action.ErrorCode,
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

    private static bool IsAcceptableNudge(ProjectConversationState state, ProjectConversationAiResponseNudgeModel nudge)
    {
        if (state.Conversation is not null &&
            !string.Equals(state.Conversation.ProjectId, nudge.ProjectId, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(nudge.RedactionState, "metadata_only", StringComparison.Ordinal) ||
            !string.Equals(nudge.VisibilityState, "metadata_only", StringComparison.Ordinal))
        {
            return false;
        }

        if (state.LastAcceptedAiResponseNudge is { } prior &&
            string.Equals(prior.ProjectId, nudge.ProjectId, StringComparison.Ordinal) &&
            string.Equals(prior.ConversationId, nudge.ConversationId, StringComparison.Ordinal) &&
            string.Equals(prior.ResponseId, nudge.ResponseId, StringComparison.Ordinal) &&
            string.Equals(prior.GenerationId, nudge.GenerationId, StringComparison.Ordinal) &&
            (nudge.SourceVersion < prior.SourceVersion || nudge.Sequence <= prior.Sequence))
        {
            return false;
        }

        ProjectConversationAiResponseProgressModel? current = state.Conversation?.Items
            .Select(static item => item.AiResponseProgress)
            .OfType<ProjectConversationAiResponseProgressModel>()
            .FirstOrDefault(progress =>
                string.Equals(progress.ProjectId, nudge.ProjectId, StringComparison.Ordinal) &&
                string.Equals(progress.ConversationId, nudge.ConversationId, StringComparison.Ordinal) &&
                string.Equals(progress.ResponseId, nudge.ResponseId, StringComparison.Ordinal) &&
                string.Equals(progress.GenerationId, nudge.GenerationId, StringComparison.Ordinal));

        return current is null ||
            nudge.SourceVersion >= current.SourceVersion &&
            nudge.Sequence > current.Sequence;
    }
}
