using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Cli;

using AssociateEmailToProjectCommand = Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject;
using ApprovalDecision = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind;
using AssociationCorrection = Hexalith.ChatBot.Contracts.Enums.AssociationCorrectionKind;
using AssociationDecision = Hexalith.ChatBot.Contracts.Enums.AssociationDecisionKind;
using CorrectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation;
using DecideAiActionApprovalCommand = Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval;
using DeferEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.DeferEmailProjectAssociation;
using ExecuteApprovedAIActionCommand = Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction;
using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;
using RejectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.RejectEmailProjectAssociation;
using RequestFailedWorkflowRetryCommand = Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry;

public sealed class ChatBotCliService
{
    private readonly IChatBotClient _client;
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public ChatBotCliService(
        IChatBotClient client,
        TextWriter output,
        TextWriter error)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public async Task<int> ShowAssociationStatusAsync(
        string associationId,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            AssociationRoutingStatus status = await _client
                .GetAssociationRoutingStatusAsync(associationId, options.CorrelationId, options.TaskId, cancellationToken)
                .ConfigureAwait(false);
            await _output.WriteAsync(ChatBotCliOutputFormatter.FormatAssociationStatus(status, options.Json)).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (IsSafeClientFailure(ex))
        {
            return await WriteSafeDenialAsync(ex, options.Json).ConfigureAwait(false);
        }
    }

    public async Task<int> ShowConversationAsync(
        string projectId,
        string? cursor,
        int pageSize,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            ProjectConversationResponse response = await _client
                .GetProjectConversationAsync(projectId, cursor, pageSize, options.CorrelationId, options.TaskId, cancellationToken)
                .ConfigureAwait(false);
            await _output.WriteAsync(ChatBotCliOutputFormatter.FormatProjectConversation(response, options.Json)).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (IsSafeClientFailure(ex))
        {
            return await WriteSafeDenialAsync(ex, options.Json).ConfigureAwait(false);
        }
    }

    public async Task<int> ShowTaskReviewAsync(
        string projectId,
        string taskIntentId,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            TaskIntentReview review = await _client
                .GetTaskIntentReviewAsync(projectId, taskIntentId, options.CorrelationId, options.TaskId, cancellationToken)
                .ConfigureAwait(false);
            await _output.WriteAsync(ChatBotCliOutputFormatter.FormatTaskIntentReview(review, options.Json)).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (IsSafeClientFailure(ex))
        {
            return await WriteSafeDenialAsync(ex, options.Json).ConfigureAwait(false);
        }
    }

    public async Task<int> ShowOperationStatusAsync(
        string operationId,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            OperationStatus status = await _client
                .GetOperationStatusAsync(operationId, options.CorrelationId, options.TaskId, cancellationToken)
                .ConfigureAwait(false);
            await _output.WriteAsync(ChatBotCliOutputFormatter.FormatOperationStatus(status, options.Json)).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (IsSafeClientFailure(ex))
        {
            return await WriteSafeDenialAsync(ex, options.Json).ConfigureAwait(false);
        }
    }

    public async Task<int> ShowOperationAuditAsync(
        string operationId,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            OperationAuditHistory history = await _client
                .GetOperationAuditHistoryAsync(operationId, options.CorrelationId, options.TaskId, cancellationToken)
                .ConfigureAwait(false);
            await _output.WriteAsync(ChatBotCliOutputFormatter.FormatOperationAudit(history, options.Json)).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (IsSafeClientFailure(ex))
        {
            return await WriteSafeDenialAsync(ex, options.Json).ConfigureAwait(false);
        }
    }

