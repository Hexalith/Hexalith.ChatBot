using System.Text.Json;

using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Registration;
using Hexalith.ChatBot.UI.State.ProjectConversation;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ProjectConversationStateTests
{
    [Fact]
    public void FrontComposerRegistrationShouldLoadEveryChatBotReducer()
    {
        // Fluxor treats an explicit ActionType as the metadata for a one-parameter reducer. A typed, two-parameter
        // reducer must use the parameter-inferred attribute; otherwise the real UI fails during startup even though
        // direct reducer unit tests pass.
        ServiceCollection services = new();
        services.AddHexalithFrontComposerQuickstart(
            static options => options.ScanAssemblies(typeof(ChatBotUiFrontComposerMarker).Assembly));
    }

    [Fact]
    public void ProjectSwitchShouldClearPriorConversationWhileFailureKeepsLastSafeView()
    {
        ProjectConversationModel priorConversation = new(
            "project-alpha",
            "Project Alpha",
            null,
            "Current",
            "Associated",
            [],
            null,
            false,
            25,
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.project-conversation-response.v1",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            "none");
        ProjectConversationState state = new(false, priorConversation, null);

        // Switching to a DIFFERENT project drops the prior conversation.
        ProjectConversationState switching = ProjectConversationReducers.ReduceLoad(
            state,
            new LoadProjectConversationAction("project-beta"));
        ProjectConversationState failed = ProjectConversationReducers.ReduceFailed(
            state,
            new ProjectConversationFailedAction("authorization_denied"));

        switching.IsLoading.ShouldBeTrue();
        switching.Conversation.ShouldBeNull();
        failed.IsLoading.ShouldBeFalse();
        failed.Conversation.ShouldBe(priorConversation);
        failed.ErrorCode.ShouldBe("authorization_denied");
    }

    [Fact]
    public void SameProjectReloadShouldKeepTheRenderedConversationAndOpenWhyPanel()
    {
        // A nudge / reconnect / post-submit / post-cancel re-query re-loads the conversation already on screen. Blanking
        // it there unmounted the stream, the composer and the Stop control mid-generation, destroying focus and any
        // typed draft, and silently closed the reviewer's open Why panel. Only a genuine project switch may clear.
        ProjectConversationModel conversation = Conversation("project-alpha");
        ProjectConversationState state = new(false, conversation, null)
        {
            WhyPanelProjectId = "project-alpha",
            WhyPanelAssociationId = "association-001",
            LastAcceptedAiResponseNudge = null,
        };

        ProjectConversationState reloading = ProjectConversationReducers.ReduceLoad(
            state,
            new LoadProjectConversationAction("project-alpha"));

        reloading.IsLoading.ShouldBeTrue();
        reloading.Conversation.ShouldBe(conversation);
        reloading.WhyPanelProjectId.ShouldBe("project-alpha");
        reloading.WhyPanelAssociationId.ShouldBe("association-001");
    }

    [Fact]
    public void SameProjectBackgroundReloadShouldKeepComposerEnabledAndPreserveFocus()
    {
        ProjectConversationModel conversation = Conversation("project-alpha");
        ProjectConversationState backgroundReload = ProjectConversationReducers.ReduceLoad(
            new ProjectConversationState(false, conversation, null),
            new LoadProjectConversationAction("project-alpha"));

        ChatBotProjectConversationWorkspace.ShouldDisableComposer(backgroundReload, conversation).ShouldBeFalse();
        ChatBotProjectConversationWorkspace.ShouldDisableComposer(
            new ProjectConversationState(true, null, null),
            conversation: null).ShouldBeTrue();
        ChatBotProjectConversationWorkspace.ShouldDisableComposer(
            backgroundReload with { IsSubmitting = true },
            conversation).ShouldBeTrue();
    }

    [Fact]
    public void CurrentAndHistoryResponsesShouldCorrelateIndependentlyAndLateHistoryMustNotResurrectOmittedItems()
    {
        ProjectConversationItemModel formerlyCurrent = Item("item-current-omitted", 8);
        ProjectConversationState initial = new(false, Conversation("project-alpha", [formerlyCurrent]), null)
        {
            CurrentPageItemIds = new HashSet<string>([formerlyCurrent.ItemId], StringComparer.Ordinal),
            HistoricalItems = [],
        };
        LoadProjectConversationAction history = new("project-alpha", "cursor-before-8") { RequestId = "history-1" };
        LoadProjectConversationAction current = new("project-alpha") { RequestId = "current-2" };

        ProjectConversationState loadingHistory = ProjectConversationReducers.ReduceLoad(initial, history);
        ProjectConversationState loadingCurrent = ProjectConversationReducers.ReduceLoad(loadingHistory, current);
        ProjectConversationState afterLateHistory = ProjectConversationReducers.ReduceLoaded(
            loadingCurrent,
            new ProjectConversationLoadedAction(
                Conversation("project-alpha", [formerlyCurrent]),
                history.RequestId,
                "project-alpha",
                history.Cursor));
        ProjectConversationState afterCurrent = ProjectConversationReducers.ReduceLoaded(
            afterLateHistory,
            new ProjectConversationLoadedAction(
                Conversation("project-alpha", [Item("item-new-current", 9)]),
                current.RequestId,
                "project-alpha"));

        loadingCurrent.HistoryLoadRequestId.ShouldBeNull();
        afterLateHistory.ShouldBe(loadingCurrent);
        afterCurrent.Conversation!.Items.Select(static item => item.ItemId).ShouldBe(["item-new-current"]);
        afterCurrent.HistoricalItems.ShouldBeEmpty();
    }

    [Fact]
    public void HistoryPageShouldMergeOlderItemsWithoutReplacingTheNewestHeader()
    {
        ProjectConversationItemModel newest = Item("item-newest", 10);
        ProjectConversationState initial = new(false, Conversation("project-alpha", [newest], "cursor-before-10", true), null)
        {
            CurrentPageItemIds = new HashSet<string>([newest.ItemId], StringComparer.Ordinal),
            HistoricalItems = [],
        };
        LoadProjectConversationAction history = new("project-alpha", "cursor-before-10") { RequestId = "history-1" };

        ProjectConversationState loading = ProjectConversationReducers.ReduceLoad(initial, history);
        ProjectConversationState loaded = ProjectConversationReducers.ReduceLoaded(
            loading,
            new ProjectConversationLoadedAction(
                Conversation("project-alpha", [Item("item-older", 4)], null, false),
                history.RequestId,
                "project-alpha",
                history.Cursor));

        loaded.Conversation!.ProjectDisplayName.ShouldBe("Project Alpha");
        loaded.Conversation.Items.Select(static item => item.ItemId).ShouldBe(["item-newest", "item-older"]);
        loaded.Conversation.HasMore.ShouldBeFalse();
        loaded.IsHistoryLoading.ShouldBeFalse();
    }

    [Fact]
    public void MixedStreamCoverageShouldPurgeOnlyCoveredOwnerAndAllCoveringEmptyShouldPurgeEverything()
    {
        ProjectConversationItemModel ownerA = AiItem("owner-a-old", "owner-a", 7);
        ProjectConversationItemModel ownerB = AiItem("owner-b-same-version", "owner-b", 7);
        ProjectConversationState initial = new(false, Conversation("project-alpha", [ownerA, ownerB]), null)
        {
            HistoricalItems = [ownerA, ownerB],
            CurrentPageItemIds = new HashSet<string>(StringComparer.Ordinal),
            RequestedProjectId = "project-alpha",
        };
        ProjectConversationModel coveringA = Conversation("project-alpha") with
        {
            AuthoritativeCoverage = [new ProjectConversationStreamCoverageModel("owner-a", 1, 10, true, true)],
        };

        ProjectConversationState afterMixedCoverage = ProjectConversationReducers.ReduceLoaded(
            initial,
            new ProjectConversationLoadedAction(coveringA));
        ProjectConversationState afterAllCoveringEmpty = ProjectConversationReducers.ReduceLoaded(
            afterMixedCoverage,
            new ProjectConversationLoadedAction(Conversation("project-alpha") with { IsAllCoveringEmpty = true }));

        afterMixedCoverage.Conversation!.Items.Select(static item => item.ItemId).ShouldBe([ownerB.ItemId]);
        afterAllCoveringEmpty.Conversation!.Items.ShouldBeEmpty();
        afterAllCoveringEmpty.HistoricalItems.ShouldBeEmpty();
    }

    [Fact]
    public void LoadedResponseForAnotherProjectShouldFailClosedAndKeepTheLastSafeView()
    {
        ProjectConversationModel safe = Conversation("project-alpha", [Item("item-safe", 1)]);
        LoadProjectConversationAction load = new("project-alpha") { RequestId = "current-1" };
        ProjectConversationState loading = ProjectConversationReducers.ReduceLoad(new(false, safe, null), load);

        ProjectConversationState result = ProjectConversationReducers.ReduceLoaded(
            loading,
            new ProjectConversationLoadedAction(
                Conversation("project-beta", [Item("item-foreign", 2)]),
                load.RequestId,
                "project-alpha"));

        result.Conversation.ShouldBe(safe);
        result.ErrorCode.ShouldBe("project-conversation-load-identity-mismatch");
    }

    [Fact]
    public void CrossProjectNudgeShouldFailClosedWhileNoConversationIsLoaded()
    {
        // The cross-project guard used to be skipped whenever Conversation was null, so a foreign-project nudge was
        // accepted for the whole window in which a reload was in flight.
        ProjectConversationState noConversation = new(false, null, null);
        ProjectConversationAiResponseNudgeModel foreign = new(
            "project-OTHER",
            "conversation-001",
            "response-001",
            "generation-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            11,
            5,
            "rendering",
            "metadata_only",
            "metadata_only");

        ProjectConversationState result = ProjectConversationReducers.ReduceAiResponseNudge(
            noConversation,
            new ProjectConversationAiResponseNudgeReceivedAction(foreign));

        result.LastAcceptedAiResponseNudge.ShouldBeNull();
        result.StreamingErrorCode.ShouldBe("ai-response-nudge-unsafe");
    }

    private static ProjectConversationModel Conversation(
        string projectId,
        IReadOnlyList<ProjectConversationItemModel>? items = null,
        string? nextCursor = null,
        bool hasMore = false) => new(
        projectId,
        "Project Alpha",
        null,
        "Current",
        "Associated",
        items ?? [],
        nextCursor,
        hasMore,
        25,
        "m365-mailbox-intake",
        "metadata_only",
        "collaboration_input",
        "chatbot.project-conversation-response.v1",
        "01ARZ3NDEKTSV4RRFFQ69G5FAX",
        "none");

    private static ProjectConversationItemModel Item(string itemId, long sourceVersion)
        => JsonSerializer.Deserialize<ProjectConversationItemModel>(
            $$"""{"itemId":"{{itemId}}","sourceVersion":{{sourceVersion}}}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The project-conversation item fixture could not be created.");

    private static ProjectConversationItemModel AiItem(string itemId, string owner, long sourceVersion)
        => JsonSerializer.Deserialize<ProjectConversationItemModel>(
            $$"""
            {
              "itemId": "{{itemId}}",
              "sourceVersion": {{sourceVersion}},
              "aiResponseProgress": {
                "projectId": "project-alpha",
                "conversationId": "{{owner}}",
                "responseId": "response-001",
                "generationId": "generation-001",
                "correlationId": "correlation-001",
                "sourceVersion": {{sourceVersion}},
                "sequence": 1,
                "state": "rendering",
                "terminalReason": "none",
                "safeNextAction": "wait",
                "redactionState": "metadata_only",
                "visibilityState": "metadata_only",
                "isTerminal": false,
                "stateOwnerAggregateId": "{{owner}}"
              }
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The AI project-conversation item fixture could not be created.");

    [Fact]
    public void ComposerReducersShouldTrackSubmittingAcceptedAndValidationStates()
    {
        ProjectConversationState state = new(false, null, null);

        ProjectConversationState submitting = ProjectConversationReducers.ReduceSubmitComposer(
            state,
            new SubmitProjectConversationComposerAction("project-001", ProjectConversationComposerMode.AskAi, "request", "en-US", 8));
        ProjectConversationSubmissionReceiptModel receipt = new(
            ProjectConversationComposerMode.AskAi,
            "command-001",
            "correlation-001",
            "task-001",
            "Proposed",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            "wait-for-projection");

        ProjectConversationState accepted = ProjectConversationReducers.ReduceSubmissionAccepted(
            submitting,
            new ProjectConversationSubmissionAcceptedAction(receipt));
        ProjectConversationState invalid = ProjectConversationReducers.ReduceComposerValidationFailed(
            accepted,
            new ProjectConversationComposerValidationFailedAction("composer_input_required"));

        submitting.IsSubmitting.ShouldBeTrue();
        submitting.ComposerMode.ShouldBe(ProjectConversationComposerMode.AskAi);
        accepted.IsSubmitting.ShouldBeFalse();
        accepted.PendingSubmission.ShouldBe(receipt);
        invalid.ComposerValidationErrorCode.ShouldBe("composer_input_required");
    }

    [Fact]
    public void ProjectSwitchShouldInvalidateEveryOldSubmissionCancellationAndLoadIdentity()
    {
        ProjectConversationState oldScope = new(false, Conversation("project-old"), null)
        {
            RequestedProjectId = "project-old",
            ProjectScopeVersion = 7,
            IsSubmitting = true,
            SubmissionRequestId = "submit-old",
            PendingSubmission = new ProjectConversationSubmissionReceiptModel(
                ProjectConversationComposerMode.Message,
                "command-old",
                "correlation-old",
                null,
                "Proposed",
                DateTimeOffset.UtcNow,
                "wait-for-projection"),
            IsCancellingAiResponse = true,
            CancellationRequestId = "cancel-old",
            CancellingResponseId = "response-old",
            CancellingGenerationId = "generation-old",
            CurrentLoadRequestId = "load-old",
            HistoryLoadRequestId = "history-old",
            VerifiedStopAnnouncementGenerationId = "generation-old",
        };

        ProjectConversationState switched = ProjectConversationReducers.ReduceLoad(
            oldScope,
            new LoadProjectConversationAction("project-new") { RequestId = "load-new" });
        ProjectConversationState afterLateAcceptance = ProjectConversationReducers.ReduceSubmissionAccepted(
            switched,
            new ProjectConversationSubmissionAcceptedAction(oldScope.PendingSubmission!)
            {
                ProjectId = "project-old",
                RequestId = "submit-old",
                ScopeVersion = 7,
            });

        switched.ProjectScopeVersion.ShouldBe(8);
        switched.RequestedProjectId.ShouldBe("project-new");
        switched.IsSubmitting.ShouldBeFalse();
        switched.PendingSubmission.ShouldBeNull();
        switched.SubmissionRequestId.ShouldBeNull();
        switched.IsCancellingAiResponse.ShouldBeFalse();
        switched.CancellationRequestId.ShouldBeNull();
        switched.CancellingResponseId.ShouldBeNull();
        switched.CurrentLoadRequestId.ShouldBe("load-new");
        switched.HistoryLoadRequestId.ShouldBeNull();
        switched.VerifiedStopAnnouncementGenerationId.ShouldBeNull();
        afterLateAcceptance.ShouldBe(switched);
    }

    [Fact]
    public void StreamingReducersShouldAcceptOnlyNewerMatchingMetadataOnlyNudges()
    {
        ProjectConversationState state = new(
            false,
            Conversation("project-001"),
            null,
            LastAcceptedAiResponseNudge: new ProjectConversationAiResponseNudgeModel(
                "project-001",
                "conversation-001",
                "response-001",
                "generation-001",
                "correlation-001",
                10,
                3,
                "rendering",
                "metadata_only",
                "metadata_only"));
        ProjectConversationAiResponseNudgeModel accepted = new(
            "project-001",
            "conversation-001",
            "response-001",
            "generation-001",
            "correlation-001",
            11,
            4,
            "rendering",
            "metadata_only",
            "metadata_only");
        ProjectConversationAiResponseNudgeModel stale = accepted with { Sequence = 2 };
        ProjectConversationAiResponseNudgeModel mismatched = accepted with { ProjectId = "project-other" };

        ProjectConversationState afterAccepted = ProjectConversationReducers.ReduceAiResponseNudge(
            state,
            new ProjectConversationAiResponseNudgeReceivedAction(accepted));
        ProjectConversationState afterStale = ProjectConversationReducers.ReduceAiResponseNudge(
            state,
            new ProjectConversationAiResponseNudgeReceivedAction(stale));
        ProjectConversationState afterMismatch = ProjectConversationReducers.ReduceAiResponseNudge(
            state,
            new ProjectConversationAiResponseNudgeReceivedAction(mismatched));

        afterAccepted.LastAcceptedAiResponseNudge.ShouldBe(accepted);
        afterAccepted.StreamingErrorCode.ShouldBeNull();
        afterStale.StreamingErrorCode.ShouldBe("ai-response-nudge-unsafe");
        afterMismatch.StreamingErrorCode.ShouldBe("ai-response-nudge-unsafe");
    }

    [Fact]
    public void ReconnectReducerShouldSurfaceLocalizedNoticeClearedByFreshActivity()
    {
        ProjectConversationState state = new(false, Conversation("project-001"), null);

        ProjectConversationState reconnected = ProjectConversationReducers.ReduceAiResponseReconnect(
            state,
            new ProjectConversationAiResponseReconnectAction("project-001"));
        ProjectConversationState afterSubmit = ProjectConversationReducers.ReduceSubmitComposer(
            reconnected,
            new SubmitProjectConversationComposerAction("project-001", ProjectConversationComposerMode.AskAi, "hi", "en-US", 1));

        reconnected.StreamingNotice.ShouldBe("reconnected");
        reconnected.StreamingErrorCode.ShouldBeNull();
        afterSubmit.StreamingNotice.ShouldBeNull();
    }

    [Fact]
    public void StaleReconnectAfterProjectSwitchShouldNotReviveOldScopeOrNotice()
    {
        ProjectConversationState switched = new(false, Conversation("project-new"), null)
        {
            RequestedProjectId = "project-new",
            ProjectScopeVersion = 8,
        };
        ProjectConversationAiResponseReconnectAction stale = new("project-old") { ScopeVersion = 7 };

        ProjectConversationState result = ProjectConversationReducers.ReduceAiResponseReconnect(switched, stale);

        result.ShouldBe(switched);
        result.StreamingNotice.ShouldBeNull();
    }

}
