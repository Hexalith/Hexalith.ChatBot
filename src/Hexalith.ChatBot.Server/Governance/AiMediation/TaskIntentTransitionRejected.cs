using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record TaskIntentTransitionRejected(
    string TaskIntentId,
    string ProjectId,
    string TransitionId,
    string ReasonCode,
    long? SourceVersion,
    string CorrelationId) : IRejectionEvent;
