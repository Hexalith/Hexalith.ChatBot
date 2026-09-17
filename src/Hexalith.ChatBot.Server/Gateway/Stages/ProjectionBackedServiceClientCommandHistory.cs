using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedServiceClientCommandHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.ServiceClient), IServiceClientCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
        => ReadAsync(tenantId, serviceClientId, cancellationToken);

    public ValueTask RecordAdmittedAsync(string tenantId, string serviceClientId, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, serviceClientId, admittedAtUtc, cancellationToken);
}
