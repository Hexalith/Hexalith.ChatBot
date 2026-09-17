using System.Security.Claims;

using Hexalith.EventStore.Client.Queries;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed record AiExecutionExhaustedRow(
    string Key,
    string TenantId,
    string ProjectId,
    string StateOwnerAggregateId,
    string ResponseId,
    string GenerationId,
    long StartedSourceVersion,
    int AttemptCount,
    int TerminalSubmissionAttemptCount,
    string FailureReason,
    DateTimeOffset UpdatedAtUtc);
