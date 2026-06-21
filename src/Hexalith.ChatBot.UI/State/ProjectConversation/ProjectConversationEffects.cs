using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed class ProjectConversationEffects(ProjectConversationService service, IState<ProjectConversationState> state)
{
    public const string GenericFailureCode = "project-conversation-unavailable";
    public const string EmptyComposerCode = "composer_input_required";

    private readonly ProjectConversationService _service = service ?? throw new ArgumentNullException(nameof(service));
    private readonly IState<ProjectConversationState> _state = state ?? throw new ArgumentNullException(nameof(state));

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

    [EffectMethod]
    public async Task HandleSubmitComposerAsync(SubmitProjectConversationComposerAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (string.IsNullOrWhiteSpace(action.Text))
        {
            dispatcher.Dispatch(new ProjectConversationComposerValidationFailedAction(EmptyComposerCode));
            return;
        }

        string correlationId = $"ui-composer:{action.ProjectId}:{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        try
        {
            CommandSubmissionResponse response = action.Mode is ProjectConversationComposerMode.AskAi
                ? await _service
                    .SubmitAskAiAsync(action.ProjectId, action.Text, action.Locale, action.ExpectedSourceVersion, correlationId)
                    .ConfigureAwait(false)
                : await _service
                    .SubmitUserMessageAsync(action.ProjectId, action.Text, action.Locale, action.ExpectedSourceVersion, correlationId)
                    .ConfigureAwait(false);

            dispatcher.Dispatch(new ProjectConversationSubmissionAcceptedAction(new ProjectConversationSubmissionReceiptModel(
                action.Mode,
                response.CommandId,
                response.CorrelationId,
                response.TaskId,
                response.LifecycleState.ToString(),
                response.AcceptedAt,
                "wait-for-projection")));
            dispatcher.Dispatch(new LoadProjectConversationAction(action.ProjectId));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new ProjectConversationSubmissionFailedAction(
                string.IsNullOrWhiteSpace(problem.Result?.Code) ? GenericFailureCode : problem.Result.Code));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new ProjectConversationSubmissionFailedAction(GenericFailureCode));
        }
    }

    // Translates a signal-only projection-changed notification into the rich-nudge re-query path. The signal carries
    // no version/sequence, so a forward-looking metadata-only nudge is synthesized for the CURRENTLY-LOADED conversation
    // (one past the last-rendered progress); IsAcceptableNudge then accepts a genuine advance and dedups a redundant
    // re-signal of the same state. Fails closed when the signal does not match the loaded conversation's project.
    [EffectMethod]
    public Task HandleProjectionSignalAsync(ProjectConversationProjectionSignalReceivedAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (_state.Value.Conversation is not { } conversation ||
            !string.Equals(conversation.ProjectId, action.ProjectId, StringComparison.Ordinal))
        {
            // Stale/cross-project/unloaded signal: ignore. Server state remains the source of truth and the next
            // matching signal or user action re-queries.
            return Task.CompletedTask;
        }

        ProjectConversationAiResponseNudgeModel nudge = BuildReQueryNudge(conversation);

        // Benign duplicate / no-advance re-signal: the tenant-wide, at-least-once change broadcast routinely delivers
        // signals (duplicate deliveries, or changes to OTHER conversations in the same tenant) that do not advance THIS
        // conversation's last-rendered progress, so the synthesized forward nudge is identical to the one already
        // accepted. That is already-rendered state, so silently dedup (the AC "duplicate-safe" requirement) instead of
        // dispatching a nudge the reducer would reject and surface to the user as a spurious "stale" streaming error.
        if (nudge == _state.Value.LastAcceptedAiResponseNudge)
        {
            return Task.CompletedTask;
        }

        dispatcher.Dispatch(new ProjectConversationAiResponseNudgeReceivedAction(nudge));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public Task HandleAiResponseNudgeAsync(ProjectConversationAiResponseNudgeReceivedAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        // Fluxor runs the reducer before this effect, so ReduceAiResponseNudge has already applied the single
        // fail-closed acceptance gate (cross-project + metadata-only + stale/out-of-order). Re-query only when the
        // reducer accepted THIS nudge, so the effect and reducer can never disagree (this is the fix for the prior
        // effect-vs-reducer divergence). Rejected nudges surface the safe stale/unsafe streaming status.
        if (ReferenceEquals(_state.Value.LastAcceptedAiResponseNudge, action.Nudge))
        {
            dispatcher.Dispatch(new LoadProjectConversationAction(action.Nudge.ProjectId));
        }
        else
        {
            dispatcher.Dispatch(new ProjectConversationAiResponseNudgeRejectedAction("ai-response-nudge-unsafe"));
        }

        return Task.CompletedTask;
    }

    private static ProjectConversationAiResponseNudgeModel BuildReQueryNudge(ProjectConversationModel conversation)
    {
        ProjectConversationAiResponseProgressModel? latest = conversation.Items
            .Select(static item => item.AiResponseProgress)
            .OfType<ProjectConversationAiResponseProgressModel>()
            .OrderByDescending(static progress => progress.SourceVersion)
            .ThenByDescending(static progress => progress.Sequence)
            .FirstOrDefault();

        return new ProjectConversationAiResponseNudgeModel(
            conversation.ProjectId,
            latest?.ConversationId ?? conversation.ProjectId,
            latest?.ResponseId ?? string.Empty,
            latest?.GenerationId ?? string.Empty,
            latest?.CorrelationId ?? conversation.CorrelationId,
            (latest?.SourceVersion ?? 0) + 1,
            (latest?.Sequence ?? 0) + 1,
            latest?.State ?? string.Empty,
            "metadata_only",
            "metadata_only");
    }

    [EffectMethod]
    public Task HandleAiResponseReconnectAsync(ProjectConversationAiResponseReconnectAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        dispatcher.Dispatch(new LoadProjectConversationAction(action.ProjectId));
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleStopAiResponseAsync(StopProjectConversationAiResponseAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationPendingAction(
                action.Progress.ResponseId,
                action.Progress.GenerationId));
            CommandSubmissionResponse response = await _service
                .SubmitStopAiResponseAsync(action.Progress)
                .ConfigureAwait(false);
            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationAcceptedAction(new ProjectConversationSubmissionReceiptModel(
                ProjectConversationComposerMode.AskAi,
                response.CommandId,
                response.CorrelationId,
                response.TaskId,
                response.LifecycleState.ToString(),
                response.AcceptedAt,
                "wait-for-projection")));
            dispatcher.Dispatch(new LoadProjectConversationAction(action.Progress.ProjectId));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationFailedAction(
                string.IsNullOrWhiteSpace(problem.Result?.Code) ? GenericFailureCode : problem.Result.Code));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationFailedAction(GenericFailureCode));
        }
    }

    [EffectMethod]
    public async Task HandleOpenWhyPanelAsync(OpenProjectAssociationWhyPanelAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            ProjectAssociationWhyPanelModel panel = await _service
                .GetAssociationWhyPanelAsync(action.ProjectId, action.AssociationId)
                .ConfigureAwait(false);
            dispatcher.Dispatch(new ProjectAssociationWhyPanelLoadedAction(action.ProjectId, action.AssociationId, panel));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new ProjectAssociationWhyPanelFailedAction(
                action.ProjectId,
                action.AssociationId,
                string.IsNullOrWhiteSpace(problem.Result?.Code) ? GenericFailureCode : problem.Result.Code));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new ProjectAssociationWhyPanelFailedAction(action.ProjectId, action.AssociationId, GenericFailureCode));
        }
    }
}
