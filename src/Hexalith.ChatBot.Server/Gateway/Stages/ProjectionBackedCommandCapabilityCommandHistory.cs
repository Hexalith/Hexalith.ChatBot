using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedCommandCapabilityCommandHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.CommandCapability), ICommandCapabilityCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(string tenantId, string commandCapabilityRef, CancellationToken cancellationToken)
        => ReadAsync(tenantId, commandCapabilityRef, cancellationToken);

    public ValueTask RecordAdmittedAsync(string tenantId, string commandCapabilityRef, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, commandCapabilityRef, admittedAtUtc, cancellationToken);
}
