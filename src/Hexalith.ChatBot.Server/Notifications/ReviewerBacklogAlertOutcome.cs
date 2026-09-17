using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of a reviewer-backlog evaluation pass: how many alerts fired, were delivered, or were suppressed fail-closed.</summary>
internal sealed record ReviewerBacklogAlertOutcome(int Fired, int Delivered, int AuditUnavailable);
