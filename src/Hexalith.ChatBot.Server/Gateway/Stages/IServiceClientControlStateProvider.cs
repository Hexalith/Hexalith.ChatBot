using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74 governance control state for a service client to the admission pipeline.
/// The durable projection of the <c>ServiceClientDisabled</c> control-state event into this provider is
/// deferred (mirroring the Story 7.12/7.13 sanctioned read-side deferral): the default implementation always
/// reports <see cref="ServiceClientControlState.Active"/>, and tests inject a fake reporting
/// <see cref="ServiceClientControlState.Disabled"/> to exercise the <see cref="ServiceClientGrantValidator"/>
/// fail-closed branch in isolation.
/// </summary>
internal interface IServiceClientControlStateProvider
{
    /// <summary>
    /// Resolves the FR74 control state for the given service client within the authenticated tenant. Defaults
    /// to <see cref="ServiceClientControlState.Active"/> when no disable has been projected.
    /// </summary>
    ValueTask<ServiceClientControlState> GetControlStateAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken);
}

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
