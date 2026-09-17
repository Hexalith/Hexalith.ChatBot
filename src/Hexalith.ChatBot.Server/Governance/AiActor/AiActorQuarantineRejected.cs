using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiActor;

/// <summary>
/// Structured rejection for an invalid or unauthorized AI-actor quarantine submission/approval. Carries only
/// safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record AiActorQuarantineRejected(
    string QuarantineChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
