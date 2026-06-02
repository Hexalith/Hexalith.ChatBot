using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74 governance control state for an AI actor to the admission pipeline. The
/// durable projection of the <c>AiActorDisabled</c> control-state event into this provider is deferred
/// (mirroring the Story 7.12/7.15 sanctioned read-side deferral): the default implementation always reports
/// <see cref="AiActorControlState.Active"/>, and tests inject a fake reporting
/// <see cref="AiActorControlState.Disabled"/> to exercise the <see cref="ServiceClientGrantValidator"/>
/// fail-closed AI-actor branch in isolation. This is a dedicated AI-actor control plane, separate from
/// <see cref="IServiceClientControlStateProvider"/> even though both subjects flow through the same validator.
/// </summary>
internal interface IAiActorControlStateProvider
{
    /// <summary>
    /// Resolves the FR74 control state for the given AI actor within the authenticated tenant. Defaults to
    /// <see cref="AiActorControlState.Active"/> when no disable has been projected.
    /// </summary>
    ValueTask<AiActorControlState> GetControlStateAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IAiActorControlStateProvider"/> that always reports
/// <see cref="AiActorControlState.Active"/>. The durable projection feeding a disabled state is deferred per the
/// Story 7.12/7.15 read-side deferral; the validator seam is wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysActiveAiActorControlStateProvider : IAiActorControlStateProvider
{
    public ValueTask<AiActorControlState> GetControlStateAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(AiActorControlState.Active);
}
