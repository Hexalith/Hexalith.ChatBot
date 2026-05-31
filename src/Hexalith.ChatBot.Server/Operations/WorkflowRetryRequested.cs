using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Metadata-only event recording that a failed workflow retry was accepted through the command spine.
/// </summary>
public sealed record WorkflowRetryRequested(
    string RetryId,
    string FailedEventId,
    string FailedOperationClass,
    string FailureReasonCode,
    long ExpectedFailedSourceVersion,
    string? Rationale) : IEventPayload;
