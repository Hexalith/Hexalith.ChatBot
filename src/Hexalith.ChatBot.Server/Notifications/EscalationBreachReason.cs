using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>Why an escalation fired for an item: its age exceeded the threshold, or its severity met/exceeded it.</summary>
internal enum EscalationBreachReason
{
    AgeThreshold,
    SeverityThreshold,
}
