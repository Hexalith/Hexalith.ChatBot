using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Default <see cref="IServiceClientControlStateProvider"/> that always reports
/// <see cref="ServiceClientControlState.Active"/>. The durable projection feeding a disabled state is deferred
/// per the Story 7.12/7.13 read-side deferral; the validator seam is wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysActiveServiceClientControlStateProvider : IServiceClientControlStateProvider
{
    public ValueTask<ServiceClientControlState> GetControlStateAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(ServiceClientControlState.Active);
}
