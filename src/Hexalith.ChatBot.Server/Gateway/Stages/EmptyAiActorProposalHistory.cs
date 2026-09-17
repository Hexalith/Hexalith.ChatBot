using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IAiActorProposalHistory"/> that always reports an empty admitted-proposal history (so the
/// seam admits by default). The durable per-AI-actor history is deferred per the Story 7.14–7.19 read-side deferral.
/// </summary>
internal sealed class EmptyAiActorProposalHistory : IAiActorProposalHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
