using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedAiActorProposalHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.AiActor), IAiActorProposalHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(string tenantId, string aiActorId, CancellationToken cancellationToken)
        => ReadAsync(tenantId, aiActorId, cancellationToken);

    public ValueTask RecordAdmittedAsync(string tenantId, string aiActorId, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, aiActorId, admittedAtUtc, cancellationToken);
}
