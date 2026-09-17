using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="ICommandCapabilityControlStateProvider"/> that always reports
/// <see cref="CommandCapabilityControlState.Active"/>. The durable projection feeding a disabled state is
/// deferred per the Story 7.12/7.15/7.18 read-side deferral; the enforcement seam is wired and unit-tested with
/// a fake.
/// </summary>
internal sealed class AlwaysActiveCommandCapabilityControlStateProvider : ICommandCapabilityControlStateProvider
{
    public ValueTask<CommandCapabilityControlState> GetControlStateAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(CommandCapabilityControlState.Active);
}
