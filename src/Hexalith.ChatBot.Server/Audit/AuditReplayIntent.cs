using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Audit;

internal sealed record AuditReplayIntent(
    AuditReplayIntentKind Kind,
    string TenantId,
    string ActorId,
    string CommandName,
    string ResourceId,
    string CorrelationId,
    string? IdempotencyKey,
    string ReasonCode,
    DateTimeOffset QueuedAt,
    CommandSubmissionResponse? AcceptedOutcome = null,
    string? CoarseKeyHash = null,
    string? IdentityKeyHash = null)
{
    /// <summary>Builds the replay request for an operation whose pre-commit audit failed.</summary>
    internal static AuditReplayIntent ForPreCommitReplay(AuditEnvelope envelope, string reasonCode, DateTimeOffset queuedAt)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return new AuditReplayIntent(
            AuditReplayIntentKind.PreCommitOperationReplay,
            envelope.TenantId,
            envelope.ActorId,
            envelope.CommandName,
            envelope.ResourceId,
            envelope.CorrelationId,
            envelope.IdempotencyKey,
            reasonCode,
            queuedAt);
    }
}