    public Task<int> AssociateAsync(
        string associationId,
        string intakeId,
        string projectId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? note,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
        => SubmitAsync(
            new AssociateEmailToProjectCommand(
                associationId,
                intakeId,
                projectId,
                AssociationDecision.Associate,
                note,
                evidenceFingerprint,
                sourceVersion,
                schemaVersion),
            options,
            cancellationToken);

    public Task<int> RejectAssociationAsync(
        string associationId,
        string intakeId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? note,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
        => SubmitAsync(
            new RejectEmailProjectAssociationCommand(
                associationId,
                intakeId,
                AssociationDecision.Reject,
                note,
                evidenceFingerprint,
                sourceVersion,
                schemaVersion),
            options,
            cancellationToken);

    public Task<int> DeferAssociationAsync(
        string associationId,
        string intakeId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? note,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
        => SubmitAsync(
            new DeferEmailProjectAssociationCommand(
                associationId,
                intakeId,
                AssociationDecision.Defer,
                note,
                evidenceFingerprint,
                sourceVersion,
                schemaVersion),
            options,
            cancellationToken);

    public Task<int> CorrectAssociationAsync(
        string associationId,
        string intakeId,
        string priorProjectId,
        string targetProjectId,
        string predecessorAssociationId,
        string evidenceFingerprint,
        long sourceVersion,
        string schemaVersion,
        string? rationale,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
        => SubmitAsync(
            new CorrectEmailProjectAssociationCommand(
                associationId,
                intakeId,
                priorProjectId,
                targetProjectId,
                AssociationCorrection.ProjectReassignment,
                rationale,
                predecessorAssociationId,
                evidenceFingerprint,
                sourceVersion,
                schemaVersion),
            options,
            cancellationToken);

    public Task<int> RetryOperationAsync(
        string retryId,
        string failedEventId,
        string failedOperationClass,
        string failureReasonCode,
        long expectedFailedSourceVersion,
        string? rationale,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
        => SubmitAsync(
            new RequestFailedWorkflowRetryCommand(
                retryId,
                failedEventId,
                failedOperationClass,
                failureReasonCode,
                expectedFailedSourceVersion,
                rationale),
            options,
            cancellationToken);

    public Task<int> DecideApprovalAsync(
        string projectId,
        string approvalId,
        string proposalId,
        string sourceMessageId,
        ApprovalDecision decision,
        long expectedApprovalSourceVersion,
        string commandCorrelationId,
        string decisionId,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
        => SubmitAsync(
            new DecideAiActionApprovalCommand(
                projectId,
                approvalId,
                proposalId,
                sourceMessageId,
                decision,
                expectedApprovalSourceVersion,
                commandCorrelationId,
                decisionId),
            options,
            cancellationToken);

    public Task<int> ExecuteAiActionAsync(
        string projectId,
        string proposalId,
        string approvalId,
        string taskIntentId,
        string sourceMessageId,
        string requesterId,
        string commandName,
        string commandAllowlistVersion,
        long expectedApprovalSourceVersion,
        long expectedProposalSourceVersion,
        string commandCorrelationId,
        string executionId,
        string transitionId,
        IReadOnlyList<string> sourceEvidenceReferences,
        IReadOnlyList<string> affectedResourceReferences,
        IReadOnlyList<string> recipientReferences,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
        => SubmitAsync(
            new ExecuteApprovedAIActionCommand(
                projectId,
                proposalId,
                approvalId,
                taskIntentId,
                sourceMessageId,
                requesterId,
                commandName,
                commandAllowlistVersion,
                expectedApprovalSourceVersion,
                expectedProposalSourceVersion,
                commandCorrelationId,
                executionId,
                transitionId,
                sourceEvidenceReferences,
                affectedResourceReferences,
                recipientReferences),
            options,
            cancellationToken);

    private async Task<int> SubmitAsync(
        IChatBotCommand command,
        ChatBotCliOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            CommandSubmissionResponse response = await _client
                .SubmitAsync(command, options.CorrelationId, options.TaskId, ChatBotSurfaceOrigin.Cli, cancellationToken)
                .ConfigureAwait(false);
            await _output.WriteAsync(ChatBotCliOutputFormatter.FormatCommandAccepted(response, options.Json)).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (IsSafeClientFailure(ex))
        {
            return await WriteSafeDenialAsync(ex, options.Json).ConfigureAwait(false);
        }
    }

    private async Task<int> WriteSafeDenialAsync(Exception ex, bool json)
    {
        await _error.WriteAsync(ChatBotCliOutputFormatter.FormatSafeDenial(ex, json)).ConfigureAwait(false);
        return 1;
    }

    private static bool IsSafeClientFailure(Exception ex)
        => ex is ArgumentException or HexalithChatBotApiException or InvalidOperationException;
}
