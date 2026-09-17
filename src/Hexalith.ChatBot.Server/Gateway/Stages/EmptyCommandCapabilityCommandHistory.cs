using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="ICommandCapabilityCommandHistory"/> that always reports an empty admitted-command history (so
/// the seam admits by default). The durable per-(tenant × command-type) history is deferred per the Story 7.20–7.22
/// read-side deferral.
/// </summary>
internal sealed class EmptyCommandCapabilityCommandHistory : ICommandCapabilityCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
