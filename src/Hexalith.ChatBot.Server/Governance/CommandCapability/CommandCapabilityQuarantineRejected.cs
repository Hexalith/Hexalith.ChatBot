using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.CommandCapability;

/// <summary>
/// Structured rejection for an invalid or unauthorized command-capability quarantine submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record CommandCapabilityQuarantineRejected(
    string QuarantineChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
