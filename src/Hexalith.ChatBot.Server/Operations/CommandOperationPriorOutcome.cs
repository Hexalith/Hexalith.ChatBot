using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>The retained safe outcome of the predecessor attempt; no command contents are carried.</summary>
/// <param name="OperationId">The original operation identity.</param>
/// <param name="CorrelationId">The original correlation identity.</param>
/// <param name="CreatedAt">The original server UTC creation time.</param>
/// <param name="State">The retained terminal state.</param>
/// <param name="ReasonCode">The catalog-owned safe reason.</param>
/// <param name="CommittedRevision">The original committed revision, when present.</param>
internal sealed record CommandOperationPriorOutcome(
    string OperationId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    CommandExecutionState State,
    string ReasonCode,
    long? CommittedRevision);
