using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>
/// Sandbox-only client decorator that captures the generated mailbox intake identity before optionally rejecting the
/// dependency call. It never captures command payload content.
/// </summary>
internal sealed class CapturingRecoveryChatBotClient(IChatBotClient inner, bool rejectSubmission) : IChatBotClient
{
    /// <summary>Gets the safe candidate aggregate identity observed before submission.</summary>
    public string? CandidateRef { get; private set; }

    /// <summary>Gets the UTC instant at which the safe candidate identity was observed.</summary>
    public DateTimeOffset? ObservedAtUtc { get; private set; }

    /// <inheritdoc />
    public Task<CommandSubmissionResponse> SubmitAsync(
        IChatBotCommand command,
        string? correlationId = null,
        string? taskId = null,
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command is Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake intake)
        {
            CandidateRef = intake.IntakeId;
            ObservedAtUtc = DateTimeOffset.UtcNow;
        }

        if (rejectSubmission)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new HexalithChatBotApiException(
                "The controlled recovery dependency rejected the candidate.",
                statusCode: 503,
                response: null,
                headers: new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                innerException: null);
        }

        return inner.SubmitAsync(command, correlationId, taskId, origin, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OperationStatus> GetOperationStatusAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => inner.GetOperationStatusAsync(operationId, correlationId, taskId, cancellationToken);

    /// <inheritdoc />
    public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => inner.GetOperationAuditHistoryAsync(operationId, correlationId, taskId, cancellationToken);

    /// <inheritdoc />
    public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
        string associationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => inner.GetAssociationRoutingStatusAsync(associationId, correlationId, taskId, cancellationToken);

    /// <inheritdoc />
    public Task<ProjectConversationResponse> GetProjectConversationAsync(
        string projectId,
        string? cursor = null,
        int pageSize = 25,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => inner.GetProjectConversationAsync(projectId, cursor, pageSize, correlationId, taskId, cancellationToken);
}
