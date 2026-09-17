using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>Dispatched to load or refresh the read-only operational health overview within the staleness window.</summary>
public sealed record LoadOperationalDashboardAction;
