using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed record AiExecutionWorkItem(
    string Key,
    string TenantId,
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    long StartedSourceVersion,
    ExecuteLowRiskAIAssistance Execution,
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
    public static string KeyFor(
        string tenantId,
        string projectId,
        string conversationId,
        string responseId,
        string generationId)
        => $"ai-execution:{tenantId}:{projectId}:{conversationId}:{responseId}:{generationId}";
}
