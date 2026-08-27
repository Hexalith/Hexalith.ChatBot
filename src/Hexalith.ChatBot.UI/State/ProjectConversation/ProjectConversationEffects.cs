using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed class ProjectConversationEffects(ProjectConversationService service, IState<ProjectConversationState> state)
{
    private const int SubmissionProjectionVerificationAttempts = 20;
    private static readonly TimeSpan SubmissionProjectionVerificationDelay = TimeSpan.FromMilliseconds(250);
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
            dispatcher.Dispatch(new ProjectConversationLoadedAction(
                conversation,
                action.RequestId,
                action.ProjectId,
                action.Cursor));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new ProjectConversationFailedAction(
                string.IsNullOrWhiteSpace(problem.Result?.Code) ? GenericFailureCode : problem.Result.Code,
                action.RequestId,
                action.ProjectId,
                action.Cursor));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new ProjectConversationFailedAction(
                GenericFailureCode,
                action.RequestId,
                action.ProjectId,
                action.Cursor));
        }
    }

    [EffectMethod]
    public async Task HandleSubmitComposerAsync(SubmitProjectConversationComposerAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (string.IsNullOrWhiteSpace(action.Text))
        {
            dispatcher.Dispatch(new ProjectConversationComposerValidationFailedAction(EmptyComposerCode)
            {
                RequestId = action.RequestId,
                ScopeVersion = _state.Value.ProjectScopeVersion,
            });
            return;
        }

        long scopeVersion = _state.Value.ProjectScopeVersion;
        if (!IsCurrentSubmission(action.ProjectId, action.RequestId, scopeVersion))
        {
            return;
        }

        // SubmissionToken hashes this straight into the MessageId/TaskIntentId, so every send needs a distinct value.
        // Keep it inside the public client contract as a canonical ULID: a descriptive `ui-composer:*` token is rejected
        // by ChatBotClient before transport and otherwise degrades into the generic unavailable UI state.
        string correlationId = ChatBotCorrelationId.New().Value;
        string expectedProjectionIdentity = action.Mode is ProjectConversationComposerMode.AskAi
            ? ProjectConversationService.AskAiProjectionIdentity(correlationId)
            : ProjectConversationService.UserMessageProjectionIdentity(correlationId);
        try
        {
            CommandSubmissionResponse response = action.Mode is ProjectConversationComposerMode.AskAi
                ? await _service
                    .SubmitAskAiAsync(action.ProjectId, action.Text, action.Locale, action.ExpectedSourceVersion, correlationId)
                    .ConfigureAwait(false)
                : await _service
                    .SubmitUserMessageAsync(action.ProjectId, action.Text, action.Locale, action.ExpectedSourceVersion, correlationId)
                    .ConfigureAwait(false);

            if (!IsCurrentSubmission(action.ProjectId, action.RequestId, scopeVersion))
            {
                return;
            }

            dispatcher.Dispatch(new ProjectConversationSubmissionAcceptedAction(new ProjectConversationSubmissionReceiptModel(
                action.Mode,
                response.CommandId,
                response.CorrelationId,
                response.TaskId,
                response.LifecycleState.ToString(),
                response.AcceptedAt,
                "wait-for-projection"))
            {
                ProjectId = action.ProjectId,
                RequestId = action.RequestId,
                ScopeVersion = scopeVersion,
            });

            // Admission and projection are deliberately separate durable steps. Re-query immediately for the common
            // case, then verify the accepted source-version advance with bounded GET-only polling. An ordinary message
            // or proposal does not carry AI progress, so it emits no streaming nudge; without this bounded follow-up,
            // the first read can race the EventStore projection and leave the accepted item invisible indefinitely.
            // Never resubmit the POST here. The accepted receipt has already cleared the matching draft, and every
            // retry below is an authoritative typed read. Apply the exact typed response that proved the accepted
            // identity exists: throwing it away and issuing one more GET let that final request fail transiently after
            // a successful poll, stranding the UI in a degraded state even though the item was already authoritative.
            if (!IsCurrentScope(action.ProjectId, scopeVersion))
            {
                return;
            }

            dispatcher.Dispatch(new LoadProjectConversationAction(action.ProjectId));
            ProjectConversationModel? acceptedProjection = await WaitForAcceptedProjectionAsync(
                action,
                expectedProjectionIdentity,
                scopeVersion).ConfigureAwait(false);
            if (acceptedProjection is not null && IsCurrentScope(action.ProjectId, scopeVersion))
            {
                dispatcher.Dispatch(new ProjectConversationLoadedAction(
                    acceptedProjection,
                    RequestedProjectId: action.ProjectId));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new ProjectConversationSubmissionFailedAction(
                string.IsNullOrWhiteSpace(problem.Result?.Code) ? GenericFailureCode : problem.Result.Code)
            {
                ProjectId = action.ProjectId,
                RequestId = action.RequestId,
                ScopeVersion = scopeVersion,
            });
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new ProjectConversationSubmissionFailedAction(GenericFailureCode)
            {
                ProjectId = action.ProjectId,
                RequestId = action.RequestId,
                ScopeVersion = scopeVersion,
            });
        }
    }

    private async Task<ProjectConversationModel?> WaitForAcceptedProjectionAsync(
        SubmitProjectConversationComposerAction action,
        string expectedProjectionIdentity,
        long scopeVersion)
    {
        for (int attempt = 1; attempt <= SubmissionProjectionVerificationAttempts; attempt++)
        {
            if (!IsCurrentScope(action.ProjectId, scopeVersion))
            {
                return null;
            }

            try
            {
                ProjectConversationModel conversation = await _service
                    .GetProjectConversationAsync(action.ProjectId)
                    .ConfigureAwait(false);
                if (string.Equals(conversation.ProjectId, action.ProjectId, StringComparison.Ordinal) &&
                    conversation.Items.Any(item => IsAcceptedProjectionItem(item, expectedProjectionIdentity)))
                {
                    return conversation;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // The command is already accepted. A transient or safely-redacted read failure must never be
                // reclassified as submission failure or trigger another mutation; keep the accepted receipt and let
                // the remaining bounded reads, SignalR, reconnect, or an explicit user reload recover the view.
            }

            if (attempt < SubmissionProjectionVerificationAttempts)
            {
                await Task.Delay(SubmissionProjectionVerificationDelay).ConfigureAwait(false);
            }
        }

        return null;
    }

    // SignalR is advisory only: never invent a version watermark. A matching signal triggers an authoritative read,
    // and the reducer advances state only after that read returns for the still-current project scope.
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

        // Tenant fail-closed. The action has always carried TenantId and this method's contract claimed to fail closed
        // on it, but nothing compared it -- the guard existed only in the comment. Compared only when the loaded
        // conversation actually carries a tenant, so the no-JWT dev/test posture (null TenantContext) still nudges.
        if (!string.IsNullOrWhiteSpace(conversation.TenantContext) &&
            !string.Equals(conversation.TenantContext, action.TenantId, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        if (action.ScopeVersion != 0 && action.ScopeVersion != _state.Value.ProjectScopeVersion)
        {
            return Task.CompletedTask;
        }

        dispatcher.Dispatch(new LoadProjectConversationAction(conversation.ProjectId));
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

    [EffectMethod]
    public Task HandleAiResponseReconnectAsync(ProjectConversationAiResponseReconnectAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if ((action.ScopeVersion == 0 || action.ScopeVersion == _state.Value.ProjectScopeVersion) &&
            IsCurrentScope(action.ProjectId, _state.Value.ProjectScopeVersion))
        {
            dispatcher.Dispatch(new LoadProjectConversationAction(action.ProjectId));
        }
        return Task.CompletedTask;
    }

    [EffectMethod]
    public async Task HandleStopAiResponseAsync(StopProjectConversationAiResponseAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            long scopeVersion = _state.Value.ProjectScopeVersion;
            if (!IsCurrentScope(action.Progress.ProjectId, scopeVersion))
            {
                return;
            }

            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationPendingAction(
                action.Progress.ResponseId,
                action.Progress.GenerationId)
            {
                ProjectId = action.Progress.ProjectId,
                RequestId = action.RequestId,
                ScopeVersion = scopeVersion,
            });
            CommandSubmissionResponse response = await _service
                .SubmitStopAiResponseAsync(action.Progress)
                .ConfigureAwait(false);
            if (!IsCurrentCancellation(action.Progress.ProjectId, action.RequestId, scopeVersion))
            {
                return;
            }

            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationAcceptedAction(new ProjectConversationSubmissionReceiptModel(
                ProjectConversationComposerMode.AskAi,
                response.CommandId,
                response.CorrelationId,
                response.TaskId,
                response.LifecycleState.ToString(),
                response.AcceptedAt,
                "wait-for-projection"))
            {
                ProjectId = action.Progress.ProjectId,
                RequestId = action.RequestId,
                ScopeVersion = scopeVersion,
            });
            await VerifyCancellationTerminalAsync(action.Progress, action.RequestId, scopeVersion, dispatcher).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationFailedAction(
                string.IsNullOrWhiteSpace(problem.Result?.Code) ? GenericFailureCode : problem.Result.Code)
            {
                ProjectId = action.Progress.ProjectId,
                RequestId = action.RequestId,
                ScopeVersion = _state.Value.ProjectScopeVersion,
            });
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationFailedAction(GenericFailureCode)
            {
                ProjectId = action.Progress.ProjectId,
                RequestId = action.RequestId,
                ScopeVersion = _state.Value.ProjectScopeVersion,
            });
        }
    }

    private async Task VerifyCancellationTerminalAsync(
        ProjectConversationAiResponseProgressModel requested,
        string requestId,
        long scopeVersion,
        IDispatcher dispatcher)
    {
        DateTimeOffset deadline = requested.RecoveryDeadlineUtc ?? DateTimeOffset.UtcNow.AddSeconds(2);
        while (DateTimeOffset.UtcNow <= deadline && IsCurrentCancellation(requested.ProjectId, requestId, scopeVersion))
        {
            try
            {
                ProjectConversationModel conversation = await _service
                    .GetProjectConversationAsync(requested.ProjectId)
                    .ConfigureAwait(false);
                if (!IsCurrentCancellation(requested.ProjectId, requestId, scopeVersion))
                {
                    return;
                }

                dispatcher.Dispatch(new ProjectConversationLoadedAction(conversation));
                ProjectConversationAiResponseProgressModel? terminal = conversation.Items
                    .Select(static item => item.AiResponseProgress)
                    .OfType<ProjectConversationAiResponseProgressModel>()
                    .FirstOrDefault(progress =>
                        string.Equals(progress.ProjectId, requested.ProjectId, StringComparison.Ordinal) &&
                        string.Equals(progress.ConversationId, requested.ConversationId, StringComparison.Ordinal) &&
                        string.Equals(progress.ResponseId, requested.ResponseId, StringComparison.Ordinal) &&
                        string.Equals(progress.GenerationId, requested.GenerationId, StringComparison.Ordinal) &&
                        progress.IsTerminal);
                if (terminal is not null)
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // A transient read failure consumes one bounded verification attempt and keeps the last safe view.
            }

            await Task.Delay(SubmissionProjectionVerificationDelay).ConfigureAwait(false);
        }

        if (IsCurrentCancellation(requested.ProjectId, requestId, scopeVersion))
        {
            dispatcher.Dispatch(new ProjectConversationAiResponseCancellationFailedAction(
                "ai-response-cancellation-unverified")
            {
                ProjectId = requested.ProjectId,
                RequestId = requestId,
                ScopeVersion = scopeVersion,
            });
        }
    }

    private static bool IsAcceptedProjectionItem(ProjectConversationItemModel item, string expectedIdentity)
        => string.Equals(item.ItemId, expectedIdentity, StringComparison.Ordinal) ||
            string.Equals(item.TaskId, expectedIdentity, StringComparison.Ordinal) ||
            string.Equals(item.AiProposalId, expectedIdentity, StringComparison.Ordinal) ||
            string.Equals(item.AiRequestId, expectedIdentity, StringComparison.Ordinal) ||
            string.Equals(item.AiSourceMessageId, expectedIdentity, StringComparison.Ordinal);

    private bool IsCurrentScope(string projectId, long scopeVersion)
    {
        string? scopedProjectId = _state.Value.RequestedProjectId ?? _state.Value.Conversation?.ProjectId;
        return scopeVersion == _state.Value.ProjectScopeVersion &&
            (scopedProjectId is null || string.Equals(scopedProjectId, projectId, StringComparison.Ordinal));
    }

    private bool IsCurrentSubmission(string projectId, string requestId, long scopeVersion)
        => IsCurrentScope(projectId, scopeVersion) &&
            (_state.Value.SubmissionRequestId is null ||
             string.Equals(_state.Value.SubmissionRequestId, requestId, StringComparison.Ordinal));

    private bool IsCurrentCancellation(string projectId, string requestId, long scopeVersion)
        => IsCurrentScope(projectId, scopeVersion) &&
            (_state.Value.CancellationRequestId is null ||
             string.Equals(_state.Value.CancellationRequestId, requestId, StringComparison.Ordinal));

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
