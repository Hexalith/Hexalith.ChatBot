using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiActor;

/// <summary>
/// Structured rejection for an invalid or unauthorized AI-actor rate-limit submission (including an out-of-bounds
/// budget). Carries only safe tokens.
/// </summary>
public sealed record AiActorRateLimitRejected(
    string RateLimitChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
