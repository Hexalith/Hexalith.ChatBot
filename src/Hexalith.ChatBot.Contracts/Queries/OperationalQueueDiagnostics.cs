using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record OperationalQueueDiagnostics(
    string CorrelationId,
    string TenantRef,
    string? MailboxRef,
    string WorkflowItemRef,
    string CurrentState,
    string LastTransition,
    int RetryCount,
    string? FailureReason,
    string NextSafeAction);
