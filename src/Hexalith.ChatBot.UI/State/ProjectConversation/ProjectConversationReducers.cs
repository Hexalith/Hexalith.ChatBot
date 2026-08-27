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
        string? scopedProjectId = state.RequestedProjectId ?? state.Conversation?.ProjectId;
        bool switchingProject = !string.IsNullOrWhiteSpace(scopedProjectId) &&
            !string.Equals(scopedProjectId, action.ProjectId, StringComparison.Ordinal);
        if (action.IsHistory && (state.Conversation is null || switchingProject))
        {
            return state with { ErrorCode = "project-conversation-history-without-current" };
        }

        return state with
        {
            IsLoading = action.IsHistory ? state.IsLoading : true,
            IsHistoryLoading = action.IsHistory,
            Conversation = switchingProject ? null : state.Conversation,
            ErrorCode = null,
            RequestedProjectId = action.ProjectId,
            ProjectScopeVersion = switchingProject ? checked(state.ProjectScopeVersion + 1) : state.ProjectScopeVersion,
            CurrentLoadRequestId = action.IsHistory ? state.CurrentLoadRequestId : action.RequestId,
            // A fresh current-page request invalidates every older-page request already in flight. Otherwise a late
            // history response can resurrect an item the newer authoritative page intentionally omitted/redacted.
            HistoryLoadRequestId = action.IsHistory ? action.RequestId : null,
            HistoricalItems = switchingProject ? [] : state.HistoricalItems,
            CurrentPageItemIds = switchingProject ? null : state.CurrentPageItemIds,
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
            IsSubmitting = switchingProject ? false : state.IsSubmitting,
            PendingSubmission = switchingProject ? null : state.PendingSubmission,
            SubmissionRequestId = switchingProject ? null : state.SubmissionRequestId,
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
            CancellationRequestId = switchingProject ? null : state.CancellationRequestId,
            VerifiedStopAnnouncementGenerationId = switchingProject ? null : state.VerifiedStopAnnouncementGenerationId,
            StreamingNotice = switchingProject ? null : state.StreamingNotice,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceLoaded(ProjectConversationState state, ProjectConversationLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        bool isHistory = !string.IsNullOrWhiteSpace(action.Cursor);
        string requestedProjectId = action.RequestedProjectId ?? action.Conversation.ProjectId;
        bool correlated = string.IsNullOrWhiteSpace(action.RequestId) ||
            string.Equals(
                action.RequestId,
                isHistory ? state.HistoryLoadRequestId : state.CurrentLoadRequestId,
                StringComparison.Ordinal);
        if (!correlated)
        {
            return state;
        }

        if (!string.Equals(action.Conversation.ProjectId, requestedProjectId, StringComparison.Ordinal) ||
            !string.Equals(state.RequestedProjectId ?? requestedProjectId, requestedProjectId, StringComparison.Ordinal))
        {
            return state with
            {
                IsLoading = isHistory ? state.IsLoading : false,
                IsHistoryLoading = false,
                ErrorCode = "project-conversation-load-identity-mismatch",
            };
        }

        ProjectConversationModel mergedConversation;
        IReadOnlyList<ProjectConversationItemModel> historicalItems;
        IReadOnlySet<string> currentPageItemIds;
        if (isHistory)
        {
            if (state.Conversation is not { } current ||
                !string.Equals(current.ProjectId, requestedProjectId, StringComparison.Ordinal))
            {
                return state with { IsHistoryLoading = false, ErrorCode = "project-conversation-load-identity-mismatch" };
            }

            currentPageItemIds = state.CurrentPageItemIds ?? current.Items.Select(static item => item.ItemId).ToHashSet(StringComparer.Ordinal);
            historicalItems = MergeHistoryItems(
                state.HistoricalItems ?? [],
                action.Conversation.Items.Where(item => !currentPageItemIds.Contains(item.ItemId)));
            mergedConversation = current with
            {
                Items = MergeItems(current.Items, historicalItems),
                NextCursor = action.Conversation.NextCursor,
                HasMore = action.Conversation.HasMore,
            };
        }
        else
        {
            IReadOnlySet<string> previousCurrentIds = state.CurrentPageItemIds ?? new HashSet<string>(StringComparer.Ordinal);
            // A current-page refresh is authoritative. Anything formerly on that page but now omitted (including a
            // newly redacted item) is purged from accumulated history instead of being resurrected by an older page.
            historicalItems = action.Conversation.IsAllCoveringEmpty
                ? []
                : (state.HistoricalItems ?? [])
                    .Where(item => !previousCurrentIds.Contains(item.ItemId))
                    .Where(item => !IsCoveredByAuthoritativeStream(item, action.Conversation.AuthoritativeCoverage))
                    .ToArray();
            currentPageItemIds = action.Conversation.Items
                .Select(static item => item.ItemId)
                .ToHashSet(StringComparer.Ordinal);
            historicalItems = historicalItems.Where(item => !currentPageItemIds.Contains(item.ItemId)).ToArray();
            mergedConversation = action.Conversation with
            {
                Items = MergeItems(action.Conversation.Items, historicalItems),
            };
        }

        // The tracked generation reached SOME terminal state (stopped/cancelled, but also completed/failed/unavailable
        // when the stop raced natural completion). Tracking must clear on any of them: gating only on a verified stop
        // left IsCancellingAiResponse true forever after a race, and because the Stop control's Disabled binding reads
        // that flag, every LATER generation rendered a permanently disabled Stop for the life of the circuit. [AC4]
        ProjectConversationAiResponseProgressModel? trackedTerminal = state.CancellingResponseId is null
            ? null
            : mergedConversation.Items
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
            IsHistoryLoading = false,
            Conversation = mergedConversation,
            HistoricalItems = historicalItems,
            CurrentPageItemIds = currentPageItemIds,
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

    private static bool IsCoveredByAuthoritativeStream(
        ProjectConversationItemModel item,
        IReadOnlyList<ProjectConversationStreamCoverageModel> coverage)
    {
        ProjectConversationAiResponseProgressModel? progress = item.AiResponseProgress;
        if (progress is null || string.IsNullOrWhiteSpace(progress.StateOwnerAggregateId))
        {
            return false;
        }

        return coverage.Any(interval =>
            interval.IsContiguous &&
            interval.CoversAllKnownItems &&
            string.Equals(interval.StateOwnerAggregateId, progress.StateOwnerAggregateId, StringComparison.Ordinal) &&
            progress.SourceVersion >= interval.FromSourceVersion &&
            progress.SourceVersion <= interval.ThroughSourceVersion);
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceFailed(ProjectConversationState state, ProjectConversationFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        bool isHistory = !string.IsNullOrWhiteSpace(action.Cursor);
        bool correlated = string.IsNullOrWhiteSpace(action.RequestId) ||
            string.Equals(
                action.RequestId,
                isHistory ? state.HistoryLoadRequestId : state.CurrentLoadRequestId,
                StringComparison.Ordinal);
        if (!correlated)
        {
            return state;
        }

        return state with
        {
            IsLoading = isHistory ? state.IsLoading : false,
            IsHistoryLoading = false,
            // Keep the last safe, same-project view. A transient read failure must not erase the conversation,
            // composer draft, or the cancellation whose terminal outcome is still being verified.
            Conversation = state.Conversation,
            ErrorCode = action.ErrorCode,
            StreamingErrorCode = action.ErrorCode,
            StreamingNotice = null,
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
        string? scopedProjectId = state.RequestedProjectId ?? state.Conversation?.ProjectId;
        if (scopedProjectId is not null && !string.Equals(scopedProjectId, action.ProjectId, StringComparison.Ordinal))
        {
            return state;
        }

        return state with
        {
            ComposerMode = action.Mode,
            IsSubmitting = true,
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
            StreamingNotice = null,
            VerifiedStopAnnouncementGenerationId = null,
            PendingSubmission = null,
            SubmissionRequestId = action.RequestId,
            RequestedProjectId = scopedProjectId ?? action.ProjectId,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceComposerValidationFailed(ProjectConversationState state, ProjectConversationComposerValidationFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (!MatchesSubmission(state, action.RequestId, action.ScopeVersion, null))
        {
            return state;
        }

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
        if (!MatchesSubmission(state, action.RequestId, action.ScopeVersion, action.ProjectId))
        {
            return state;
        }

        return state with
        {
            IsSubmitting = false,
            PendingSubmission = action.Receipt,
            ComposerValidationErrorCode = null,
            SubmissionErrorCode = null,
            SubmissionRequestId = null,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceSubmissionFailed(ProjectConversationState state, ProjectConversationSubmissionFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (!MatchesSubmission(state, action.RequestId, action.ScopeVersion, action.ProjectId))
        {
            return state;
        }

        return state with
        {
            IsSubmitting = false,
            SubmissionErrorCode = action.ErrorCode,
            SubmissionRequestId = null,
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
    [ReducerMethod]
    public static ProjectConversationState ReduceAiResponseReconnect(
        ProjectConversationState state,
        ProjectConversationAiResponseReconnectAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if ((action.ScopeVersion != 0 && action.ScopeVersion != state.ProjectScopeVersion) ||
            !string.Equals(state.RequestedProjectId ?? state.Conversation?.ProjectId, action.ProjectId, StringComparison.Ordinal))
        {
            return state;
        }

        return state with { StreamingNotice = "reconnected", StreamingErrorCode = null };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceAiResponseCancellationPending(ProjectConversationState state, ProjectConversationAiResponseCancellationPendingAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (action.ProjectId is not null &&
            (!string.Equals(state.RequestedProjectId ?? state.Conversation?.ProjectId, action.ProjectId, StringComparison.Ordinal) ||
             action.ScopeVersion != state.ProjectScopeVersion))
        {
            return state;
        }

        return state with
        {
            IsCancellingAiResponse = true,
            CancellingResponseId = action.ResponseId,
            CancellingGenerationId = action.GenerationId,
            StreamingErrorCode = null,
            StreamingNotice = null,
            VerifiedStopAnnouncementGenerationId = null,
            CancellationRequestId = action.RequestId,
        };
    }

    [ReducerMethod]
    public static ProjectConversationState ReduceAiResponseCancellationAccepted(ProjectConversationState state, ProjectConversationAiResponseCancellationAcceptedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (!MatchesCancellation(state, action.RequestId, action.ScopeVersion, action.ProjectId))
        {
            return state;
        }

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
        if (!MatchesCancellation(state, action.RequestId, action.ScopeVersion, action.ProjectId))
        {
            return state;
        }

        return state with
        {
            IsCancellingAiResponse = false,
            CancellingResponseId = null,
            CancellingGenerationId = null,
            StreamingErrorCode = action.ErrorCode,
            CancellationRequestId = null,
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

    private static bool MatchesSubmission(
        ProjectConversationState state,
        string? requestId,
        long scopeVersion,
        string? projectId)
        => (requestId is null || string.Equals(requestId, state.SubmissionRequestId, StringComparison.Ordinal)) &&
            (requestId is null || scopeVersion == state.ProjectScopeVersion) &&
            (projectId is null || string.Equals(projectId, state.RequestedProjectId ?? state.Conversation?.ProjectId, StringComparison.Ordinal));

    private static bool MatchesCancellation(
        ProjectConversationState state,
        string? requestId,
        long scopeVersion,
        string? projectId)
        => (requestId is null || string.Equals(requestId, state.CancellationRequestId, StringComparison.Ordinal)) &&
            (requestId is null || scopeVersion == state.ProjectScopeVersion) &&
            (projectId is null || string.Equals(projectId, state.RequestedProjectId ?? state.Conversation?.ProjectId, StringComparison.Ordinal));

    private static IReadOnlyList<ProjectConversationItemModel> MergeItems(
        IEnumerable<ProjectConversationItemModel> preferred,
        IEnumerable<ProjectConversationItemModel> fallback)
    {
        Dictionary<string, ProjectConversationItemModel> byId = new(StringComparer.Ordinal);
        List<string> order = [];
        foreach (ProjectConversationItemModel item in preferred.Concat(fallback))
        {
            if (byId.ContainsKey(item.ItemId))
            {
                continue;
            }

            byId[item.ItemId] = item;
            order.Add(item.ItemId);
        }

        return order.Select(id => byId[id]).ToArray();
    }

    private static IReadOnlyList<ProjectConversationItemModel> MergeHistoryItems(
        IEnumerable<ProjectConversationItemModel> accumulated,
        IEnumerable<ProjectConversationItemModel> loaded)
    {
        Dictionary<string, ProjectConversationItemModel> byId = new(StringComparer.Ordinal);
        List<string> order = [];
        foreach (ProjectConversationItemModel item in accumulated.Concat(loaded))
        {
            if (!byId.TryGetValue(item.ItemId, out ProjectConversationItemModel? existing))
            {
                byId[item.ItemId] = item;
                order.Add(item.ItemId);
            }
            else if (item.SourceVersion > existing.SourceVersion)
            {
                // History responses may overlap and complete out of order. Keep the newest durable version while
                // preserving the item's established visual position; the current page still wins in MergeItems.
                byId[item.ItemId] = item;
            }
        }

        return order.Select(id => byId[id]).ToArray();
    }

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
