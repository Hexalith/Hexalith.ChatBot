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
    DateTimeOffset QueuedAt);
