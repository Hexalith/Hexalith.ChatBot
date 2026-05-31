namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Requests a retry for a failed workflow operation through the governed command spine.
/// </summary>
/// <param name="RetryId">Stable ULID for this retry request.</param>
/// <param name="FailedEventId">Stable ULID of the failed event or operation being retried.</param>
/// <param name="FailedOperationClass">Metadata-only operation class for the failed work.</param>
/// <param name="FailureReasonCode">Finite message-catalog or retry-policy reason code.</param>
/// <param name="ExpectedFailedSourceVersion">Expected source version of the failed item.</param>
/// <param name="Rationale">Optional metadata-only operator rationale.</param>
public sealed record RequestFailedWorkflowRetry(
    string RetryId,
    string FailedEventId,
    string FailedOperationClass,
    string FailureReasonCode,
    long ExpectedFailedSourceVersion,
    string? Rationale) : IChatBotCommand;
