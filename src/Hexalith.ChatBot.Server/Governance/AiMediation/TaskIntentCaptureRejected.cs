using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record TaskIntentCaptureRejected(string? SourceMessageId, string ReasonCode) : IRejectionEvent;
