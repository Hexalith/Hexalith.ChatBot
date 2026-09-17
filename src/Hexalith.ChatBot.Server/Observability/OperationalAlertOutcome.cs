using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// The result of one operational-alert evaluation pass: how many alerts fired, were delivered, or were suppressed
/// fail-closed because the pre-commit audit was unavailable. Mirrors <c>ReviewerBacklogAlertOutcome</c>.
/// </summary>
internal sealed record OperationalAlertOutcome(int Fired, int Delivered, int AuditUnavailable);
