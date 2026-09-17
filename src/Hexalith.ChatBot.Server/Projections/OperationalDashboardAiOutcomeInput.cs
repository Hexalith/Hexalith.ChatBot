using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Metadata-only AI-action-outcome health input for the dashboard's AI outcomes view. At M0/M1 fidelity this is
/// supplied by the caller from existing AI-outcome projection state; it defaults to <see cref="ChatBotHealthStatus.Unknown"/>
/// (fail-safe) when no AI-outcome source is wired.
/// </summary>
internal sealed record OperationalDashboardAiOutcomeInput(
    ChatBotHealthStatus Health,
    int Depth,
    int OldestItemAgeSeconds,
    DateTimeOffset? FreshnessTimestampUtc,
    string OwnerRole = "operations-admin")
{
    public static OperationalDashboardAiOutcomeInput Unknown(DateTimeOffset snapshotUtc)
        => new(ChatBotHealthStatus.Unknown, 0, 0, snapshotUtc);
}
