using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;

using System.Text;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed record AiExecutionWorkItem(
    string Key,
    string TenantId,
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    long StartedSourceVersion,
    ExecuteLowRiskAIAssistance? Execution,
    AiExecutionWorkStatus Status,
    string CorrelationId,
    DateTimeOffset UpdatedAtUtc,
    string? LeaseOwner = null,
    DateTimeOffset? LeaseExpiresAtUtc = null,
    int AttemptCount = 0,
    int TerminalSubmissionAttemptCount = 0,
    string? CancellationId = null,
    LowRiskAiAssistanceExecutionRecord? CompletionRecord = null)
{
    public string? FailureReason { get; init; }

    public bool HasValidPersistedIdentity()
    {
        try
        {
            bool validKey = string.Equals(
                    Key,
                    KeyFor(TenantId, ProjectId, ConversationId, ResponseId, GenerationId),
                    StringComparison.Ordinal) ||
                string.Equals(
                    Key,
                    LegacyKeyFor(TenantId, ProjectId, ConversationId, ResponseId, GenerationId),
                    StringComparison.Ordinal);
            if (Execution is null)
            {
                return validKey &&
                    Status is AiExecutionWorkStatus.CancellationRequested &&
                    CancellationId is not null &&
                    StartedSourceVersion > 0 &&
                    !string.IsNullOrWhiteSpace(CorrelationId) &&
                    AttemptCount >= 0 &&
                    TerminalSubmissionAttemptCount >= 0;
            }

            return validKey &&
                StartedSourceVersion > 0 &&
                !string.IsNullOrWhiteSpace(CorrelationId) &&
                string.Equals(Execution.ProjectId, ProjectId, StringComparison.Ordinal) &&
                string.Equals(Execution.ProposalId, ResponseId, StringComparison.Ordinal) &&
                string.Equals(Execution.ExecutionId, GenerationId, StringComparison.Ordinal) &&
                string.Equals(Execution.CorrelationId, CorrelationId, StringComparison.Ordinal) &&
                AttemptCount >= 0 &&
                TerminalSubmissionAttemptCount >= 0;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public string CanonicalKey => KeyFor(TenantId, ProjectId, ConversationId, ResponseId, GenerationId);

    internal static AiExecutionWorkItem MergeStarted(AiExecutionWorkItem current, AiExecutionWorkItem incoming)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(incoming);
        if (current.Status is AiExecutionWorkStatus.Terminal)
        {
            return current;
        }

        if (current.Execution is null)
        {
            return incoming with
            {
                Status = current.CancellationId is null ? incoming.Status : AiExecutionWorkStatus.CancellationRequested,
                CancellationId = current.CancellationId ?? incoming.CancellationId,
                CorrelationId = incoming.CorrelationId,
                UpdatedAtUtc = incoming.UpdatedAtUtc,
            };
        }

        return current;
    }

    internal static AiExecutionWorkItem CancellationPlaceholder(
        AiResponseGenerationCancellationRequested request,
        string key)
        => new(
            key,
            request.TenantId,
            request.ProjectId,
            request.ConversationId,
            request.ResponseId,
            request.GenerationId,
            request.ExpectedSourceVersion,
            null,
            AiExecutionWorkStatus.CancellationRequested,
            request.CorrelationId,
            request.RequestedAtUtc,
            CancellationId: request.CancellationId);

    public static string KeyFor(
        string tenantId,
        string projectId,
        string conversationId,
        string responseId,
        string generationId)
    {
        return string.Join(
            '.',
            "ai-execution-v2",
            Encode(tenantId),
            Encode(projectId),
            Encode(conversationId),
            Encode(responseId),
            Encode(generationId));
    }

    internal static string LegacyKeyFor(
        string tenantId,
        string projectId,
        string conversationId,
        string responseId,
        string generationId)
        => $"ai-execution:{tenantId}:{projectId}:{conversationId}:{responseId}:{generationId}";

    private static string Encode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "AI execution identity segments cannot exceed 256 characters.");
        }

        return Convert.ToHexString(Encoding.UTF8.GetBytes(value));
    }
}
