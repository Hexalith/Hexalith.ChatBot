namespace Hexalith.ChatBot.Server.Gateway;

internal sealed record ChatBotAuthorizationFailureAuditFact(
    string TenantId,
    string ActorId,
    string CommandType,
    string ReasonCode,
    string CorrelationId,
    string? TaskId,
    string SurfaceOrigin);
