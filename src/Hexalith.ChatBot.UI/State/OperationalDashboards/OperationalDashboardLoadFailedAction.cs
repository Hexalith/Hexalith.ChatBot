using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>Dispatched when the overview load failed; carries a safe metadata-only error code only.</summary>
/// <param name="Error">The safe metadata-only error code.</param>
public sealed record OperationalDashboardLoadFailedAction(string Error);
