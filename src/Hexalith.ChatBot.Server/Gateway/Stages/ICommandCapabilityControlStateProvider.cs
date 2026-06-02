using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Read-side seam exposing the FR74 governance control state for a command capability (a command <em>type</em>)
/// to the actor-agnostic admission pipeline. The durable projection of the <c>CommandCapabilityDisabled</c>
/// control-state event into this provider is deferred (mirroring the Story 7.12/7.15/7.18 sanctioned read-side
/// deferral): the default implementation always reports <see cref="CommandCapabilityControlState.Active"/>, and
/// tests inject a fake reporting <see cref="CommandCapabilityControlState.Disabled"/> to exercise the
/// <see cref="ParticipantAuthorizationStage"/> fail-closed command-capability branch in isolation. This is a
/// dedicated command-capability control plane — distinct from the per-actor providers
/// (<see cref="IServiceClientControlStateProvider"/>/<see cref="IAiActorControlStateProvider"/>) and from the
/// global static <c>ChatBotSpineCommandAllowlist</c> — keyed by the safe command type name. Each tenant's
/// disabled set is independent (isolation).
/// </summary>
internal interface ICommandCapabilityControlStateProvider
{
    /// <summary>
    /// Resolves the FR74 control state for the given command capability (command type name) within the
    /// authenticated tenant. Defaults to <see cref="CommandCapabilityControlState.Active"/> when no disable has
    /// been projected.
    /// </summary>
    ValueTask<CommandCapabilityControlState> GetControlStateAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken);
}

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
