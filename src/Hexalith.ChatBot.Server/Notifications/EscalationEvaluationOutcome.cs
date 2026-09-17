using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of an escalation evaluation pass: how many escalations fired, were delivered, or were suppressed fail-closed.</summary>
internal sealed record EscalationEvaluationOutcome(int Fired, int Delivered, int AuditUnavailable);
