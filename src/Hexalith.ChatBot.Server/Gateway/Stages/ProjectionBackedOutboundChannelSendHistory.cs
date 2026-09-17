using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedOutboundChannelSendHistory(IGovernedControlStateProjectionStore store, ISystemClock clock)
    : ProjectionBackedAdmittedHistory(store, clock, GovernedControlSubjectClasses.OutboundChannel), IOutboundChannelSendHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentSendsAsync(string tenantId, string outboundChannelRef, CancellationToken cancellationToken)
        => ReadAsync(tenantId, outboundChannelRef, cancellationToken);

    public ValueTask RecordSendAsync(string tenantId, string outboundChannelRef, DateTimeOffset sentAtUtc, CancellationToken cancellationToken)
        => RecordAsync(tenantId, outboundChannelRef, sentAtUtc, cancellationToken);
}
