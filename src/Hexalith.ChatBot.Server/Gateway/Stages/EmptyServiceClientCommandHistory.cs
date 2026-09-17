using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IServiceClientCommandHistory"/> that always reports an empty admitted-command history (so the
/// seam admits by default). The durable per-client history is deferred per the Story 7.14/7.15/7.16 read-side deferral.
/// </summary>
internal sealed class EmptyServiceClientCommandHistory : IServiceClientCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
