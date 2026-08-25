using Fluxor;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public static class ProjectConversationReducers
{
    // A re-query is NOT a cold load. Nudge, reconnect, post-submit and post-cancel reloads all dispatch
    // LoadProjectConversationAction for the conversation already on screen; blanking Conversation there would unmount
    // the stream, the composer and the Stop control mid-generation, destroying focus and any typed draft (AC3's
    // "stable control remains keyboard reachable" and AC4's "focus returns to composer"). So the prior conversation --
    // and the reviewer's open Why panel -- survive a same-project reload and are replaced only when Loaded/Failed
    // resolves. A genuine project switch still clears everything, including the nudge watermark and any cancellation
    // tracked against the project being left.
    [ReducerMethod]
    public static ProjectConversationState ReduceLoad(ProjectConversationState state, LoadProjectConversationAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        bool switchingProject = state.Conversation is { } current &&
            !string.Equals(current.ProjectId, action.ProjectId, StringComparison.Ordinal);
        return state with
        {
            IsLoading = true,
            Conversation = switchingProject ? null : state.Conversation,
            ErrorCode = null,
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
            IsWhyPanelLoading = switchingProject ? false : state.IsWhyPanelLoading,
            WhyPanel = switchingProject ? null : state.WhyPanel,
            WhyPanelProjectId = switchingProject ? null : state.WhyPanelProjectId,
            WhyPanelAssociationId = switchingProject ? null : state.WhyPanelAssociationId,
            WhyPanelErrorCode = switchingProject ? null : state.WhyPanelErrorCode,
            StreamingErrorCode = null,
            LastAcceptedAiResponseNudge = switchingProject ? null : state.LastAcceptedAiResponseNudge,
            IsCancellingAiResponse = switchingProject ? false : state.IsCancellingAiResponse,
            CancellingResponseId = switchingProject ? null : state.CancellingResponseId,
            CancellingGenerationId = switchingProject ? null : state.CancellingGenerationId,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceLoaded(ProjectConversationState state, ProjectConversationLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        // The tracked generation reached SOME terminal state (stopped/cancelled, but also completed/failed/unavailable
        // when the stop raced natural completion). Tracking must clear on any of them: gating only on a verified stop
        // left IsCancellingAiResponse true forever after a race, and because the Stop control's Disabled binding reads
        // that flag, every LATER generation rendered a permanently disabled Stop for the life of the circuit. [AC4]
        ProjectConversationAiResponseProgressModel? trackedTerminal = state.CancellingResponseId is null
            ? null
            : action.Conversation.Items
                .Select(static item => item.AiResponseProgress)
                .OfType<ProjectConversationAiResponseProgressModel>()
                .FirstOrDefault(progress =>
                    string.Equals(progress.ResponseId, state.CancellingResponseId, StringComparison.Ordinal) &&
                    string.Equals(progress.GenerationId, state.CancellingGenerationId, StringComparison.Ordinal) &&
                    progress.IsTerminal);

        // ...but "Response stopped" is announced only for a server-verified stop. A generation that completed or failed
        // on its own is terminal without being a stop, so it clears the tracking silently.
        bool verifiedCancel = trackedTerminal is not null &&
            ProjectConversationAiResponseProgressStates.IsVerifiedStop(trackedTerminal.State);
        bool trackingResolved = trackedTerminal is not null;
        return state with
        {
            IsLoading = false,
            Conversation = action.Conversation,
            ErrorCode = null,
            IsCancellingAiResponse = trackingResolved ? false : state.IsCancellingAiResponse,
            CancellingResponseId = trackingResolved ? null : state.CancellingResponseId,
            CancellingGenerationId = trackingResolved ? null : state.CancellingGenerationId,

            // A fresh authoritative read supersedes a transient "reconnected" notice; leaving it set made a genuine
            // terminal stopped/completed row keep rendering as "Reconnected" (StreamingStatusText takes precedence).
            StreamingNotice = null,

            // Hand the Stop control a durable, store-owned announcement token for THIS session's verified stop.
            VerifiedStopAnnouncementGenerationId = verifiedCancel
                ? state.CancellingGenerationId
                : state.VerifiedStopAnnouncementGenerationId,
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
            VerifiedStopAnnouncementGenerationId = null,
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
            VerifiedStopAnnouncementGenerationId = null,
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
        // Fail closed on project identity. Guarding on "Conversation is not null" meant the cross-project check was
        // skipped for the whole window in which a reload is in flight, so a foreign-project nudge was accepted there.
        if (state.Conversation is not { } loaded ||
            !string.Equals(loaded.ProjectId, nudge.ProjectId, StringComparison.Ordinal))
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
