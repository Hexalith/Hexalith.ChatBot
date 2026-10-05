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
    string? IdentityKeyHash = null);
