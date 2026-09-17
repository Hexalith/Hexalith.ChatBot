using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Stable metric-name tokens for the NFR42a published-SLO catalog (Story 8.3). Names are aligned to the Story 8.2
/// instrument names where one exists and use a stable, unique dotted token where no 8.2 instrument is yet emitted.
/// <see cref="Required"/> is the closed at-minimum set the catalog must publish (AC1) — every entry must be present
/// with no duplicates.
/// </summary>
public static class OperatingBaselineMetrics
{
    public const string IngestionLatency = "chatbot.ingestion.latency";
    public const string AssociationLatency = "chatbot.association.latency";
    public const string AmbiguousResolutionTime = "chatbot.ambiguous.resolution.time";
    public const string CommandExecutionLatency = "chatbot.command.execution.latency";
    public const string OperationIdentityLatency = "chatbot.operation.identity.latency";
    public const string AuditProjectionLag = "chatbot.audit.projection.lag";
    public const string RetryExhaustionRate = "chatbot.retry.exhausted";
    public const string DuplicateSuppressionRate = "chatbot.duplicate.suppressed";
    public const string MailboxFailureRate = "chatbot.mailbox.failure.rate";
    public const string ApprovalQueueAge = "chatbot.approval.queue.age";
    public const string AiMediationLatency = "chatbot.ai.mediation.latency";
    public const string CorrectionPropagationLatency = "chatbot.correction.propagation.latency";
    public const string MailboxSubscriptionExpiry = "chatbot.mailbox.subscription.expiry";

    /// <summary>The closed at-minimum set of metric names the published catalog must cover (NFR42a / AC1).</summary>
    public static IReadOnlyList<string> Required { get; } =
    [
        IngestionLatency,
        AssociationLatency,
        AmbiguousResolutionTime,
        CommandExecutionLatency,
        OperationIdentityLatency,
        AuditProjectionLag,
        RetryExhaustionRate,
        DuplicateSuppressionRate,
        MailboxFailureRate,
        ApprovalQueueAge,
        AiMediationLatency,
        CorrectionPropagationLatency,
        MailboxSubscriptionExpiry,
    ];
}
