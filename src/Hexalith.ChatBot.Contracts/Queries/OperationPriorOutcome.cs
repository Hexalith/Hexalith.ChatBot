using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>The metadata-only outcome retained for replay or a predecessor attempt.</summary>
/// <param name="CommandId">The original caller command ULID.</param>
/// <param name="CorrelationId">The original correlation identity.</param>
/// <param name="OperationId">The exact original operation identity.</param>
/// <param name="LifecycleState">The compatible lifecycle state.</param>
/// <param name="AcceptedAt">The original UTC acceptance time.</param>
/// <param name="ReasonCode">The catalog-owned safe reason.</param>
/// <param name="RetryEligible">Whether current policy permits retry.</param>
/// <param name="TaskId">The optional original task identity.</param>
public sealed record OperationPriorOutcome(
    string CommandId,
    string CorrelationId,
    string OperationId,
    LifecycleState LifecycleState,
    DateTimeOffset AcceptedAt,
    string ReasonCode,
    bool RetryEligible,
    string? TaskId = null);
